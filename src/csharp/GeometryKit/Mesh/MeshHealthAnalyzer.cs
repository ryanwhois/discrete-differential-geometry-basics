using GeometryKit.Core;

namespace GeometryKit.Mesh;

public static class MeshHealthAnalyzer
{
    public static MeshHealthReport Analyse(TriangleMesh mesh, GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        var tol = tolerance ?? GeometryTolerance.Default;
        var edges = new Dictionary<(int Low, int High), List<int>>();
        var vertexTriangles = new List<int>[mesh.VertexCount];
        for (var i = 0; i < mesh.VertexCount; i++) vertexTriangles[i] = [];
        var issues = new List<MeshIssue>();
        var degenerate = 0;
        var surfaceArea = 0.0;

        for (var faceIndex = 0; faceIndex < mesh.TriangleCount; faceIndex++)
        {
            var triangle = mesh.Triangles[faceIndex];
            foreach (var vertex in triangle.Vertices) vertexTriangles[vertex].Add(faceIndex);
            if (triangle.HasRepeatedVertex)
            {
                degenerate++;
                issues.Add(new(MeshIssueSeverity.Error, "repeated-index", "Triangle repeats a vertex index.", faceIndex));
                continue;
            }

            var (a, b, c) = mesh.GetTriangle(faceIndex);
            var twiceArea = Point3d.Cross(b - a, c - a).Length;
            var scale = Math.Max(Point3d.Distance(a, b), Math.Max(Point3d.Distance(b, c), Point3d.Distance(c, a)));
            if (twiceArea <= tol.ForScale(scale) * scale)
            {
                degenerate++;
                issues.Add(new(MeshIssueSeverity.Error, "degenerate-triangle", "Triangle has area below the configured tolerance.", faceIndex));
            }
            else surfaceArea += twiceArea * 0.5;

            AddEdge(edges, triangle.A, triangle.B, faceIndex);
            AddEdge(edges, triangle.B, triangle.C, faceIndex);
            AddEdge(edges, triangle.C, triangle.A, faceIndex);
        }

        var boundary = 0;
        var nonManifold = 0;
        foreach (var (edge, incidents) in edges)
        {
            if (incidents.Count == 1) boundary++;
            else if (incidents.Count > 2)
            {
                nonManifold++;
                issues.Add(new(MeshIssueSeverity.Error, "non-manifold-edge",
                    $"Edge {edge.Low}--{edge.High} is incident to {incidents.Count} triangles.", incidents[0]));
            }
        }

        for (var vertex = 0; vertex < vertexTriangles.Length; vertex++)
            if (vertexTriangles[vertex].Count == 0)
                issues.Add(new(MeshIssueSeverity.Warning, "isolated-vertex", "Vertex is not used by a triangle.", VertexIndex: vertex));

        var components = CountFaceComponents(mesh, edges);
        return new(mesh.VertexCount, mesh.TriangleCount, edges.Count, boundary, nonManifold, degenerate, components, surfaceArea, issues);
    }

    private static void AddEdge(Dictionary<(int Low, int High), List<int>> edges, int a, int b, int faceIndex)
    {
        var key = a < b ? (a, b) : (b, a);
        if (!edges.TryGetValue(key, out var incidents)) edges[key] = incidents = [];
        incidents.Add(faceIndex);
    }

    private static int CountFaceComponents(TriangleMesh mesh, Dictionary<(int Low, int High), List<int>> edges)
    {
        if (mesh.TriangleCount == 0) return 0;
        var neighbours = new List<int>[mesh.TriangleCount];
        for (var i = 0; i < neighbours.Length; i++) neighbours[i] = [];
        foreach (var incidents in edges.Values.Where(list => list.Count == 2))
        {
            neighbours[incidents[0]].Add(incidents[1]);
            neighbours[incidents[1]].Add(incidents[0]);
        }

        var visited = new bool[mesh.TriangleCount];
        var count = 0;
        for (var start = 0; start < visited.Length; start++)
        {
            if (visited[start]) continue;
            count++;
            var queue = new Queue<int>();
            queue.Enqueue(start);
            visited[start] = true;
            while (queue.TryDequeue(out var current))
                foreach (var next in neighbours[current].Where(next => !visited[next]))
                {
                    visited[next] = true;
                    queue.Enqueue(next);
                }
        }
        return count;
    }
}
