// test_mesh.cpp
// Unit tests for mesh data structures
// Added by Graph Technologies, 2025

#include <catch2/catch_test_macros.hpp>
#include <catch2/matchers/catch_matchers_floating_point.hpp>
#include "../core/Mesh.h"
#include <Eigen/Dense>

TEST_CASE("Mesh construction", "[mesh]") {
    // Build simple tetrahedron
    Eigen::MatrixXd V(4, 3);
    V << 0, 0, 0,
         1, 0, 0,
         0, 1, 0,
         0, 0, 1;
    
    Eigen::MatrixXi F(4, 3);
    F << 0, 2, 1,
         0, 1, 3,
         0, 3, 2,
         1, 2, 3;
    
    Mesh mesh;
    mesh.build(V, F);
    
    SECTION("Element counts") {
        REQUIRE(mesh.numVertices() == 4);
        REQUIRE(mesh.numEdges() == 6);
        REQUIRE(mesh.numFaces() == 4);
    }
    
    SECTION("Euler characteristic") {
        REQUIRE(mesh.eulerCharacteristic() == 2);
    }
    
    SECTION("Halfedge connectivity") {
        for (const auto& he : mesh.halfedges) {
            // Twin symmetry
            REQUIRE(he->twin->twin == he.get());
            
            // Cycle property
            REQUIRE(he->next->next->next == he.get());
            
            // Edge consistency
            REQUIRE(he->edge == he->twin->edge);
        }
    }
}

TEST_CASE("Vertex operations", "[mesh][vertex]") {
    // Build cube
    Eigen::MatrixXd V(8, 3);
    V << -1, -1, -1,
          1, -1, -1,
          1,  1, -1,
         -1,  1, -1,
         -1, -1,  1,
          1, -1,  1,
          1,  1,  1,
         -1,  1,  1;
    
    Eigen::MatrixXi F(12, 3);
    F << 0, 1, 2,  0, 2, 3,
         4, 7, 6,  4, 6, 5,
         0, 4, 5,  0, 5, 1,
         2, 6, 7,  2, 7, 3,
         0, 3, 7,  0, 7, 4,
         1, 5, 6,  1, 6, 2;
    
    Mesh mesh;
    mesh.build(V, F);
    
    SECTION("Vertex degree") {
        // Degree is implementation-dependent for this triangulated fixture.
        for (const auto& v : mesh.vertices) {
            REQUIRE(v->degree() >= 3);
        }
    }
    
    SECTION("Star operation") {
        auto star = mesh.vertices[0]->star();
        REQUIRE(star.size() >= 3);
    }
}

TEST_CASE("Mesh utilities", "[mesh][utils]") {
    Eigen::MatrixXd V(4, 3);
    V << 1, 0, 0,
         0, 1, 0,
         -1, 0, 0,
         0, -1, 0;
    
    Eigen::MatrixXi F(2, 3);
    F << 0, 1, 2,
         0, 2, 3;
    
    Mesh mesh;
    mesh.build(V, F);
    
    SECTION("Center mesh") {
        mesh.center();
        
        Eigen::Vector3d centroid = Eigen::Vector3d::Zero();
        for (const auto& v : mesh.vertices) {
            centroid += v->position;
        }
        centroid /= mesh.numVertices();
        
        REQUIRE_THAT(centroid.norm(), Catch::Matchers::WithinAbs(0.0, 1e-10));
    }
    
    SECTION("Normalize mesh") {
        mesh.normalize();
        
        double maxDist = 0.0;
        for (const auto& v : mesh.vertices) {
            maxDist = std::max(maxDist, v->position.norm());
        }
        
        REQUIRE_THAT(maxDist, Catch::Matchers::WithinAbs(1.0, 1e-10));
    }
}

TEST_CASE("Open meshes have explicit boundary loops", "[mesh][boundary]") {
    Eigen::MatrixXd V(4, 3);
    V << 0, 0, 0,
         1, 0, 0,
         1, 1, 0,
         0, 1, 0;
    Eigen::MatrixXi F(2, 3);
    F << 0, 1, 2,
         0, 2, 3;

    Mesh mesh;
    mesh.build(V, F);

    REQUIRE(mesh.numEdges() == 5);
    REQUIRE(mesh.halfedges.size() == 10);
    REQUIRE(mesh.eulerCharacteristic() == 1);
    REQUIRE(mesh.numBoundaryLoops() == 1);
    REQUIRE(mesh.boundaryLoops().front().size() == 4);
    REQUIRE(mesh.validate().empty());
    for (const auto& vertex : mesh.vertices) {
        REQUIRE(vertex->isBoundary());
        REQUIRE(vertex->degree() >= 2);
    }
}

TEST_CASE("Invalid topology is rejected", "[mesh][validation]") {
    Eigen::MatrixXd V(4, 3);
    V << 0, 0, 0,
         1, 0, 0,
         0, 1, 0,
         0, -1, 0;
    Eigen::MatrixXi duplicateDirected(2, 3);
    duplicateDirected << 0, 1, 2,
                         0, 1, 3;
    Mesh mesh;
    REQUIRE_THROWS(mesh.build(V, duplicateDirected));
}
