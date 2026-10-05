using System.Collections.Generic;
using System.Numerics;
using System.Linq;

namespace DDGCompanion.Core
{
    public class Face
    {
        public HalfEdge? HalfEdge { get; set; }
        public int Index { get; set; } = -1;
        
        public List<Vertex> Vertices()
        {
            var verts = new List<Vertex>();
            if (HalfEdge == null) return verts;
            var he = HalfEdge;
            do
            {
                verts.Add(he.Vertex!);
                he = he.Next!;
            } while (he != HalfEdge);
            return verts;
        }
        
        public double Area()
        {
            var verts = Vertices();
            if (verts.Count != 3) return 0;
            var e1 = verts[1].Position - verts[0].Position;
            var e2 = verts[2].Position - verts[0].Position;
            return 0.5f * Vector3.Cross(e1, e2).Length();
        }
        
        public Vector3 Normal()
        {
            var verts = Vertices();
            if (verts.Count != 3) return Vector3.Zero;
            var e1 = verts[1].Position - verts[0].Position;
            var e2 = verts[2].Position - verts[0].Position;
            var cross = Vector3.Cross(e1, e2);
            return cross.LengthSquared() > 1e-20f ? Vector3.Normalize(cross) : Vector3.Zero;
        }
    }
}
