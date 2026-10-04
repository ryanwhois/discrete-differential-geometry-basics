// CurvatureTests.cs
// Unit tests for curvature computation
// Added by Graph Technologies, 2025

using System;
using System.Numerics;
using Xunit;
using DDGCompanion.Core;
using DDGCompanion.Algorithms;

namespace DDGCompanion.Tests
{
    public class CurvatureTests
    {
        [Fact]
        public void TestGaussBonnetTheorem()
        {
            // Test on tetrahedron: total curvature should equal 2πχ
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
            
            double totalK = DiscreteGaussianCurvature.TotalCurvature(mesh);
            double expectedK = 2.0 * Math.PI * mesh.EulerCharacteristic();
            
            // Should satisfy Gauss-Bonnet
            Assert.True(Math.Abs(totalK - expectedK) < 0.1); // Allow small numerical error
        }
        
        [Fact]
        public void TestGaussBonnetError()
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
            
            double error = DiscreteGaussianCurvature.GaussBonnetError(mesh);
            
            // Error should be small
            Assert.True(error < 0.1);
        }

        [Fact]
        public void TestGaussBonnetForOpenDisk()
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
            Assert.True(DiscreteGaussianCurvature.GaussBonnetError(mesh) < 1e-6);
        }
    }
}
