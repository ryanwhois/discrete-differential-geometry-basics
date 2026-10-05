using System;
using System.Collections.Generic;
using System.Numerics;

namespace DDGCompanion.Core
{
    public class Vertex
    {
        public Vector3 Position { get; set; }
        public HalfEdge? HalfEdge { get; set; }
        public int Index { get; set; } = -1;
        
        public Vertex(Vector3 position)
        {
            Position = position;
        }
        
        public List<Vertex> Star()
        {
            var neighbors = new List<Vertex>();
            if (HalfEdge == null) return neighbors;
            
            var he = HalfEdge;
            var visited = new HashSet<int>();
            while (he != null && visited.Add(he.Index))
            {
                if (he.Vertex != null)
                {
                    neighbors.Add(he.Vertex);
                }

                if (he.Twin?.Next == null) break;
                he = he.Twin.Next;
                if (he == HalfEdge) break;
            }
            
            return neighbors;
        }
        
        public int Degree()
        {
            if (HalfEdge == null) return 0;
            int count = 0;
            var he = HalfEdge;
            var visited = new HashSet<int>();
            while (he != null && visited.Add(he.Index))
            {
                count++;

                if (he.Twin?.Next == null) break;
                he = he.Twin.Next;
                if (he == HalfEdge) break;
            }
            return count;
        }
        
        public bool IsBoundary()
        {
            if (HalfEdge == null) return true;
            var he = HalfEdge;
            var visited = new HashSet<int>();
            while (he != null && visited.Add(he.Index))
            {
                if (he.Face == null || he.Twin == null || he.Twin.Face == null || he.Twin.Next == null) return true;
                he = he.Twin.Next;
                if (he == HalfEdge) break;
            }
            return false;
        }

        public List<HalfEdge> OutgoingHalfEdges()
        {
            var result = new List<HalfEdge>();
            if (HalfEdge == null) return result;
            var he = HalfEdge;
            var visited = new HashSet<int>();
            while (he != null && visited.Add(he.Index))
            {
                result.Add(he);
                he = he.Twin?.Next;
            }
            return result;
        }

        public List<Face> AdjacentFaces()
        {
            var result = new List<Face>();
            foreach (var he in OutgoingHalfEdges())
                if (he.Face != null) result.Add(he.Face);
            return result;
        }
    }
}
