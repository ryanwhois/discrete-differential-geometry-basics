// Edge.cs
// Discrete Differential Geometry - Edge Structure
// Added by Graph Technologies, 2025

using System;
using System.Numerics;

namespace DDGCompanion.Core
{
    public class Edge
    {
        public HalfEdge? HalfEdge { get; set; }
        public int Index { get; set; } = -1;
        
        public Vertex? V0() => HalfEdge?.Twin?.Vertex;
        public Vertex? V1() => HalfEdge?.Vertex;
        
        public bool IsBoundary() => HalfEdge?.Face == null || HalfEdge?.Twin?.Face == null;
        
        public float Length()
        {
            if (HalfEdge?.Vertex == null || HalfEdge?.Twin?.Vertex == null)
                return 0;
            return Vector3.Distance(HalfEdge.Vertex.Position, HalfEdge.Twin.Vertex.Position);
        }
        
        public double Cotan()
        {
            double cotSum = 0.0;
            if (HalfEdge == null || HalfEdge.Twin == null) return 0.0;
            foreach (var he in new[] { HalfEdge!, HalfEdge.Twin! })
            {
                if (he.Face == null || he.Next?.Vertex == null || he.Source() == null || he.Vertex == null) continue;
                var opposite = he.Next.Vertex.Position;
                var a = he.Source()!.Position - opposite;
                var b = he.Vertex.Position - opposite;
                float cross = Vector3.Cross(a, b).Length();
                if (cross > 1e-12f) cotSum += Vector3.Dot(a, b) / cross;
            }
            return cotSum / 2.0;
        }
        
        public Vector3 Midpoint()
        {
            if (HalfEdge?.Vertex == null || HalfEdge?.Twin?.Vertex == null)
                return Vector3.Zero;
            return 0.5f * (HalfEdge.Vertex.Position + HalfEdge.Twin.Vertex.Position);
        }
    }
}
