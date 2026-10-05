// wasm-tests.js
// WebAssembly test suite
// Added by Graph Technologies, 2025

class WASMTestRunner {
    constructor(module) {
        this.module = module;
        this.passed = 0;
        this.failed = 0;
        this.tests = [];
    }
    
    test(name, fn) {
        this.tests.push({ name, fn });
    }
    
    async runAll() {
        console.log('\n🧪 DDG WASM Test Suite');
        console.log('Added by Graph Technologies, 2025');
        console.log('======================================\n');
        
        for (const test of this.tests) {
            try {
                await test.fn();
                console.log(`✅ ${test.name}`);
                this.passed++;
            } catch (e) {
                console.error(`❌ ${test.name}`);
                console.error(`   ${e.message}`);
                this.failed++;
            }
        }
        
        console.log('\n======================================');
        console.log(`Results: ${this.passed} passed, ${this.failed} failed`);
        console.log(`Success rate: ${(this.passed / this.tests.length * 100).toFixed(1)}%`);
        
        return this.failed === 0;
    }
    
    assert(condition, message) {
        if (!condition) {
            throw new Error(message || 'Assertion failed');
        }
    }
    
    assertClose(a, b, tolerance = 1e-6, message) {
        if (Math.abs(a - b) > tolerance) {
            throw new Error(message || `Expected ${a} ≈ ${b} (diff: ${Math.abs(a-b)})`);
        }
    }
}

