using GeometryKit.Core;

namespace GeometryKit.Mesh;

public sealed record MeshProjection(Point3d Point, int TriangleIndex, (double A, double B, double C) Barycentric, double Distance);
public sealed record RaycastHit(Point3d Point, int TriangleIndex, (double A, double B, double C) Barycentric, double Parameter);

/// <summary>
/// Immutable median-split BVH for closest-point and ray queries. It deliberately indexes the
/// supplied mesh snapshot only; callers rebuild after an edit rather than receiving stale results.
/// </summary>
public sealed class MeshSpatialIndex
{
    private const int LeafSize = 8;
    private readonly TriangleMesh _mesh;
    private readonly int[] _triangleIndices;
    private readonly Node _root;

    public MeshSpatialIndex(TriangleMesh mesh, GeometryTolerance? tolerance = null)
    {
        _mesh = mesh ?? throw new ArgumentNullException(nameof(mesh));
        var report = MeshHealthAnalyzer.Analyse(mesh, tolerance);
        if (!report.IsQueryable)
            throw new ArgumentException("A spatial index requires a mesh without degenerate or non-manifold triangles.", nameof(mesh));
        _triangleIndices = Enumerable.Range(0, mesh.TriangleCount).ToArray();
        _root = Build(0, _triangleIndices.Length);
    }

    public MeshProjection? ClosestPoint(Point3d point)
    {
        if (_triangleIndices.Length == 0) return null;
        var bestDistanceSquared = double.PositiveInfinity;
        MeshProjection? best = null;
        var queue = new PriorityQueue<Node, double>();
        queue.Enqueue(_root, _root.Bounds.DistanceSquared(point));
        while (queue.TryDequeue(out var node, out var lowerBound))
        {
            if (lowerBound > bestDistanceSquared) continue;
            if (node.IsLeaf)
            {
                for (var index = node.Start; index < node.Start + node.Count; index++)
                {
                    var triangleIndex = _triangleIndices[index];
                    var candidate = ClosestPointOnTriangle(point, triangleIndex);
                    var distanceSquared = Point3d.DistanceSquared(point, candidate.Point);
                    if (distanceSquared >= bestDistanceSquared) continue;
                    bestDistanceSquared = distanceSquared;
                    best = candidate with { Distance = Math.Sqrt(distanceSquared) };
                }
            }
            else
            {
                queue.Enqueue(node.Left!, node.Left!.Bounds.DistanceSquared(point));
                queue.Enqueue(node.Right!, node.Right!.Bounds.DistanceSquared(point));
            }
        }
        return best;
    }

    public RaycastHit? Raycast(Ray3d ray, double maximumDistance = double.PositiveInfinity)
    {
        if (_triangleIndices.Length == 0) return null;
        if (ray.Direction.LengthSquared == 0) throw new ArgumentException("Ray direction must be non-zero.", nameof(ray));
        RaycastHit? closest = null;
        var stack = new Stack<Node>();
        stack.Push(_root);
        while (stack.TryPop(out var node))
        {
            var limit = closest?.Parameter ?? maximumDistance;
            if (!node.Bounds.Intersects(ray, limit)) continue;
            if (node.IsLeaf)
            {
                for (var index = node.Start; index < node.Start + node.Count; index++)
                {
                    var hit = RayTriangle(ray, _triangleIndices[index]);
                    if (hit is not null && hit.Parameter <= (closest?.Parameter ?? maximumDistance)) closest = hit;
                }
            }
            else
            {
                stack.Push(node.Left!);
                stack.Push(node.Right!);
            }
        }
        return closest;
    }

    private Node Build(int start, int count)
    {
        var bounds = Aabb3d.Empty;
        var centroidBounds = Aabb3d.Empty;
        for (var i = start; i < start + count; i++)
        {
            var triangleBounds = TriangleBounds(_triangleIndices[i]);
            bounds = bounds.Encapsulate(triangleBounds);
            centroidBounds = centroidBounds.Encapsulate(triangleBounds.Centre);
        }
        if (count <= LeafSize) return new(bounds, start, count, null, null);

        var extents = centroidBounds.Extents;
        var axis = extents.X >= extents.Y && extents.X >= extents.Z ? 0 : extents.Y >= extents.Z ? 1 : 2;
        Array.Sort(_triangleIndices, start, count, Comparer<int>.Create((left, right) =>
            Coordinate(TriangleBounds(left).Centre, axis).CompareTo(Coordinate(TriangleBounds(right).Centre, axis))));
        var leftCount = count / 2;
        return new(bounds, start, count, Build(start, leftCount), Build(start + leftCount, count - leftCount));
    }

