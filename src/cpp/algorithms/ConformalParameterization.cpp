// ConformalParameterization.cpp
// Discrete Differential Geometry - Conformal Parameterization Implementation
// Added by Graph Technologies, 2025

#include "ConformalParameterization.h"
#include "CotanLaplacian.h"
#include <Eigen/Eigenvalues>
#include <Eigen/SparseLU>
#include <cmath>
#include <algorithm>

namespace ddg {

Eigen::MatrixXd ConformalParameterization::spectral(const Mesh& mesh) {
    // Build cotan Laplacian and mass matrix
    Eigen::SparseMatrix<double> L = CotanLaplacian::build(mesh);
    Eigen::SparseMatrix<double> M = CotanLaplacian::buildMassMatrix(mesh);
    
    // Solve generalized eigenvalue problem: L*phi = lambda*M*phi
    Eigen::MatrixXd eigenvectors;
    Eigen::VectorXd eigenvalues;
    solveEigenProblem(-L, M, eigenvectors, eigenvalues, 3);
    
    // Use 2nd and 3rd eigenvectors as conformal coordinates
    // (1st is constant, skip it)
    Eigen::MatrixXd uv(mesh.numVertices(), 2);
    uv.col(0) = eigenvectors.col(1);
    uv.col(1) = eigenvectors.col(2);
    
    return uv;
}

Eigen::MatrixXd ConformalParameterization::boundaryCircle(const Mesh& mesh) {
    constexpr double pi = 3.14159265358979323846;
    int n = mesh.numVertices();
    Eigen::SparseMatrix<double> L = CotanLaplacian::build(mesh);
    
    const auto loops = mesh.boundaryLoops();
    if (loops.empty()) {
        throw std::runtime_error("Mesh has no boundary - use spectral method");
    }
    const auto largest = std::max_element(
        loops.begin(), loops.end(),
        [](const auto& a, const auto& b) { return a.size() < b.size(); });
    const auto& boundary = *largest;
    std::vector<int> boundaryVerts;
    boundaryVerts.reserve(boundary.size());
    for (Vertex* v : boundary) boundaryVerts.push_back(v->index);
    
    // Map boundary to unit circle
    Eigen::MatrixXd boundaryUV(boundaryVerts.size(), 2);
    std::vector<double> cumulative(boundary.size() + 1, 0.0);
    for (size_t i = 0; i < boundary.size(); ++i) {
        cumulative[i + 1] = cumulative[i] +
            (boundary[(i + 1) % boundary.size()]->position - boundary[i]->position).norm();
    }
    const double perimeter = cumulative.back();
    if (perimeter <= 1e-14) throw std::runtime_error("Boundary perimeter is degenerate");
    for (size_t i = 0; i < boundaryVerts.size(); i++) {
        double angle = 2.0 * pi * cumulative[i] / perimeter;
        boundaryUV(i, 0) = std::cos(angle);
        boundaryUV(i, 1) = std::sin(angle);
    }
    
    // Solve Laplace equation with boundary constraints
    Eigen::MatrixXd uv = Eigen::MatrixXd::Zero(n, 2);
    
    // Set boundary values
    for (size_t i = 0; i < boundaryVerts.size(); i++) {
        uv.row(boundaryVerts[i]) = boundaryUV.row(i);
    }
    
    return CotanLaplacian::solveConstrained(
        L, Eigen::MatrixXd::Zero(n, 2), boundaryVerts, boundaryUV);
}

Eigen::MatrixXd ConformalParameterization::lscm(const Mesh& mesh,
                                                 const std::vector<int>& fixedVertices,
                                                 const Eigen::MatrixXd& fixedPositions) {
    (void)mesh;
    (void)fixedVertices;
    (void)fixedPositions;
    throw std::logic_error("True LSCM assembly is deferred; use boundaryCircle for disk topology");
}

double ConformalParameterization::dirichletEnergy(const Mesh& mesh, 
                                                   const Eigen::MatrixXd& uv) {
    double energy = 0.0;
    
    for (const auto& f : mesh.faces) {
        if (!f->isTriangle()) continue;
        
        auto verts = f->vertices();
        Eigen::Vector3d p0 = verts[0]->position;
        Eigen::Vector3d p1 = verts[1]->position;
        Eigen::Vector3d p2 = verts[2]->position;
        
        Eigen::Vector2d uv0 = uv.row(verts[0]->index);
        Eigen::Vector2d uv1 = uv.row(verts[1]->index);
        Eigen::Vector2d uv2 = uv.row(verts[2]->index);
        
        // Compute area in 3D
        Eigen::Vector3d e1 = p1 - p0;
        Eigen::Vector3d e2 = p2 - p0;
        double area3D = 0.5 * e1.cross(e2).norm();
        
        // Compute gradient in parameter space
        Eigen::Vector2d g1 = uv1 - uv0;
        Eigen::Vector2d g2 = uv2 - uv0;
        double gradNorm = g1.squaredNorm() + g2.squaredNorm();
        
        energy += area3D * gradNorm;
    }
    
    return energy;
}

void ConformalParameterization::solveEigenProblem(
    const Eigen::SparseMatrix<double>& L,
    const Eigen::SparseMatrix<double>& M,
    Eigen::MatrixXd& eigenvectors,
    Eigen::VectorXd& eigenvalues,
    int numEigs
) {
    // Convert to dense for eigenvalue solver
    // For production, use sparse eigenvalue solver (Spectra/ARPACK)
    Eigen::MatrixXd L_dense = Eigen::MatrixXd(L);
    Eigen::MatrixXd M_dense = Eigen::MatrixXd(M);
    
    // Solve generalized eigenvalue problem
    Eigen::GeneralizedSelfAdjointEigenSolver<Eigen::MatrixXd> solver(L_dense, M_dense);
    
    eigenvectors = solver.eigenvectors().leftCols(numEigs);
    eigenvalues = solver.eigenvalues().head(numEigs);
}

} // namespace ddg
