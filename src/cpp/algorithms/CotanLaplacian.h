#pragma once
#include <Eigen/Sparse>
#include <vector>
#include "../core/Mesh.h"

class CotanLaplacian {
public:
    static Eigen::SparseMatrix<double> build(const Mesh& mesh);
    static Eigen::SparseMatrix<double> buildMassMatrix(const Mesh& mesh);
    static Eigen::VectorXd computeVertexAreas(const Mesh& mesh);
    static Eigen::MatrixXd solvePoisson(const Mesh& mesh, const Eigen::MatrixXd& F);
    static Eigen::MatrixXd solveConstrained(
        const Eigen::SparseMatrix<double>& A,
        const Eigen::MatrixXd& rhs,
        const std::vector<int>& fixedVertices,
        const Eigen::MatrixXd& fixedValues);
};
