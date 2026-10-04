#include "Mesh.h"
#include <map>
#include <algorithm>
#include <set>
#include <stdexcept>
#include <sstream>

void Mesh::build(const Eigen::MatrixXd& V, const Eigen::MatrixXi& F) {
    vertices.clear(); edges.clear(); faces.clear(); halfedges.clear();
    if (V.cols() != 3 || F.cols() != 3) {
        throw std::invalid_argument("Mesh::build expects Vx3 positions and triangular Fx3 faces");
    }

    for (int i = 0; i < V.rows(); i++) {
        if (!V.row(i).allFinite()) throw std::invalid_argument("Vertex positions must be finite");
        vertices.push_back(std::make_unique<Vertex>(V.row(i)));
        vertices.back()->index = i;
    }

    std::map<std::pair<int,int>, HalfEdge*> heMap;
    std::map<std::pair<int,int>, int> undirectedCounts;

    for (int i = 0; i < F.rows(); i++) {
        const int a = F(i, 0), b = F(i, 1), c = F(i, 2);
        for (int v : {a, b, c}) {
            if (v < 0 || v >= V.rows()) throw std::out_of_range("Face index is outside the vertex array");
        }
        if (a == b || b == c || c == a) throw std::invalid_argument("Faces must contain three distinct vertices");

        auto face = std::make_unique<Face>();
        std::vector<HalfEdge*> faceHEs;

        for (int j = 0; j < 3; j++) {
            const int source = F(i, j);
            const int target = F(i, (j + 1) % 3);
            if (heMap.count({source, target})) {
                throw std::invalid_argument("Duplicate directed edge; mesh is non-manifold or inconsistently oriented");
            }
            const auto undirected = std::minmax(source, target);
            if (++undirectedCounts[undirected] > 2) {
                throw std::invalid_argument("Non-manifold edge has more than two incident faces");
            }
            auto he = std::make_unique<HalfEdge>();
            he->vertex = vertices[target].get();
            he->face = face.get();
            faceHEs.push_back(he.get());
            heMap[{source, target}] = he.get();
            halfedges.push_back(std::move(he));
        }

        for (int j = 0; j < 3; j++) faceHEs[j]->next = faceHEs[(j+1)%3];
        face->halfedge = faceHEs[0];
        faces.push_back(std::move(face));
    }

    // Pair interior halfedges and create one explicit edge for every pair.
    for (auto& [key, he] : heMap) {
        if (he->edge) continue;
        auto twinIt = heMap.find({key.second, key.first});
        if (twinIt != heMap.end()) {
            he->twin = twinIt->second;
            twinIt->second->twin = he;
        } else {
            auto boundary = std::make_unique<HalfEdge>();
            boundary->vertex = vertices[key.first].get();
            boundary->twin = he;
            he->twin = boundary.get();
            halfedges.push_back(std::move(boundary));
        }
        auto edge = std::make_unique<Edge>();
        edge->halfedge = he;
        he->edge = edge.get();
        he->twin->edge = edge.get();
        edges.push_back(std::move(edge));
    }

    // Link explicit boundary halfedges into loops.
    std::map<int, HalfEdge*> boundaryBySource;
    for (auto& owned : halfedges) {
        HalfEdge* he = owned.get();
        if (!he->isBoundary()) continue;
        const int source = he->source()->index;
        if (!boundaryBySource.emplace(source, he).second) {
            throw std::invalid_argument("Boundary vertex has multiple outgoing boundary halfedges");
        }
    }
    for (auto& [source, he] : boundaryBySource) {
        auto next = boundaryBySource.find(he->target()->index);
        if (next == boundaryBySource.end()) throw std::invalid_argument("Open boundary chain could not be closed");
        he->next = next->second;
    }

    for (auto& he : halfedges) {
        Vertex* source = he->source();
        if (source && (!source->halfedge || he->isBoundary())) source->halfedge = he.get();
    }
    reindex();

    const auto errors = validate();
    if (!errors.empty()) throw std::invalid_argument(errors.front());
}

void Mesh::reindex() {
    for (size_t i = 0; i < vertices.size(); i++) vertices[i]->index = i;
    for (size_t i = 0; i < edges.size(); i++) edges[i]->index = i;
    for (size_t i = 0; i < faces.size(); i++) faces[i]->index = i;
    for (size_t i = 0; i < halfedges.size(); i++) halfedges[i]->index = i;
}

std::vector<std::vector<Vertex*>> Mesh::boundaryLoops() const {
    std::vector<std::vector<Vertex*>> loops;
    std::set<const HalfEdge*> visited;
    for (const auto& owned : halfedges) {
        HalfEdge* start = owned.get();
        if (!start->isBoundary() || visited.count(start)) continue;
        std::vector<Vertex*> loop;
        HalfEdge* he = start;
        do {
            if (!he || !he->next || !he->target()) break;
            visited.insert(he);
            loop.push_back(he->target());
            he = he->next;
        } while (he != start && !visited.count(he));
        if (he == start && !loop.empty()) loops.push_back(std::move(loop));
    }
    return loops;
}

std::vector<std::string> Mesh::validate() const {
    std::vector<std::string> errors;
    for (const auto& owned : halfedges) {
        const HalfEdge* he = owned.get();
        if (!he->vertex || !he->edge || !he->twin || !he->next) {
            errors.emplace_back("Halfedge has a missing vertex, edge, twin, or next pointer");
            continue;
        }
        if (he->twin->twin != he) errors.emplace_back("Halfedge twin relation is not symmetric");
        if (he->edge != he->twin->edge) errors.emplace_back("Twin halfedges do not share an edge");
    }
    for (const auto& face : faces) {
        if (!face->isTriangle()) {
            errors.emplace_back("Degenerate or non-triangular face");
            continue;
        }

        const auto verts = face->vertices();
        const Eigen::Vector3d e01 = verts[1]->position - verts[0]->position;
        const Eigen::Vector3d e02 = verts[2]->position - verts[0]->position;
        const Eigen::Vector3d e12 = verts[2]->position - verts[1]->position;
        const double maxEdgeSquared = std::max({e01.squaredNorm(), e02.squaredNorm(), e12.squaredNorm()});
        const double twiceArea = e01.cross(e02).norm();
        if (maxEdgeSquared == 0.0 || twiceArea <= 1e-12 * maxEdgeSquared) {
            errors.emplace_back("Degenerate or non-triangular face");
        }
    }
    return errors;
}

Eigen::MatrixXd Mesh::vertexPositions() const {
    Eigen::MatrixXd V(numVertices(), 3);
    for (size_t i = 0; i < numVertices(); i++) V.row(i) = vertices[i]->position;
    return V;
}

void Mesh::setVertexPositions(const Eigen::MatrixXd& V) {
    for (size_t i = 0; i < numVertices(); i++) vertices[i]->position = V.row(i);
}

void Mesh::center() {
    if (vertices.empty()) return;
    Eigen::Vector3d c = Eigen::Vector3d::Zero();
    for (const auto& v : vertices) c += v->position;
    c /= numVertices();
    for (auto& v : vertices) v->position -= c;
}

void Mesh::normalize() {
    center();
    double maxD = 0;
    for (const auto& v : vertices) maxD = std::max(maxD, v->position.norm());
    if (maxD > 0) for (auto& v : vertices) v->position /= maxD;
}