function runTests(module) {
    const runner = new WASMTestRunner(module);
    
    // ========================================================================
    // Mesh Construction Tests
    // ========================================================================
    
    runner.test('Mesh: Tetrahedron construction', () => {
        const mesh = new module.Mesh();
        
        const positions = new Float32Array([
            0, 0, 0,
            1, 0, 0,
            0, 1, 0,
            0, 0, 1
        ]);
        
        const faces = new Int32Array([
            0, 1, 2,
            0, 1, 3,
            0, 2, 3,
            1, 2, 3
        ]);
        
        mesh.buildFromArrays(positions, faces);
        
        runner.assert(mesh.numVertices() === 4, 'Should have 4 vertices');
        runner.assert(mesh.numEdges() === 6, 'Should have 6 edges');
        runner.assert(mesh.numFaces() === 4, 'Should have 4 faces');
        runner.assert(mesh.eulerCharacteristic() === 2, 'Euler characteristic should be 2');
    });
    
    runner.test('Mesh: Cube construction', () => {
        const mesh = new module.Mesh();
        
        const positions = new Float32Array([
            -1, -1, -1,  1, -1, -1,  1,  1, -1, -1,  1, -1,
            -1, -1,  1,  1, -1,  1,  1,  1,  1, -1,  1,  1
        ]);
        
        const faces = new Int32Array([
            0,1,2, 0,2,3, 4,7,6, 4,6,5,
            0,4,5, 0,5,1, 2,6,7, 2,7,3,
            0,3,7, 0,7,4, 1,5,6, 1,6,2
        ]);
        
        mesh.buildFromArrays(positions, faces);
        
        runner.assert(mesh.numVertices() === 8, 'Cube: 8 vertices');
        runner.assert(mesh.eulerCharacteristic() === 2, 'Cube: χ = 2');
    });
    
    // ========================================================================
    // Gaussian Curvature Tests
    // ========================================================================
    
    runner.test('Gaussian Curvature: Tetrahedron', () => {
        const mesh = new module.Mesh();
        
        const positions = new Float32Array([
            0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1
        ]);
        
        const faces = new Int32Array([
            0, 2, 1, 0, 1, 3, 0, 3, 2, 1, 2, 3
        ]);
        
        mesh.buildFromArrays(positions, faces);
        
        const K = mesh.computeGaussianCurvature();
        runner.assert(K.length === 4, 'Should have curvature for each vertex');
        
        // All curvatures should be positive (convex)
        for (let i = 0; i < K.length; i++) {
            runner.assert(K[i] > 0, `Vertex ${i} should have positive curvature`);
        }
    });
    
    runner.test('Gauss-Bonnet Theorem: Tetrahedron', () => {
        const mesh = new module.Mesh();
        
        const positions = new Float32Array([
            0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1
        ]);
        const faces = new Int32Array([0, 2, 1, 0, 1, 3, 0, 3, 2, 1, 2, 3]);
        
        mesh.buildFromArrays(positions, faces);
        
        const totalK = mesh.totalGaussianCurvature();
        const expectedK = 2 * Math.PI * mesh.eulerCharacteristic();
        
        runner.assertClose(totalK, expectedK, 0.1, 
            `Gauss-Bonnet: total K should equal 2πχ`);
    });
    
    runner.test('Gauss-Bonnet Theorem: Cube', () => {
        const mesh = new module.Mesh();
        
        const positions = new Float32Array([
            -1,-1,-1, 1,-1,-1, 1,1,-1, -1,1,-1,
            -1,-1,1, 1,-1,1, 1,1,1, -1,1,1
        ]);
        
        const faces = new Int32Array([
            0,1,2, 0,2,3, 4,7,6, 4,6,5,
            0,4,5, 0,5,1, 2,6,7, 2,7,3,
            0,3,7, 0,7,4, 1,5,6, 1,6,2
        ]);
        
        mesh.buildFromArrays(positions, faces);
        
        const error = mesh.gaussBonnetError();
        runner.assert(error < 0.1, `Gauss-Bonnet error should be small (got ${error})`);
    });
    
    // ========================================================================
    // Mean Curvature Flow Tests
    // ========================================================================
    
    runner.test('Mean Curvature Flow: Smoothing reduces noise', () => {
        const mesh = new module.Mesh();
        
        // Create noisy triangle
        const positions = new Float32Array([
            0, 0, 0.1,  // Slightly perturbed
            1, 0, -0.05,
            0.5, 0.9, 0.08
        ]);
        const faces = new Int32Array([0, 1, 2]);
        
        mesh.buildFromArrays(positions, faces);
        
        // Get initial positions
        const initialPos = mesh.getPositions();
        const initialNoise = Math.abs(initialPos[2]) + Math.abs(initialPos[5]) + Math.abs(initialPos[8]);
        
        // Apply smoothing
        mesh.meanCurvatureFlow(0.01, 10);
        
        // Get smoothed positions
        const smoothedPos = mesh.getPositions();
        const smoothedNoise = Math.abs(smoothedPos[2]) + Math.abs(smoothedPos[5]) + Math.abs(smoothedPos[8]);
        
        runner.assert(smoothedNoise < initialNoise, 'Smoothing should reduce z-noise');
    });
    
    // ========================================================================
    // Geodesic Distance Tests
    // ========================================================================
    
    runner.test('Heat Method: Distance to self is zero', () => {
        const mesh = new module.Mesh();
        
        const positions = new Float32Array([
            0, 0, 0, 1, 0, 0, 0.5, 0.9, 0, 0.5, 0.3, 0.8
        ]);
        const faces = new Int32Array([0, 2, 1, 0, 1, 3, 0, 3, 2, 1, 2, 3]);
        
        mesh.buildFromArrays(positions, faces);
        
        const dist = mesh.computeGeodesicDistance(0);
        
        runner.assertClose(dist[0], 0, 1e-6, 'Distance to self should be 0');
    });
    
    runner.test('Heat Method: Non-negative distances', () => {
        const mesh = new module.Mesh();
        
        const positions = new Float32Array([
            0, 0, 0, 1, 0, 0, 0.5, 0.9, 0, 0.5, 0.3, 0.8
        ]);
        const faces = new Int32Array([0, 2, 1, 0, 1, 3, 0, 3, 2, 1, 2, 3]);
        
        mesh.buildFromArrays(positions, faces);
        
        const dist = mesh.computeGeodesicDistance(0);
        
        for (let i = 0; i < dist.length; i++) {
            runner.assert(dist[i] >= 0, `Distance ${i} should be non-negative`);
        }
    });
    
    // ========================================================================
    // Utility Tests
    // ========================================================================
    
    runner.test('Mesh: Normalize', () => {
        const mesh = new module.Mesh();
        
        const positions = new Float32Array([
            0, 0, 0, 10, 0, 0, 5, 8, 0
        ]);
        const faces = new Int32Array([0, 1, 2]);
        
        mesh.buildFromArrays(positions, faces);
        mesh.normalize();
        
        const normalized = mesh.getPositions();
        
        // Find max distance from origin
        let maxDist = 0;
        for (let i = 0; i < normalized.length; i += 3) {
            const dist = Math.sqrt(
                normalized[i] ** 2 +
                normalized[i+1] ** 2 +
                normalized[i+2] ** 2
            );
            maxDist = Math.max(maxDist, dist);
        }
        
        runner.assertClose(maxDist, 1.0, 0.01, 'Max distance should be ~1 after normalization');
    });
    
    runner.test('Mesh: Center', () => {
        const mesh = new module.Mesh();
        
        const positions = new Float32Array([
            5, 5, 5, 6, 5, 5, 5.5, 5.9, 5
        ]);
        const faces = new Int32Array([0, 1, 2]);
        
        mesh.buildFromArrays(positions, faces);
        mesh.center();
        
        const centered = mesh.getPositions();
        
        // Compute centroid
        let cx = 0, cy = 0, cz = 0;
        for (let i = 0; i < centered.length; i += 3) {
            cx += centered[i];
            cy += centered[i+1];
            cz += centered[i+2];
        }
        cx /= (centered.length / 3);
        cy /= (centered.length / 3);
        cz /= (centered.length / 3);
        
        const centroidDist = Math.sqrt(cx**2 + cy**2 + cz**2);
        runner.assertClose(centroidDist, 0, 1e-6, 'Centroid should be at origin');
    });
    
    // Run all tests
    return runner.runAll();
}

// Export for use in Node.js or browser
if (typeof module !== 'undefined' && module.exports) {
    module.exports = runTests;
}
