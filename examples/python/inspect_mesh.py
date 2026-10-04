#!/usr/bin/env python3
"""Dependency-free OBJ topology inspector for triangular surface meshes."""

from __future__ import annotations

import argparse
import json
import math
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Iterable


@dataclass(frozen=True)
class MeshReport:
    vertices: int
    edges: int
    faces: int
    euler_characteristic: int
    boundary_edges: int
    boundary_loops: int
    degenerate_faces: list[int]
    non_manifold_edges: list[tuple[int, int]]
    orientation_conflicts: list[tuple[int, int]]

    @property
    def valid(self) -> bool:
        return not (self.degenerate_faces or self.non_manifold_edges or self.orientation_conflicts)


def _index(token: str, vertex_count: int, line_number: int) -> int:
    head = token.split("/", 1)[0]
    if not head:
        raise ValueError(f"line {line_number}: face vertex index is empty")
    raw = int(head)
    if raw == 0:
        raise ValueError(f"line {line_number}: OBJ indices are one-based; zero is invalid")
    value = raw - 1 if raw > 0 else vertex_count + raw
    if value < 0 or value >= vertex_count:
        raise ValueError(f"line {line_number}: vertex index {raw} is out of range")
    return value


def parse_obj(lines: Iterable[str]) -> tuple[list[tuple[float, float, float]], list[tuple[int, int, int]]]:
    vertices: list[tuple[float, float, float]] = []
    faces: list[tuple[int, int, int]] = []
    for line_number, original in enumerate(lines, 1):
        line = original.partition("#")[0].strip()
        if not line:
            continue
        fields = line.split()
        if fields[0] == "v":
            if len(fields) < 4:
                raise ValueError(f"line {line_number}: vertex requires three coordinates")
            point = tuple(float(value) for value in fields[1:4])
            if not all(math.isfinite(value) for value in point):
                raise ValueError(f"line {line_number}: vertex coordinates must be finite")
            vertices.append(point)  # type: ignore[arg-type]
        elif fields[0] == "f":
            if len(fields) != 4:
                raise ValueError(f"line {line_number}: only triangular faces are supported")
            face = tuple(_index(token, len(vertices), line_number) for token in fields[1:])
            faces.append(face)  # type: ignore[arg-type]
    return vertices, faces


def inspect_mesh(
    vertices: list[tuple[float, float, float]],
    faces: list[tuple[int, int, int]],
    epsilon: float = 1e-12,
) -> MeshReport:
    undirected: dict[tuple[int, int], list[tuple[int, int]]] = {}
    degenerates: list[int] = []
    for face_index, (a, b, c) in enumerate(faces):
        if len({a, b, c}) != 3:
            degenerates.append(face_index)
        else:
            p, q, r = vertices[a], vertices[b], vertices[c]
            u = (q[0] - p[0], q[1] - p[1], q[2] - p[2])
            v = (r[0] - p[0], r[1] - p[1], r[2] - p[2])
            cross = (
                u[1] * v[2] - u[2] * v[1],
                u[2] * v[0] - u[0] * v[2],
                u[0] * v[1] - u[1] * v[0],
            )
            if sum(component * component for component in cross) <= epsilon * epsilon:
                degenerates.append(face_index)
        for source, target in ((a, b), (b, c), (c, a)):
            key = (min(source, target), max(source, target))
            undirected.setdefault(key, []).append((source, target))

    non_manifold = sorted(edge for edge, uses in undirected.items() if len(uses) > 2)
    conflicts = sorted(edge for edge, uses in undirected.items() if len(uses) == 2 and uses[0] == uses[1])
    boundary = [edge for edge, uses in undirected.items() if len(uses) == 1]
    adjacency: dict[int, list[int]] = {}
    for a, b in boundary:
        adjacency.setdefault(a, []).append(b)
        adjacency.setdefault(b, []).append(a)
    seen: set[int] = set()
    loops = 0
    for start in adjacency:
        if start in seen:
            continue
        loops += 1
        stack = [start]
        while stack:
            vertex = stack.pop()
            if vertex in seen:
                continue
            seen.add(vertex)
            stack.extend(adjacency.get(vertex, ()))

    return MeshReport(
        vertices=len(vertices),
        edges=len(undirected),
        faces=len(faces),
        euler_characteristic=len(vertices) - len(undirected) + len(faces),
        boundary_edges=len(boundary),
        boundary_loops=loops,
        degenerate_faces=degenerates,
        non_manifold_edges=non_manifold,
        orientation_conflicts=conflicts,
    )


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("obj", type=Path, help="triangular Wavefront OBJ file")
    parser.add_argument("--json", action="store_true", help="emit machine-readable JSON")
    args = parser.parse_args()
    vertices, faces = parse_obj(args.obj.read_text(encoding="utf-8").splitlines())
    report = inspect_mesh(vertices, faces)
    payload = {**asdict(report), "valid": report.valid}
    if args.json:
        print(json.dumps(payload, indent=2))
    else:
        for key, value in payload.items():
            print(f"{key.replace('_', ' ')}: {value}")
    return 0 if report.valid else 2


if __name__ == "__main__":
    raise SystemExit(main())
