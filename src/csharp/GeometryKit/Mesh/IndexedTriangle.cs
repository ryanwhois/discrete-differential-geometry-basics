namespace GeometryKit.Mesh;

public readonly record struct IndexedTriangle(int A, int B, int C)
{
    public IEnumerable<int> Vertices
    {
        get
        {
            yield return A;
            yield return B;
            yield return C;
        }
    }

    public bool HasRepeatedVertex => A == B || B == C || C == A;
}
