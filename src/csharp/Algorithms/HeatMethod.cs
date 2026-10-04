// HeatMethod.cs
// Discrete Differential Geometry - Heat Method for Geodesic Distance
// Added by Graph Technologies, 2025
// Description: Fast geodesic computation via short-time heat diffusion

using System;
using System.Collections.Generic;
using System.Linq;
using Vector3 = System.Numerics.Vector3;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using DDGCompanion.Core;

namespace DDGCompanion.Algorithms
{
    public class HeatMethod
    {
        /// <summary>
        /// Compute geodesic distance from source vertices using heat method.
        /// Algorithm:
        ///   1. Diffuse heat from sources: (M - t*L)*u = δ_sources
        ///   2. Compute and normalize gradient: X = -∇u / |∇u|
        ///   3. Solve Poisson: Δφ = ∇·X
        /// </summary>
        public static double[] Compute(Mesh mesh, params int[] sourceVertices)
        {
            if (sourceVertices == null || sourceVertices.Length == 0)
                throw new ArgumentException("At least one source vertex is required.", nameof(sourceVertices));
            if (sourceVertices.Any(i => i < 0 || i >= mesh.Vertices.Count))
                throw new ArgumentOutOfRangeException(nameof(sourceVertices));
            double timestep = ComputeTimestep(mesh);
            
            // Step 1: Heat diffusion
            var u = SolveHeatFlow(mesh, sourceVertices, timestep);
            
            // Step 2: Compute integrated divergence of normalized gradient
            var div = ComputeIntegratedDivergence(mesh, u);
            
            // Step 3: Solve for distance
            var phi = SolveDistance(mesh, div);
            
            // Shift so minimum is zero
            double minVal = sourceVertices.Select(i => phi[i]).Min();
            for (int i = 0; i < phi.Length; i++)
                phi[i] -= minVal;
            
            return phi;
        }
        
        private static double ComputeTimestep(Mesh mesh)
        {
            // Timestep = mean edge length squared
            if (mesh.Edges.Count == 0) throw new ArgumentException("Heat method requires at least one edge.", nameof(mesh));
            double meanLength = mesh.Edges.Average(e => e.Length());
            return meanLength * meanLength;
        }
        
        private static Vector<double> SolveHeatFlow(Mesh mesh, int[] sources, double timestep)
        {
            var L = CotanLaplacian.Build(mesh);
            var M = CotanLaplacian.BuildMassMatrix(mesh);
            
            // System: (M - t*L)*u = M*δ_sources
            var A = M.Subtract(L.Multiply(timestep));
            
            var rhs = Vector<double>.Build.Dense(mesh.Vertices.Count);
            foreach (int src in sources)
                rhs[src] = 1.0;
            rhs = M.Multiply(rhs);
            
            return A.Solve(rhs);
        }
        
        private static Vector<double> ComputeIntegratedDivergence(Mesh mesh, Vector<double> u)
        {
            var div = Vector<double>.Build.Dense(mesh.Vertices.Count);
            
            foreach (var face in mesh.Faces)
            {
                var verts = face.Vertices();
                if (verts.Count != 3) continue;

                var p0 = verts[0].Position;
                var p1 = verts[1].Position;
                var p2 = verts[2].Position;
                var e1 = p1 - p0;
                var e2 = p2 - p0;
                var cross = Vector3.Cross(e1, e2);
                double area = 0.5 * cross.Length();
                if (area <= 1e-12) continue;
                var normal = Vector3.Normalize(cross);
                var gradient =
                    (float)((u[verts[1].Index] - u[verts[0].Index]) / (2.0 * area)) * Vector3.Cross(e2, normal) +
                    (float)((u[verts[2].Index] - u[verts[0].Index]) / (2.0 * area)) * Vector3.Cross(normal, e1);
                if (gradient.LengthSquared() <= 1e-20f) continue;
                var field = -Vector3.Normalize(gradient);
                var gradients = new[]
                {
                    Vector3.Cross(normal, p2 - p1) / (float)(2.0 * area),
                    Vector3.Cross(normal, p0 - p2) / (float)(2.0 * area),
                    Vector3.Cross(normal, p1 - p0) / (float)(2.0 * area)
                };
                for (int i = 0; i < 3; i++)
                    div[verts[i].Index] -= area * Vector3.Dot(gradients[i], field);
            }
            
            return div;
        }
        
        private static double[] SolveDistance(Mesh mesh, Vector<double> divergence)
        {
            var L = CotanLaplacian.Build(mesh);
            var phi = CotanLaplacian.SolveConstrained(
                L,
                divergence,
                new[] { 0 },
                Vector<double>.Build.Dense(1));
            return phi.ToArray();
        }
    }
}
