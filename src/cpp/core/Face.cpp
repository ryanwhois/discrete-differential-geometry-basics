#include "Face.h"
#include "HalfEdge.h"
#include "Vertex.h"

std::vector<Vertex*> Face::vertices() const {
    std::vector<Vertex*> verts;
    if (!halfedge) return verts;
    HalfEdge* he = halfedge;
    do {
        verts.push_back(he->vertex);
        he = he->next;
    } while (he != halfedge);
    return verts;
}

std::vector<HalfEdge*> Face::halfedges() const {
    std::vector<HalfEdge*> hes;
    if (!halfedge) return hes;
    HalfEdge* he = halfedge;
    do {
        hes.push_back(he);
        he = he->next;
    } while (he != halfedge);
    return hes;
}

double Face::area() const {
    if (!halfedge || !isTriangle()) return 0.0;
    auto verts = vertices();
    Eigen::Vector3d e1 = verts[1]->position - verts[0]->position;
    Eigen::Vector3d e2 = verts[2]->position - verts[0]->position;
    return 0.5 * e1.cross(e2).norm();
}

Eigen::Vector3d Face::normal() const {
    if (!halfedge || !isTriangle()) return Eigen::Vector3d::Zero();
    auto verts = vertices();
    Eigen::Vector3d e1 = verts[1]->position - verts[0]->position;
    Eigen::Vector3d e2 = verts[2]->position - verts[0]->position;
    const Eigen::Vector3d n = e1.cross(e2);
    return n.norm() > 1e-14 ? n.normalized() : Eigen::Vector3d::Zero();
}

bool Face::isTriangle() const {
    if (!halfedge) return false;
    return halfedge->next && halfedge->next->next &&
           halfedge->next->next->next == halfedge;
}
