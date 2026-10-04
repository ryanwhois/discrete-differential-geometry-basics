#include "Vertex.h"
#include "HalfEdge.h"
#include "Face.h"
#include <set>

std::vector<Vertex*> Vertex::star() const {
    std::vector<Vertex*> neighbors;
    if (!halfedge) return neighbors;
    
    HalfEdge* he = halfedge;
    std::set<const HalfEdge*> visited;
    while (he && visited.insert(he).second) {
        if (he->target()) neighbors.push_back(he->target());
        he = he->twin ? he->twin->next : nullptr;
    }
    
    return neighbors;
}

int Vertex::degree() const {
    if (!halfedge) return 0;
    
    return static_cast<int>(outgoingHalfEdges().size());
}

bool Vertex::isBoundary() const {
    if (!halfedge) return true;
    
    for (HalfEdge* he : outgoingHalfEdges()) if (he->isBoundary()) return true;
    
    return false;
}

std::vector<HalfEdge*> Vertex::outgoingHalfEdges() const {
    std::vector<HalfEdge*> halfedges;
    if (!halfedge) return halfedges;
    
    HalfEdge* he = halfedge;
    std::set<const HalfEdge*> visited;
    while (he && visited.insert(he).second) {
        halfedges.push_back(he);
        he = he->twin ? he->twin->next : nullptr;
    }
    
    return halfedges;
}

std::vector<Face*> Vertex::adjacentFaces() const {
    std::vector<Face*> faces;
    if (!halfedge) return faces;
    
    HalfEdge* he = halfedge;
    std::set<const HalfEdge*> visited;
    while (he && visited.insert(he).second) {
        if (he->face) {
            faces.push_back(he->face);
        }
        he = he->twin ? he->twin->next : nullptr;
    }
    
    return faces;
}
