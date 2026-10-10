using GeometryKit.Core;
using GeometryKit.Mesh;

namespace GeometryKit.Analysis;

public sealed record GaussianCurvatureResult(
    IReadOnlyList<double> Integrated,
    IReadOnlyList<double> Density,
    IReadOnlyList<double> VertexAreas,
    double TotalIntegratedCurvature);

public sealed record MeanCurvatureResult(
    IReadOnlyList<Point3d> MeanCurvatureNormal,
    IReadOnlyList<double> Magnitude,
    IReadOnlyList<double> VertexAreas);

public sealed record DistanceFieldResult(IReadOnlyList<double> Distances, IReadOnlyList<int?> Predecessors);

/// <summary>Discrete surface quantities for an immutable triangle mesh.</summary>
public static class SurfaceAnalysis
{
    public static GaussianCurvatureResult GaussianCurvature(TriangleMesh mesh, GeometryTolerance? tolerance = null)
    {
        EnsureQueryable(mesh, tolerance);
        var vertexAreas = BarycentricVertexAreas(mesh);
        var angles = new double[mesh.VertexCount];
        var edgeCounts = EdgeCounts(mesh);
        var boundaryVertices = new bool[mesh.VertexCount];
        foreach (var (edge, count) in edgeCounts.Where(pair => pair.Value == 1))
        {
            boundaryVertices[edge.Low] = true;
            boundaryVertices[edge.High] = true;
        }

        for (var i = 0; i < mesh.TriangleCount; i++)
        {
            var triangle = mesh.Triangles[i];
            var (a, b, c) = mesh.GetTriangle(i);
            angles[triangle.A] += Angle(b - a, c - a);
            angles[triangle.B] += Angle(c - b, a - b);
            angles[triangle.C] += Angle(a - c, b - c);
        }

        var integrated = new double[mesh.VertexCount];
        var density = new double[mesh.VertexCount];
        for (var i = 0; i < integrated.Length; i++)
        {
            integrated[i] = (boundaryVertices[i] ? Math.PI : 2.0 * Math.PI) - angles[i];
            density[i] = vertexAreas[i] > 0 ? integrated[i] / vertexAreas[i] : double.NaN;
        }
        return new(integrated, density, vertexAreas, integrated.Sum());
    }

    public static MeanCurvatureResult MeanCurvature(TriangleMesh mesh, GeometryTolerance? tolerance = null)
    {
        EnsureQueryable(mesh, tolerance);
        var areas = BarycentricVertexAreas(mesh);
        var accumulated = new Point3d[mesh.VertexCount];
        var weights = new Dictionary<(int Low, int High), double>();

        for (var index = 0; index < mesh.TriangleCount; index++)
        {
            var triangle = mesh.Triangles[index];
            var (a, b, c) = mesh.GetTriangle(index);
            AddCotanWeight(weights, triangle.B, triangle.C, Cotangent(b - a, c - a) * 0.5);
            AddCotanWeight(weights, triangle.C, triangle.A, Cotangent(c - b, a - b) * 0.5);
            AddCotanWeight(weights, triangle.A, triangle.B, Cotangent(a - c, b - c) * 0.5);
        }

        foreach (var (edge, weight) in weights)
        {
            var difference = mesh[edge.Low] - mesh[edge.High];
            accumulated[edge.Low] += weight * difference;
            accumulated[edge.High] -= weight * difference;
        }

        var normal = new Point3d[mesh.VertexCount];
        var magnitude = new double[mesh.VertexCount];
        for (var i = 0; i < normal.Length; i++)
        {
            normal[i] = areas[i] > 0 ? accumulated[i] / (2.0 * areas[i]) : Point3d.Zero;
            magnitude[i] = normal[i].Length;
        }
        return new(normal, magnitude, areas);
    }

