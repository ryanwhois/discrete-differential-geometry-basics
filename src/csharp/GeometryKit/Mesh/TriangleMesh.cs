using GeometryKit.Core;

namespace GeometryKit.Mesh;

/// <summary>
/// Immutable indexed triangle mesh. Construction rejects invalid indices; mesh quality is
/// reported separately so callers can decide whether repair is acceptable.
/// </summary>
public sealed class TriangleMesh
{
    private readonly Point3d[] _vertices;
    private readonly IndexedTriangle[] _triangles;

    public TriangleMesh(IEnumerable<Point3d> vertices, IEnumerable<IndexedTriangle> triangles)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentNullException.ThrowIfNull(triangles);
        _vertices = vertices.ToArray();
        _triangles = triangles.ToArray();

        for (var i = 0; i < _triangles.Length; i++)
        {
            foreach (var vertex in _triangles[i].Vertices)
            {
                if (vertex < 0 || vertex >= _vertices.Length)
                    throw new ArgumentOutOfRangeException(nameof(triangles),
                        $"Triangle {i} references vertex {vertex}, but the vertex count is {_vertices.Length}.");
            }
        }
    }

    public IReadOnlyList<Point3d> Vertices => _vertices;
    public IReadOnlyList<IndexedTriangle> Triangles => _triangles;
    public int VertexCount => _vertices.Length;
    public int TriangleCount => _triangles.Length;
    public Point3d this[int index] => _vertices[index];

    public (Point3d A, Point3d B, Point3d C) GetTriangle(int triangleIndex)
    {
        var triangle = _triangles[triangleIndex];
        return (_vertices[triangle.A], _vertices[triangle.B], _vertices[triangle.C]);
    }

    public Aabb3d Bounds
    {
        get
        {
            if (_vertices.Length == 0) return Aabb3d.Empty;
            var result = Aabb3d.FromPoint(_vertices[0]);
            for (var i = 1; i < _vertices.Length; i++) result = result.Encapsulate(_vertices[i]);
            return result;
        }
    }
}
