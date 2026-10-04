// Mesh.cs
// Discrete Differential Geometry - Halfedge Mesh Data Structure
// Added by Graph Technologies, 2025
// Description: Complete halfedge mesh implementation for .NET

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

namespace DDGCompanion.Core
{
    public class Mesh
    {
        public List<Vertex> Vertices { get; set; } = new();
        public List<Edge> Edges { get; set; } = new();
        public List<Face> Faces { get; set; } = new();
        public List<HalfEdge> HalfEdges { get; set; } = new();
        
        public void Build(Vector3[] positions, int[,] faceIndices)
        {
            if (positions == null) throw new ArgumentNullException(nameof(positions));
            if (faceIndices == null) throw new ArgumentNullException(nameof(faceIndices));
            if (faceIndices.GetLength(1) != 3)
                throw new ArgumentException("Mesh.Build currently supports triangle faces only.", nameof(faceIndices));

            Vertices.Clear();
            Edges.Clear();
            Faces.Clear();
            HalfEdges.Clear();
            
            // Create vertices
            for (int i = 0; i < positions.Length; i++)
            {
                Vertices.Add(new Vertex(positions[i]) { Index = i });
            }
            
            // Track halfedges by directed edge key and preserve endpoints.
            var halfedgeMap = new Dictionary<(int, int), HalfEdge>();
            var halfedgeEndpoints = new List<(int Source, int Target)>();
            var undirectedCounts = new Dictionary<(int, int), int>();
            
            // Create faces and halfedges
            for (int i = 0; i < faceIndices.GetLength(0); i++)
            {
                var face = new Face { Index = i };
                var faceHalfEdges = new List<HalfEdge>();
                
                for (int j = 0; j < 3; j++)
                {
                    int v0 = faceIndices[i, j];
                    int v1 = faceIndices[i, (j + 1) % 3];

                    if (v0 < 0 || v0 >= Vertices.Count || v1 < 0 || v1 >= Vertices.Count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(faceIndices), $"Face {i} contains invalid vertex index.");
                    }
                    if (v0 == v1)
                    {
                        throw new ArgumentException($"Face {i} contains a degenerate edge ({v0} -> {v1}).", nameof(faceIndices));
                    }
                    if (halfedgeMap.ContainsKey((v0, v1)))
                        throw new ArgumentException($"Duplicate directed edge {v0} -> {v1}. Check manifoldness and winding.", nameof(faceIndices));
                    var undirected = v0 < v1 ? (v0, v1) : (v1, v0);
                    undirectedCounts.TryGetValue(undirected, out int incidentCount);
                    if (incidentCount >= 2)
                        throw new ArgumentException($"Non-manifold edge {undirected.Item1}--{undirected.Item2}.", nameof(faceIndices));
                    undirectedCounts[undirected] = incidentCount + 1;
                    
                    var he = new HalfEdge
                    {
                        Vertex = Vertices[v1],
                        Face = face,
                        Index = HalfEdges.Count
                    };
                    
                    faceHalfEdges.Add(he);
                    halfedgeMap[(v0, v1)] = he;
                    halfedgeEndpoints.Add((v0, v1));
                    HalfEdges.Add(he);
                }
                
                // Set next pointers
                for (int j = 0; j < 3; j++)
                {
                    faceHalfEdges[j].Next = faceHalfEdges[(j + 1) % 3];
                }
                
                face.HalfEdge = faceHalfEdges[0];
                Faces.Add(face);
            }
            
            // Pair interior twins, add explicit boundary twins, and create every edge.
            int interiorHalfEdgeCount = HalfEdges.Count;
            for (int i = 0; i < interiorHalfEdgeCount; i++)
            {
                var he = HalfEdges[i];
                var (source, target) = halfedgeEndpoints[i];
                if (he.Edge != null) continue;
                if (halfedgeMap.TryGetValue((target, source), out var twin))
                {
                    he.Twin = twin;
                    twin.Twin = he;
                }
                else
                {
                    twin = new HalfEdge
                    {
                        Vertex = Vertices[source],
                        Twin = he,
                        Index = HalfEdges.Count
                    };
                    he.Twin = twin;
                    HalfEdges.Add(twin);
                }

                var edge = new Edge { HalfEdge = he, Index = Edges.Count };
                he.Edge = edge;
                twin.Edge = edge;
                Edges.Add(edge);
            }

