// MeshTests.cs
// Unit tests for Mesh data structures
// Added by Graph Technologies, 2025

using System;
using System.Numerics;
using Xunit;
using DDGCompanion.Core;

namespace DDGCompanion.Tests
{
    public class MeshTests
    {
        private Mesh CreateTetrahedron()
        {
            var positions = new Vector3[]
            {
                new(0, 0, 0),
                new(1, 0, 0),
                new(0, 1, 0),
                new(0, 0, 1)
            };
            
            var faces = new int[,]
            {
                { 0, 2, 1 },
                { 0, 1, 3 },
                { 0, 3, 2 },
                { 1, 2, 3 }
            };
            
            var mesh = new Mesh();
            mesh.Build(positions, faces);
            return mesh;
        }
        
        [Fact]
        public void TestMeshConstruction()
        {
            var mesh = CreateTetrahedron();
            
            Assert.Equal(4, mesh.Vertices.Count);
            Assert.Equal(6, mesh.Edges.Count);
            Assert.Equal(4, mesh.Faces.Count);
        }
        
        [Fact]
        public void TestEulerCharacteristic()
        {
            var mesh = CreateTetrahedron();
            Assert.Equal(2, mesh.EulerCharacteristic());
        }
        
        [Fact]
        public void TestHalfedgeConnectivity()
        {
            var mesh = CreateTetrahedron();
            
            foreach (var he in mesh.HalfEdges)
            {
                // Twin symmetry
                Assert.Same(he, he.Twin!.Twin);
                
                // Cycle property for triangular face
                Assert.Same(he, he.Next!.Next!.Next);
                
                // Edge consistency
                Assert.Same(he.Edge, he.Twin.Edge);
            }
        }
        
        [Fact]
        public void TestVertexDegree()
        {
            var mesh = CreateTetrahedron();
            
            // All vertices in tetrahedron have degree 3
            foreach (var v in mesh.Vertices)
            {
                Assert.Equal(3, v.Degree());
            }
        }
        
        [Fact]
        public void TestCenterMesh()
        {
            var mesh = CreateTetrahedron();
            mesh.Center();
            
            var centroid = Vector3.Zero;
            foreach (var v in mesh.Vertices)
                centroid += v.Position;
            centroid /= mesh.Vertices.Count;
            
            Assert.True(centroid.Length() < 1e-6f);
        }
        
        [Fact]
        public void TestNormalizeMesh()
        {
            var mesh = CreateTetrahedron();
            mesh.Normalize();
            
            float maxDist = 0;
            foreach (var v in mesh.Vertices)
                maxDist = Math.Max(maxDist, v.Position.Length());
            
            Assert.True(Math.Abs(maxDist - 1.0f) < 1e-6f);
        }

        [Fact]
        public void TestBuildRejectsInvalidFaceIndex()
        {
            var positions = new Vector3[]
            {
                new(0, 0, 0),
                new(1, 0, 0),
                new(0, 1, 0)
            };

            var faces = new int[,]
            {
                { 0, 1, 3 }
            };

            var mesh = new Mesh();
            Assert.Throws<ArgumentOutOfRangeException>(() => mesh.Build(positions, faces));
        }

        [Fact]
        public void TestOpenMeshBuildsExplicitBoundaryLoop()
        {
            var positions = new[]
            {
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(1, 1, 0),
                new Vector3(0, 1, 0)
            };
            var faces = new int[,] { { 0, 1, 2 }, { 0, 2, 3 } };
            var mesh = new Mesh();
            mesh.Build(positions, faces);

            Assert.Equal(5, mesh.Edges.Count);
            Assert.Equal(10, mesh.HalfEdges.Count);
            Assert.Equal(1, mesh.EulerCharacteristic());
            Assert.Single(mesh.BoundaryLoops());
            Assert.Equal(4, mesh.BoundaryLoops()[0].Count);
            Assert.Empty(mesh.Validate());
            Assert.All(mesh.Vertices, vertex => Assert.True(vertex.IsBoundary()));
        }

        [Fact]
        public void TestBuildRejectsDuplicateDirectedEdge()
        {
            var positions = new[]
            {
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(0, 1, 0),
                new Vector3(0, -1, 0)
            };
            var faces = new int[,] { { 0, 1, 2 }, { 0, 1, 3 } };
            var mesh = new Mesh();
            Assert.Throws<ArgumentException>(() => mesh.Build(positions, faces));
        }

        [Fact]
        public void TestValidationIsScaleRelative()
        {
            var positions = new[]
            {
                new Vector3(0, 0, 0),
                new Vector3(1e-8f, 0, 0),
                new Vector3(0, 1e-8f, 0)
            };
            var faces = new int[,] { { 0, 1, 2 } };
            var mesh = new Mesh();

            mesh.Build(positions, faces);

            Assert.Empty(mesh.Validate());
        }
    }
}