    /// <summary>
    /// Exact shortest paths on the mesh 1-skeleton. This is a dependable baseline for public
    /// APIs; the heat-method surface approximation belongs in a later sparse-solver package.
    /// </summary>
    public static DistanceFieldResult EdgeGeodesics(TriangleMesh mesh, IEnumerable<int> sourceVertices, GeometryTolerance? tolerance = null)
    {
        EnsureQueryable(mesh, tolerance);
        ArgumentNullException.ThrowIfNull(sourceVertices);
        var sources = sourceVertices.Distinct().ToArray();
        if (sources.Length == 0) throw new ArgumentException("At least one source vertex is required.", nameof(sourceVertices));
        if (sources.Any(source => source < 0 || source >= mesh.VertexCount))
            throw new ArgumentOutOfRangeException(nameof(sourceVertices));

        var neighbours = BuildVertexNeighbours(mesh);
        var distances = Enumerable.Repeat(double.PositiveInfinity, mesh.VertexCount).ToArray();
        var predecessors = new int?[mesh.VertexCount];
        var queue = new PriorityQueue<int, double>();
        foreach (var source in sources)
        {
            distances[source] = 0;
            queue.Enqueue(source, 0);
        }

        while (queue.TryDequeue(out var current, out var currentDistance))
        {
            if (currentDistance != distances[current]) continue;
            foreach (var next in neighbours[current])
            {
                var candidate = currentDistance + Point3d.Distance(mesh[current], mesh[next]);
                if (candidate >= distances[next]) continue;
                distances[next] = candidate;
                predecessors[next] = current;
                queue.Enqueue(next, candidate);
            }
        }
        return new(distances, predecessors);
    }

    public static IReadOnlyList<double> BarycentricVertexAreas(TriangleMesh mesh)
    {
        var areas = new double[mesh.VertexCount];
        for (var i = 0; i < mesh.TriangleCount; i++)
        {
            var triangle = mesh.Triangles[i];
            var (a, b, c) = mesh.GetTriangle(i);
            var share = Point3d.Cross(b - a, c - a).Length / 6.0;
            areas[triangle.A] += share;
            areas[triangle.B] += share;
            areas[triangle.C] += share;
        }
        return areas;
    }

    private static Dictionary<(int Low, int High), int> EdgeCounts(TriangleMesh mesh)
    {
        var counts = new Dictionary<(int, int), int>();
        foreach (var triangle in mesh.Triangles)
        {
            AddEdgeCount(counts, triangle.A, triangle.B);
            AddEdgeCount(counts, triangle.B, triangle.C);
            AddEdgeCount(counts, triangle.C, triangle.A);
        }
        return counts;
    }

    private static IReadOnlyList<int>[] BuildVertexNeighbours(TriangleMesh mesh)
    {
        var sets = new HashSet<int>[mesh.VertexCount];
        for (var i = 0; i < sets.Length; i++) sets[i] = [];
        foreach (var triangle in mesh.Triangles)
        {
            AddNeighbour(sets, triangle.A, triangle.B);
            AddNeighbour(sets, triangle.B, triangle.C);
            AddNeighbour(sets, triangle.C, triangle.A);
        }
        return sets.Select(set => (IReadOnlyList<int>)set.ToArray()).ToArray();
    }

    private static void AddNeighbour(HashSet<int>[] sets, int left, int right)
    {
        sets[left].Add(right);
        sets[right].Add(left);
    }

    private static void AddEdgeCount(Dictionary<(int Low, int High), int> counts, int a, int b)
    {
        var edge = a < b ? (a, b) : (b, a);
        counts.TryGetValue(edge, out var count);
        counts[edge] = count + 1;
    }

    private static void AddCotanWeight(Dictionary<(int Low, int High), double> weights, int a, int b, double weight)
    {
        var edge = a < b ? (a, b) : (b, a);
        weights.TryGetValue(edge, out var current);
        weights[edge] = current + weight;
    }

    private static double Angle(Point3d left, Point3d right)
    {
        var denominator = Math.Sqrt(left.LengthSquared * right.LengthSquared);
        if (denominator == 0) return 0;
        return Math.Acos(Math.Clamp(Point3d.Dot(left, right) / denominator, -1.0, 1.0));
    }

    private static double Cotangent(Point3d left, Point3d right)
    {
        var crossLength = Point3d.Cross(left, right).Length;
        return crossLength == 0 ? 0 : Point3d.Dot(left, right) / crossLength;
    }

    private static void EnsureQueryable(TriangleMesh mesh, GeometryTolerance? tolerance)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        var health = MeshHealthAnalyzer.Analyse(mesh, tolerance);
        if (!health.IsQueryable)
            throw new ArgumentException("Surface analysis requires a mesh without degenerate or non-manifold triangles.", nameof(mesh));
    }
}
