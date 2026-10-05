#pragma once
#include <vector>
#include <memory>
#include <string>
#include <Eigen/Dense>
#include "Vertex.h"
#include "Edge.h"
#include "Face.h"
#include "HalfEdge.h"

class Mesh {
public:
    std::vector<std::unique_ptr<Vertex>> vertices;
    std::vector<std::unique_ptr<Edge>> edges;
    std::vector<std::unique_ptr<Face>> faces;
    std::vector<std::unique_ptr<HalfEdge>> halfedges;
    
    void build(const Eigen::MatrixXd& V, const Eigen::MatrixXi& F);
    void reindex();
    std::vector<std::vector<Vertex*>> boundaryLoops() const;
    std::vector<std::string> validate() const;
    
    size_t numVertices() const { return vertices.size(); }
    size_t numEdges() const { return edges.size(); }
    size_t numFaces() const { return faces.size(); }
    size_t numBoundaryLoops() const { return boundaryLoops().size(); }
    int eulerCharacteristic() const { return numVertices() - numEdges() + numFaces(); }
    
    Eigen::MatrixXd vertexPositions() const;
    void setVertexPositions(const Eigen::MatrixXd& V);
    void center();
    void normalize();
};
