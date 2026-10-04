using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using DDGCompanion.Core;

namespace DDGCompanion.Algorithms
{
    public class CotanLaplacian
    {
        public static SparseMatrix Build(Mesh mesh)
        {
            int n = mesh.Vertices.Count;
            var builder = new SparseMatrix(n, n);
            
            var diagonal = new double[n];
            foreach (var edge in mesh.Edges)
            {
                var v0 = edge.V0();
                var v1 = edge.V1();
                if (v0 == null || v1 == null) continue;
                double weight = edge.Cotan();
                if (!double.IsFinite(weight)) throw new InvalidOperationException("Non-finite cotangent weight.");
                builder[v0.Index, v1.Index] += weight;
                builder[v1.Index, v0.Index] += weight;
                diagonal[v0.Index] -= weight;
                diagonal[v1.Index] -= weight;
            }
            for (int i = 0; i < n; i++) builder[i, i] = diagonal[i];
            return builder;
        }
        
        public static SparseMatrix BuildMassMatrix(Mesh mesh)
        {
            int n = mesh.Vertices.Count;
            var builder = new SparseMatrix(n, n);
            
            foreach (var face in mesh.Faces)
            {
                double area = face.Area() / 3.0;
                foreach (var vertex in face.Vertices())
                    builder[vertex.Index, vertex.Index] += area;
            }
            
            return builder;
        }
        
        public static Matrix<double> SolveConstrained(
            Matrix<double> matrix,
            Matrix<double> rhs,
            IReadOnlyList<int> fixedVertices,
            Matrix<double> fixedValues)
        {
            if (matrix.RowCount != matrix.ColumnCount || matrix.RowCount != rhs.RowCount)
                throw new ArgumentException("Incompatible linear-system dimensions.");
            if (fixedVertices.Count != fixedValues.RowCount || rhs.ColumnCount != fixedValues.ColumnCount)
                throw new ArgumentException("Constraint dimensions do not match the right-hand side.");

            var a = Matrix<double>.Build.DenseOfMatrix(matrix);
            var b = rhs.Clone();
            for (int k = 0; k < fixedVertices.Count; k++)
            {
                int c = fixedVertices[k];
                if (c < 0 || c >= a.RowCount) throw new ArgumentOutOfRangeException(nameof(fixedVertices));
                for (int i = 0; i < a.RowCount; i++)
                    if (i != c)
                        for (int j = 0; j < b.ColumnCount; j++)
                            b[i, j] -= a[i, c] * fixedValues[k, j];
                a.SetRow(c, Vector<double>.Build.Dense(a.ColumnCount));
                a.SetColumn(c, Vector<double>.Build.Dense(a.RowCount));
                a[c, c] = 1.0;
                b.SetRow(c, fixedValues.Row(k));
            }
            return a.Solve(b);
        }

        public static Vector<double> SolveConstrained(
            Matrix<double> matrix,
            Vector<double> rhs,
            IReadOnlyList<int> fixedVertices,
            Vector<double> fixedValues)
        {
            var result = SolveConstrained(
                matrix, rhs.ToColumnMatrix(), fixedVertices, fixedValues.ToColumnMatrix());
            return result.Column(0);
        }
    }
}
