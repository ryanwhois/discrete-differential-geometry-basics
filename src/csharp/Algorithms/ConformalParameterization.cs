// ConformalParameterization.cs
// Discrete Differential Geometry - Conformal Parameterization
// Added by Graph Technologies, 2025
// Description: Angle-preserving surface flattening using spectral and boundary methods

using System;
using System.Linq;
using System.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using DDGCompanion.Core;

namespace DDGCompanion.Algorithms
{
    public class ConformalParameterization
    {
        /// <summary>
        /// Spectral conformal parameterization using Laplacian eigenvectors.
        /// Returns 2D UV coordinates for each vertex.
        /// </summary>
        public static Vector2[] Spectral(Mesh mesh)
        {
            var L = CotanLaplacian.Build(mesh);
            var M = CotanLaplacian.BuildMassMatrix(mesh);
            
            // Solve generalized eigenvalue problem: L*phi = lambda*M*phi
            // Use 2nd and 3rd eigenvectors (1st is constant)
            var evd = L.Evd();
            
            var uv = new Vector2[mesh.Vertices.Count];
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                // Use eigenvectors 1 and 2 (skip 0 which is constant)
                uv[i] = new Vector2(
                    (float)evd.EigenVectors.Column(1)[i],
                    (float)evd.EigenVectors.Column(2)[i]
                );
            }
            
            return uv;
        }
        
        /// <summary>
        /// Map surface boundary to unit circle, solve for interior.
        /// Minimizes Dirichlet energy with fixed boundary.
        /// </summary>
        public static Vector2[] BoundaryCircle(Mesh mesh)
        {
            var L = CotanLaplacian.Build(mesh);
            int n = mesh.Vertices.Count;
            
            // Find boundary vertices
            var boundary = mesh.BoundaryLoops()
                .OrderByDescending(loop => loop.Count)
                .FirstOrDefault();
            
            if (boundary == null || boundary.Count == 0)
            {
                throw new InvalidOperationException("Mesh has no boundary - use spectral method");
            }
            
            // Map boundary to circle
            var boundaryVerts = boundary.Select(v => v.Index).ToList();
            var boundaryValues = Matrix<double>.Build.Dense(boundary.Count, 2);
            double[] cumulative = new double[boundary.Count + 1];
            for (int i = 0; i < boundary.Count; i++)
                cumulative[i + 1] = cumulative[i] + Vector3.Distance(boundary[i].Position, boundary[(i + 1) % boundary.Count].Position);
            if (cumulative[^1] <= 1e-12) throw new InvalidOperationException("Boundary perimeter is degenerate.");
            for (int i = 0; i < boundary.Count; i++)
            {
                double angle = 2.0 * Math.PI * cumulative[i] / cumulative[^1];
                boundaryValues[i, 0] = Math.Cos(angle);
                boundaryValues[i, 1] = Math.Sin(angle);
            }
            var solved = CotanLaplacian.SolveConstrained(
                L,
                Matrix<double>.Build.Dense(n, 2),
                boundaryVerts,
                boundaryValues);
            var uv = new Vector2[n];
            for (int i = 0; i < n; i++) uv[i] = new Vector2((float)solved[i, 0], (float)solved[i, 1]);
            return uv;
        }
        
        /// <summary>
        /// Compute Dirichlet energy (conformal distortion measure).
        /// E(f) = ∫|∇f|²
        /// </summary>
        public static double DirichletEnergy(Mesh mesh, Vector2[] uv)
        {
            double energy = 0.0;
            
            foreach (var face in mesh.Faces)
            {
                var verts = face.Vertices();
                if (verts.Count != 3) continue;
                
                // 3D triangle
                var p0 = verts[0].Position;
                var p1 = verts[1].Position;
                var p2 = verts[2].Position;
                float area3D = 0.5f * Vector3.Cross(p1 - p0, p2 - p0).Length();
                
                // 2D parameter space
                var uv0 = uv[verts[0].Index];
                var uv1 = uv[verts[1].Index];
                var uv2 = uv[verts[2].Index];
                
                var g1 = uv1 - uv0;
                var g2 = uv2 - uv0;
                double gradNorm = g1.LengthSquared() + g2.LengthSquared();
                
                energy += area3D * gradNorm;
            }
            
            return energy;
        }
    }
}
