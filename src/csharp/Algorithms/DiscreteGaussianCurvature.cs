// DiscreteGaussianCurvature.cs
// Discrete Differential Geometry - Discrete Gaussian Curvature
// Added by Graph Technologies, 2025
// Description: Angle defect computation for vertex curvature

using System;
using System.Linq;
using System.Numerics;
using MathNet.Numerics.LinearAlgebra;
using DDGCompanion.Core;

namespace DDGCompanion.Algorithms
{
    public class DiscreteGaussianCurvature
    {
        /// <summary>
        /// Compute Gaussian curvature at each vertex using angle defect.
        /// K_i = 2π - Σ θ_ij where θ_ij are angles at vertex i.
        /// </summary>
        public static double[] Compute(Mesh mesh)
        {
            var K = new double[mesh.Vertices.Count];
            
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                var v = mesh.Vertices[i];
                double angleSum = 0.0;
                foreach (var he in v.OutgoingHalfEdges())
                {
                    if (he.Face != null && he.Vertex != null && he.Next?.Vertex != null)
                    {
                        // Compute angle at this vertex in the face
                        var d1 = he.Vertex.Position - v.Position;
                        var d2 = he.Next.Vertex.Position - v.Position;
                        if (d1.LengthSquared() > 1e-12f && d2.LengthSquared() > 1e-12f)
                        {
                            var e1 = Vector3.Normalize(d1);
                            var e2 = Vector3.Normalize(d2);
                        
                            float dotProduct = Vector3.Dot(e1, e2);
                            dotProduct = Math.Clamp(dotProduct, -1.0f, 1.0f);
                            double angle = Math.Acos(dotProduct);
                            angleSum += angle;
                        }
                    }
                }
                K[i] = (v.IsBoundary() ? Math.PI : 2.0 * Math.PI) - angleSum;
            }
            
            return K;
        }
        
        /// <summary>
        /// Compute total Gaussian curvature (should equal 2πχ by Gauss-Bonnet).
        /// </summary>
        public static double TotalCurvature(Mesh mesh)
        {
            var K = Compute(mesh);
            return K.Sum();
        }
        
        /// <summary>
        /// Verify Gauss-Bonnet theorem: Σ K_i = 2πχ.
        /// </summary>
        public static double GaussBonnetError(Mesh mesh)
        {
            double totalK = TotalCurvature(mesh);
            double expectedK = 2.0 * Math.PI * mesh.EulerCharacteristic();
            return Math.Abs(totalK - expectedK);
        }
    }
}
