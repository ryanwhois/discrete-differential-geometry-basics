# Contributing

Thanks for improving GeometryKit and its DDG reference material.

## Scope

Prioritize:
- GeometryKit public-API clarity and host-neutral boundaries,
- algorithm correctness,
- numerical robustness,
- topology validation,
- educational clarity,
- build/test reliability.

Avoid overstating maturity in docs.

## Local Setup

### C++
```bash
cd src/cpp
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build -j4
cd build
ctest --output-on-failure
```

### C#
```bash
cd src/csharp
dotnet restore
dotnet build --configuration Release
cd Tests
dotnet test --configuration Release
```

### WASM (optional)
```bash
cd src/wasm
./build.sh
```

## Standards

### Code
- Keep changes small and focused.
- Add defensive checks for invalid/degenerate mesh input.
- Prefer explicit failure over silent NaNs.
- Document convention choices (orientation, boundary treatment, signs).

### Tests
- Add tests for every bug fix.
- Use tolerances for floating-point checks.
- Prefer mesh fixtures with known geometric/topological invariants.

### Documentation
- Use **Complete / Partial / Experimental / Planned** labels honestly.
- Update `README.md`, `SUMMARY.md`, and status docs when behavior changes.

## Pull Requests

Include:
1. What changed.
2. Why the change is needed.
3. Build/test commands run and results.
4. Remaining limitations or risks.

## Useful References

- Keenan Crane DDG notes: https://www.cs.cmu.edu/~kmcrane/Projects/DDG/
- geometry-central: https://geometry-central.net/
- libigl: https://libigl.github.io/
