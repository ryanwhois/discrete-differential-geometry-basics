# ADR 0001: Derived Meshes Are Immutable Snapshots

- Status: Accepted
- Date: 2026-10-10

## Context

Rhino, Revit and an OpenCascade-based CAD application each have their own document lifecycle,
transaction model, units and object identity. A mesh calculation that silently changes a host model
cannot be retried safely by an agent, compared in tests, or presented for user approval.

## Decision

`TriangleMesh` is immutable. GeometryKit operations return values, reports and new derived data;
they do not mutate an input mesh or call a host API. Host adapters own import, identifiers,
transactions and applying results.

## Consequences

- Deterministic functions are straightforward to unit test and expose over MCP.
- A caller must explicitly rebuild a spatial index after a host edit.
- Repairs/remeshing will return a new mesh plus a change report, never modify a live document.
