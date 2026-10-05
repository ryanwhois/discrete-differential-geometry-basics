import unittest

from inspect_mesh import inspect_mesh, parse_obj


class MeshInspectorTests(unittest.TestCase):
    def test_closed_tetrahedron(self):
        vertices, faces = parse_obj(
            ["v 0 0 0", "v 1 0 0", "v 0 1 0", "v 0 0 1",
             "f 1 3 2", "f 1 2 4", "f 1 4 3", "f 2 3 4"]
        )
        report = inspect_mesh(vertices, faces)
        self.assertEqual((report.edges, report.euler_characteristic, report.boundary_loops), (6, 2, 0))
        self.assertTrue(report.valid)

    def test_negative_obj_indices(self):
        vertices, faces = parse_obj(["v 0 0 0", "v 1 0 0", "v 0 1 0", "f -3 -2 -1"])
        self.assertEqual(faces, [(0, 1, 2)])
        self.assertEqual(inspect_mesh(vertices, faces).boundary_loops, 1)

    def test_polygon_is_rejected(self):
        with self.assertRaisesRegex(ValueError, "only triangular"):
            parse_obj(["v 0 0 0", "v 1 0 0", "v 1 1 0", "v 0 1 0", "f 1 2 3 4"])

    def test_degenerate_face_is_reported(self):
        vertices, faces = parse_obj(["v 0 0 0", "v 1 0 0", "v 2 0 0", "f 1 2 3"])
        report = inspect_mesh(vertices, faces)
        self.assertEqual(report.degenerate_faces, [0])
        self.assertFalse(report.valid)

    def test_small_well_conditioned_face_is_valid(self):
        vertices, faces = parse_obj(["v 0 0 0", "v 1e-8 0 0", "v 0 1e-8 0", "f 1 2 3"])
        report = inspect_mesh(vertices, faces)
        self.assertEqual(report.degenerate_faces, [])
        self.assertTrue(report.valid)

    def test_bow_tie_boundary_vertex_is_invalid(self):
        vertices, faces = parse_obj(
            ["v 0 0 0", "v 1 0 0", "v 0 1 0", "v -1 0 0", "v 0 -1 0", "f 1 2 3", "f 1 4 5"]
        )
        report = inspect_mesh(vertices, faces)
        self.assertEqual(report.non_manifold_boundary_vertices, [0])
        self.assertEqual(report.boundary_loops, 0)
        self.assertFalse(report.valid)


if __name__ == "__main__":
    unittest.main()
