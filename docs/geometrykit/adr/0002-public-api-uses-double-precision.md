# ADR 0002: Use Double Precision at the GeometryKit Boundary

- Status: Accepted
- Date: 2026-10-10

## Context

The course companion currently stores positions as `System.Numerics.Vector3`. That is fine for a
small viewer but is a poor public contract for model-unit geometry, accumulated transforms and
near-tolerance classification. Rhino and most engineering/CAD APIs already treat doubles as the
normal interchange representation.

## Decision

GeometryKit exposes its own `Point3d` value type and `GeometryTolerance` model. Conversions to
host or third-party vector types occur only in adapters.

## Consequences

- More predictable numerical behaviour and no public dependency on a UI/runtime vector type.
- Every tolerance-sensitive API can state its model-unit contract.
- GPU/upload code may down-convert to float at rendering boundaries only.
