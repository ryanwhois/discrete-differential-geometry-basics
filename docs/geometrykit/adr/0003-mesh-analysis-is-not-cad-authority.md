# ADR 0003: B-Rep Remains CAD Authority

- Status: Accepted
- Date: 2026-10-10

## Context

The companion project will be used beside an OpenCascade-based CAD application. Tessellation loses
the parametric and topological semantics of the original B-Rep; making a mesh authoritative would
make exact editing and reliable STEP workflows worse.

## Decision

GeometryKit accepts derived meshes and returns analysis fields, section curves, reports, guide
geometry or replacement meshes. It does not claim lossless mesh-to-B-Rep round-tripping.

## Consequences

- CAD retains control of solids, feature history and exchange formats.
- GeometryKit can evolve independently for scans, terrain, simulation meshes and fabrication.
- A bridge can map results back to stable CAD selections or regeneration parameters where possible.
