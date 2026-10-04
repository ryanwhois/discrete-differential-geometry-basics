#include "CotanLaplacian.h"
#include <Eigen/SparseLU>
#include <cmath>
#include <stdexcept>

Eigen::SparseMatrix<double> CotanLaplacian::build(const Mesh& mesh) {
    int n = mesh.numVertices();
    Eigen::SparseMatrix<double> L(n, n);
    std::vector<Eigen::Triplet<double>> triplets;
    
    Eigen::VectorXd diagonal = Eigen::VectorXd::Zero(n);
    for (const auto& edge : mesh.edges) {
        const int i = edge->v0()->index;
        const int j = edge->v1()->index;
        const double w = edge->cotan();
        if (!std::isfinite(w)) throw std::runtime_error("Non-finite cotangent weight");
        triplets.push_back({i, j, w});
        triplets.push_back({j, i, w});
        diagonal(i) -= w;
        diagonal(j) -= w;
    }
    for (int i = 0; i < n; ++i) triplets.push_back({i, i, diagonal(i)});
    L.setFromTriplets(triplets.begin(), triplets.end());
    return L;
}

Eigen::SparseMatrix<double> CotanLaplacian::buildMassMatrix(const Mesh& mesh) {
    auto areas = computeVertexAreas(mesh);
    Eigen::SparseMatrix<double> M(mesh.numVertices(), mesh.numVertices());
    std::vector<Eigen::Triplet<double>> triplets;
    for (int i = 0; i < areas.size(); i++)
        triplets.push_back({i, i, areas(i)});
    M.setFromTriplets(triplets.begin(), triplets.end());
    return M;
}

Eigen::VectorXd CotanLaplacian::computeVertexAreas(const Mesh& mesh) {
    Eigen::VectorXd areas = Eigen::VectorXd::Zero(mesh.numVertices());
    for (const auto& f : mesh.faces) {
        double A = f->area() / 3.0;
        for (auto v : f->vertices())
            areas(v->index) += A;
    }
    return areas;
}

Eigen::MatrixXd CotanLaplacian::solvePoisson(const Mesh& mesh, const Eigen::MatrixXd& F) {
    if (mesh.numVertices() == 0) return Eigen::MatrixXd(0, F.cols());
    auto L = build(mesh);
    auto M = buildMassMatrix(mesh);
    return solveConstrained(L, M * F, {0}, Eigen::MatrixXd::Zero(1, F.cols()));
}

Eigen::MatrixXd CotanLaplacian::solveConstrained(
    const Eigen::SparseMatrix<double>& A,
    const Eigen::MatrixXd& rhs,
    const std::vector<int>& fixedVertices,
    const Eigen::MatrixXd& fixedValues) {
    if (A.rows() != A.cols() || A.rows() != rhs.rows()) throw std::invalid_argument("Incompatible Poisson system dimensions");
    if (static_cast<int>(fixedVertices.size()) != fixedValues.rows() || rhs.cols() != fixedValues.cols()) {
        throw std::invalid_argument("Constraint dimensions do not match the right-hand side");
    }

    Eigen::MatrixXd dense(A);
    Eigen::MatrixXd b = rhs;
    for (int k = 0; k < static_cast<int>(fixedVertices.size()); ++k) {
        const int c = fixedVertices[k];
        if (c < 0 || c >= A.rows()) throw std::out_of_range("Constraint vertex is outside the system");
        for (int i = 0; i < A.rows(); ++i) {
            if (i != c) b.row(i) -= dense(i, c) * fixedValues.row(k);
        }
        dense.row(c).setZero();
        dense.col(c).setZero();
        dense(c, c) = 1.0;
        b.row(c) = fixedValues.row(k);
    }
    Eigen::FullPivLU<Eigen::MatrixXd> solver(dense);
    if (!solver.isInvertible()) throw std::runtime_error("Constrained Poisson system is singular");
    return solver.solve(b);
}
