namespace GeometryKit.Mesh;

public enum MeshIssueSeverity { Information, Warning, Error }

public sealed record MeshIssue(MeshIssueSeverity Severity, string Code, string Message, int? TriangleIndex = null, int? VertexIndex = null);

public sealed record MeshHealthReport(
    int VertexCount,
    int TriangleCount,
    int EdgeCount,
    int BoundaryEdgeCount,
    int NonManifoldEdgeCount,
    int DegenerateTriangleCount,
    int ConnectedComponentCount,
    double SurfaceArea,
    IReadOnlyList<MeshIssue> Issues)
{
    public bool IsQueryable => DegenerateTriangleCount == 0 && NonManifoldEdgeCount == 0;
    public bool IsClosed => TriangleCount > 0 && BoundaryEdgeCount == 0 && NonManifoldEdgeCount == 0;
    public bool HasErrors => Issues.Any(issue => issue.Severity == MeshIssueSeverity.Error);
}
