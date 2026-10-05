# Python Mesh Utilities (Experimental)

This folder contains lightweight helper scripts for quick mesh visualization/inspection workflows.

`inspect_mesh.py` is a dependency-free validator for triangular OBJ files. It
reports element counts, Euler characteristic, boundary loops, degenerate faces,
non-manifold edges, and orientation conflicts.

## Files
- `visualize_mesh.py`
- `inspect_mesh.py`
- `test_inspect_mesh.py`

## Install
```bash
cd examples/python
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

```bash
python inspect_mesh.py model.obj
python inspect_mesh.py model.obj --json
python -m unittest -v test_inspect_mesh.py
```

## Notes
- Utilities are intentionally simple and not part of the core solver implementation.
- This area is experimental and may evolve in future releases.
