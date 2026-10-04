// HeatMethod.cpp
// Discrete Differential Geometry - Heat Method Implementation
// Added by Graph Technologies, 2025

#include "HeatMethod.h"
#include "CotanLaplacian.h"
#include <Eigen/SparseLU>
#include <cmath>
#include <stdexcept>

namespace ddg {

Eigen::VectorXd HeatMethod::compute(const Mesh& mesh,
                                     const std::vector<int>& sourceVertices) {
    double timestep = computeTimestep(mesh);
    if (sourceVertices.empty()) throw std::invalid_argument("At least one source vertex is required");
    for (int source : sourceVertices) {
        if (source < 0 || source >= static_cast<int>(mesh.numVertices())) throw std::out_of_range("Source vertex is outside the mesh");
    }
    
    // Step 1: Diffuse heat from sources
    Eigen::VectorXd u = solveHeatFlow(mesh, sourceVertices, timestep);
    
    // Step 2: Compute integrated divergence
    Eigen::VectorXd div = computeIntegratedDivergence(mesh, u);
    
    // Step 3: Solve for distance
    Eigen::VectorXd phi = solveDistance(mesh, div);
    
    // Shift so distance is zero at sources
    double minDist = std::numeric_limits<double>::max();
    for (int src : sourceVertices) {
        minDist = std::min(minDist, phi(src));
    }
    phi.array() -= minDist;
    
    return phi;
}

Eigen::VectorXd HeatMethod::compute(const Mesh& mesh, int sourceVertex) {
    return compute(mesh, std::vector<int>{sourceVertex});
}

double HeatMethod::computeTimestep(const Mesh& mesh) {
    // Timestep = mean edge length squared
    double meanEdgeLength = 0.0;
    for (const auto& e : mesh.edges) {
        meanEdgeLength += e->length();
    }
    if (mesh.numEdges() == 0) throw std::invalid_argument("Heat method requires at least one edge");
    meanEdgeLength /= mesh.numEdges();
    
    return meanEdgeLength * meanEdgeLength;
}

Eigen::VectorXd HeatMethod::solveHeatFlow(const Mesh& mesh,
                                          const std::vector<int>& sources,
                                          double timestep) {
    // Solve (M - t*L)*u = δ_sources
    Eigen::SparseMatrix<double> L = CotanLaplacian::build(mesh);
    Eigen::SparseMatrix<double> M = CotanLaplacian::buildMassMatrix(mesh);
    
    Eigen::SparseMatrix<double> A = M - timestep * L;
    
    // Right-hand side: delta function at sources
    Eigen::VectorXd rhs = Eigen::VectorXd::Zero(mesh.numVertices());
    for (int src : sources) {
        rhs(src) = 1.0;
    }
    rhs = M * rhs;
    
    // Solve system
    Eigen::SparseLU<Eigen::SparseMatrix<double>> solver;
    solver.compute(A);
    if (solver.info() != Eigen::Success) throw std::runtime_error("Heat system factorization failed");
    Eigen::VectorXd u = solver.solve(rhs);
    if (solver.info() != Eigen::Success || !u.allFinite()) throw std::runtime_error("Heat system solve failed");
    
    return u;
}

Eigen::VectorXd HeatMethod::computeIntegratedDivergence(const Mesh& mesh,
                                                        const Eigen::VectorXd& u) {
    Eigen::VectorXd div = Eigen::VectorXd::Zero(mesh.numVertices());
    
    // Compute gradient of u on each face, normalize, integrate divergence
    for (const auto& f : mesh.faces) {
        if (!f->isTriangle()) continue;
        
        auto verts = f->vertices();
        int i0 = verts[0]->index;
        int i1 = verts[1]->index;
        int i2 = verts[2]->index;
        
        // Compute gradient in face
        double u0 = u(i0), u1 = u(i1), u2 = u(i2);
        
        Eigen::Vector3d p0 = verts[0]->position;
        Eigen::Vector3d p1 = verts[1]->position;
        Eigen::Vector3d p2 = verts[2]->position;
        
        Eigen::Vector3d e1 = p1 - p0;
        Eigen::Vector3d e2 = p2 - p0;
        Eigen::Vector3d N = e1.cross(e2);
        double area = 0.5 * N.norm();
        if (area <= 1e-14) continue;
        N.normalize();
        
        // Gradient of u
        Eigen::Vector3d grad_u = ((u1 - u0) * e2.cross(N) + 
                                  (u2 - u0) * N.cross(e1)) / (2.0 * area);
        
        double gradNorm = grad_u.norm();
        if (gradNorm <= 1e-14) continue;
        const Eigen::Vector3d X = -grad_u / gradNorm;

        const Eigen::Vector3d gradPhi0 = N.cross(p2 - p1) / (2.0 * area);
        const Eigen::Vector3d gradPhi1 = N.cross(p0 - p2) / (2.0 * area);
        const Eigen::Vector3d gradPhi2 = N.cross(p1 - p0) / (2.0 * area);
        div(i0) -= area * gradPhi0.dot(X);
        div(i1) -= area * gradPhi1.dot(X);
        div(i2) -= area * gradPhi2.dot(X);
    }
    
    return div;
}

Eigen::VectorXd HeatMethod::solveDistance(const Mesh& mesh,
                                          const Eigen::VectorXd& divergence) {
    // Solve Δφ = div
    Eigen::SparseMatrix<double> L = CotanLaplacian::build(mesh);
    Eigen::MatrixXd rhs = divergence;
    return CotanLaplacian::solveConstrained(
        L, rhs, {0}, Eigen::MatrixXd::Zero(1, 1)).col(0);
}
} // namespace ddg