    private Aabb3d TriangleBounds(int triangleIndex)
    {
        var (a, b, c) = _mesh.GetTriangle(triangleIndex);
        return Aabb3d.FromPoint(a).Encapsulate(b).Encapsulate(c);
    }

    private static double Coordinate(Point3d point, int axis) => axis switch { 0 => point.X, 1 => point.Y, _ => point.Z };

    private MeshProjection ClosestPointOnTriangle(Point3d point, int triangleIndex)
    {
        // Christer Ericson, Real-Time Collision Detection, closest point on triangle.
        var (a, b, c) = _mesh.GetTriangle(triangleIndex);
        var ab = b - a;
        var ac = c - a;
        var ap = point - a;
        var d1 = Point3d.Dot(ab, ap);
        var d2 = Point3d.Dot(ac, ap);
        if (d1 <= 0 && d2 <= 0) return new(a, triangleIndex, (1, 0, 0), 0);

        var bp = point - b;
        var d3 = Point3d.Dot(ab, bp);
        var d4 = Point3d.Dot(ac, bp);
        if (d3 >= 0 && d4 <= d3) return new(b, triangleIndex, (0, 1, 0), 0);

        var vc = d1 * d4 - d3 * d2;
        if (vc <= 0 && d1 >= 0 && d3 <= 0)
        {
            var v = d1 / (d1 - d3);
            return new(a + ab * v, triangleIndex, (1 - v, v, 0), 0);
        }

        var cp = point - c;
        var d5 = Point3d.Dot(ab, cp);
        var d6 = Point3d.Dot(ac, cp);
        if (d6 >= 0 && d5 <= d6) return new(c, triangleIndex, (0, 0, 1), 0);

        var vb = d5 * d2 - d1 * d6;
        if (vb <= 0 && d2 >= 0 && d6 <= 0)
        {
            var w = d2 / (d2 - d6);
            return new(a + ac * w, triangleIndex, (1 - w, 0, w), 0);
        }

        var va = d3 * d6 - d5 * d4;
        if (va <= 0 && (d4 - d3) >= 0 && (d5 - d6) >= 0)
        {
            var bc = c - b;
            var w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
            return new(b + bc * w, triangleIndex, (0, 1 - w, w), 0);
        }

        var denominator = 1.0 / (va + vb + vc);
        var faceV = vb * denominator;
        var faceW = vc * denominator;
        return new(a + ab * faceV + ac * faceW, triangleIndex, (1 - faceV - faceW, faceV, faceW), 0);
    }

    private RaycastHit? RayTriangle(Ray3d ray, int triangleIndex)
    {
        var (a, b, c) = _mesh.GetTriangle(triangleIndex);
        var edge1 = b - a;
        var edge2 = c - a;
        var p = Point3d.Cross(ray.Direction, edge2);
        var determinant = Point3d.Dot(edge1, p);
        if (Math.Abs(determinant) < 1e-12) return null;
        var inverse = 1.0 / determinant;
        var t = ray.Origin - a;
        var u = Point3d.Dot(t, p) * inverse;
        if (u is < 0 or > 1) return null;
        var q = Point3d.Cross(t, edge1);
        var v = Point3d.Dot(ray.Direction, q) * inverse;
        if (v < 0 || u + v > 1) return null;
        var parameter = Point3d.Dot(edge2, q) * inverse;
        return parameter >= 0 ? new(ray.At(parameter), triangleIndex, (1 - u - v, u, v), parameter) : null;
    }

    private sealed record Node(Aabb3d Bounds, int Start, int Count, Node? Left, Node? Right)
    {
        public bool IsLeaf => Left is null;
    }
}
