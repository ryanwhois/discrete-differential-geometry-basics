using GeometryKit.Core;
using GeometryKit.Mesh;

namespace GeometryKit.Fabrication;

public sealed record SectionSegment(Point3d Start, Point3d End, int TriangleIndex);
public sealed record SectionResult(IReadOnlyList<SectionSegment> Segments, int CoplanarTriangleCount);

/// <summary>
/// Returns independent triangle-plane segments. Stitching them into ordered polylines is a
/// separate tolerance-sensitive operation and is intentionally not hidden in this first API.
/// </summary>
public static class MeshSectioner
{
    public static SectionResult Intersect(TriangleMesh mesh, Plane3d plane, GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        var tol = tolerance ?? GeometryTolerance.Default;
        var normal = plane.Normal.Normalized();
        if (normal.LengthSquared == 0) throw new ArgumentException("Section plane must have a non-zero normal.", nameof(plane));
        var normalisedPlane = new Plane3d(plane.Origin, normal);
        var segments = new List<SectionSegment>();
        var coplanar = 0;

        for (var face = 0; face < mesh.TriangleCount; face++)
        {
            var (a, b, c) = mesh.GetTriangle(face);
            var points = new[] { a, b, c };
            var distances = points.Select(normalisedPlane.SignedDistance).ToArray();
            var scale = Math.Max(Point3d.Distance(a, b), Math.Max(Point3d.Distance(b, c), Point3d.Distance(c, a)));
            var epsilon = tol.ForScale(scale);
            if (distances.All(distance => Math.Abs(distance) <= epsilon))
            {
                coplanar++;
                continue;
            }

            var intersections = new List<Point3d>(3);
            AddEdgeIntersection(points[0], points[1], distances[0], distances[1], epsilon, intersections);
            AddEdgeIntersection(points[1], points[2], distances[1], distances[2], epsilon, intersections);
            AddEdgeIntersection(points[2], points[0], distances[2], distances[0], epsilon, intersections);
            Deduplicate(intersections, epsilon);
            if (intersections.Count == 2)
                segments.Add(new(intersections[0], intersections[1], face));
        }
        return new(segments, coplanar);
    }

    private static void AddEdgeIntersection(Point3d start, Point3d end, double startDistance, double endDistance, double epsilon, List<Point3d> intersections)
    {
        var startOnPlane = Math.Abs(startDistance) <= epsilon;
        var endOnPlane = Math.Abs(endDistance) <= epsilon;
        if (startOnPlane) intersections.Add(start);
        if (endOnPlane) intersections.Add(end);
        if (startOnPlane || endOnPlane || Math.Sign(startDistance) == Math.Sign(endDistance)) return;
        var interpolation = startDistance / (startDistance - endDistance);
        intersections.Add(start + (end - start) * interpolation);
    }

    private static void Deduplicate(List<Point3d> points, double epsilon)
    {
        for (var i = points.Count - 1; i >= 0; i--)
            for (var j = 0; j < i; j++)
                if (Point3d.DistanceSquared(points[i], points[j]) <= epsilon * epsilon)
                {
                    points.RemoveAt(i);
                    break;
                }
    }
}