            // Each manifold boundary vertex has one outgoing boundary halfedge.
            var boundaryBySource = new Dictionary<int, HalfEdge>();
            foreach (var he in HalfEdges.Where(h => h.Face == null))
            {
                int source = he.Source()!.Index;
                if (!boundaryBySource.TryAdd(source, he))
                    throw new ArgumentException($"Boundary vertex {source} has multiple outgoing boundary edges.", nameof(faceIndices));
            }
            foreach (var he in boundaryBySource.Values)
            {
                if (!boundaryBySource.TryGetValue(he.Vertex!.Index, out var next))
                    throw new ArgumentException("Boundary chain could not be closed.", nameof(faceIndices));
                he.Next = next;
            }

            // Set outgoing representatives, preferring boundary halfedges.
            foreach (var he in HalfEdges)
            {
                var source = he.Source();
                if (source != null && (source.HalfEdge == null || he.Face == null))
                    source.HalfEdge = he;
            }

            var errors = Validate();
            if (errors.Count > 0) throw new ArgumentException(errors[0], nameof(faceIndices));
        }

        public List<List<Vertex>> BoundaryLoops()
        {
            var loops = new List<List<Vertex>>();
            var visited = new HashSet<int>();
            foreach (var start in HalfEdges.Where(h => h.Face == null))
            {
                if (visited.Contains(start.Index)) continue;
                var loop = new List<Vertex>();
                var he = start;
                while (he != null && visited.Add(he.Index))
                {
                    if (he.Vertex == null) break;
                    loop.Add(he.Vertex);
                    he = he.Next;
                    if (he == start)
                    {
                        loops.Add(loop);
                        break;
                    }
                }
            }
            return loops;
        }

        public List<string> Validate()
        {
            var errors = new List<string>();
            foreach (var he in HalfEdges)
            {
                if (he.Vertex == null || he.Edge == null || he.Twin == null || he.Next == null)
                    errors.Add($"Halfedge {he.Index} has an incomplete topology.");
                else if (he.Twin.Twin != he)
                    errors.Add($"Halfedge {he.Index} has a non-symmetric twin.");
                else if (he.Edge != he.Twin.Edge)
                    errors.Add($"Halfedge {he.Index} and its twin do not share an edge.");
            }
            foreach (var face in Faces)
            {
                var vertices = face.Vertices();
                if (vertices.Count != 3)
                {
                    errors.Add($"Face {face.Index} is degenerate or non-triangular.");
                    continue;
                }

                var e01 = vertices[1].Position - vertices[0].Position;
                var e02 = vertices[2].Position - vertices[0].Position;
                var e12 = vertices[2].Position - vertices[1].Position;
                float maxEdgeSquared = MathF.Max(e01.LengthSquared(), MathF.Max(e02.LengthSquared(), e12.LengthSquared()));
                float twiceArea = Vector3.Cross(e01, e02).Length();
                if (maxEdgeSquared == 0.0f || twiceArea <= 1e-6f * maxEdgeSquared)
                    errors.Add($"Face {face.Index} is degenerate or non-triangular.");
            }
            return errors;
        }
        
        public int EulerCharacteristic() => Vertices.Count - Edges.Count + Faces.Count;
        
        public Vector3[] GetPositions()
        {
            return Vertices.Select(v => v.Position).ToArray();
        }
        
        public void SetPositions(Vector3[] positions)
        {
            for (int i = 0; i < Vertices.Count; i++)
            {
                Vertices[i].Position = positions[i];
            }
        }
        
        public void Center()
        {
            if (Vertices.Count == 0) return;
            var centroid = Vector3.Zero;
            foreach (var v in Vertices)
                centroid += v.Position;
            centroid /= Vertices.Count;
            
            foreach (var v in Vertices)
                v.Position -= centroid;
        }
        
        public void Normalize()
        {
            Center();
            
            float maxDist = 0;
            foreach (var v in Vertices)
                maxDist = Math.Max(maxDist, v.Position.Length());
            
            if (maxDist > 0)
            {
                foreach (var v in Vertices)
                    v.Position /= maxDist;
            }
        }
    }
}
