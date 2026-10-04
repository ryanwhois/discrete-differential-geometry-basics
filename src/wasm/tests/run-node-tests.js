#!/usr/bin/env node
// run-node-tests.js
// Run WASM tests in Node.js environment
// Added by Graph Technologies, 2025

const fs = require('fs');
const path = require('path');

// Check for WASM files
const wasmDir = path.join(__dirname, '../../../web/wasm');
const jsFile = path.join(wasmDir, 'ddg.js');
const wasmFile = path.join(wasmDir, 'ddg.wasm');

if (!fs.existsSync(jsFile)) {
    console.error('❌ Error: ddg.js not found!');
    console.error('Run build.sh first to generate WASM files.');
    process.exit(1);
}

if (!fs.existsSync(wasmFile)) {
    console.error('❌ Error: ddg.wasm not found!');
    console.error('Run build.sh first to generate WASM files.');
    process.exit(1);
}

console.log('✅ WASM files found');
console.log('Loading DDG module...\n');

// Load WASM module
const DDGModule = require(jsFile);

DDGModule().then(module => {
    console.log('✅ Module loaded successfully\n');
    
    // Run test suite
    let passed = 0;
    let failed = 0;
    
    function test(name, fn) {
        try {
            fn();
            console.log(`✅ ${name}`);
            passed++;
        } catch (e) {
            console.error(`❌ ${name}`);
            console.error(`   ${e.message}`);
            failed++;
        }
    }
    
    function assert(condition, message) {
        if (!condition) {
            throw new Error(message || 'Assertion failed');
        }
    }
    
    function assertClose(a, b, tolerance = 1e-6, message) {
        if (Math.abs(a - b) > tolerance) {
            throw new Error(message || `Expected ${a} ≈ ${b} (diff: ${Math.abs(a-b)})`);
        }
    }
    
    console.log('Running test suite...');
    console.log('======================\n');
    
    // Test 1: Mesh construction
    test('Mesh construction: Tetrahedron', () => {
        const mesh = new module.Mesh();
        const positions = new Float32Array([0,0,0, 1,0,0, 0,1,0, 0,0,1]);
        const faces = new Int32Array([0,2,1, 0,1,3, 0,3,2, 1,2,3]);
        
        mesh.buildFromArrays(positions, faces);
        
        assert(mesh.numVertices() === 4, 'Should have 4 vertices');
        assert(mesh.numEdges() === 6, 'Should have 6 edges');
        assert(mesh.numFaces() === 4, 'Should have 4 faces');
        assert(mesh.eulerCharacteristic() === 2, 'Euler characteristic should be 2');
    });
    
    // Test 2: Gauss-Bonnet
    test('Gauss-Bonnet: Tetrahedron', () => {
        const mesh = new module.Mesh();
        const positions = new Float32Array([0,0,0, 1,0,0, 0,1,0, 0,0,1]);
        const faces = new Int32Array([0,2,1, 0,1,3, 0,3,2, 1,2,3]);
        
        mesh.buildFromArrays(positions, faces);
        
        const totalK = mesh.totalGaussianCurvature();
        const expectedK = 2 * Math.PI * mesh.eulerCharacteristic();
        
        assertClose(totalK, expectedK, 0.1, 'Gauss-Bonnet should hold');
    });
    
    // Test 3: Gaussian curvature
    test('Gaussian curvature computation', () => {
        const mesh = new module.Mesh();
        const positions = new Float32Array([0,0,0, 1,0,0, 0,1,0, 0,0,1]);
        const faces = new Int32Array([0,2,1, 0,1,3, 0,3,2, 1,2,3]);
        
        mesh.buildFromArrays(positions, faces);
        
        const K = mesh.computeGaussianCurvature();
        
        assert(K.length === 4, 'Should return curvature for each vertex');
        
        // All should be positive (convex)
        for (let i = 0; i < K.length; i++) {
            assert(K[i] > 0, `Vertex ${i} should have positive curvature`);
        }
    });
    
    // Test 4: Mean curvature flow
    test('Mean curvature flow: Mesh updates', () => {
        const mesh = new module.Mesh();
        const positions = new Float32Array([0,0,0, 1,0,0, 0,1,0, 0,0,1]);
        const faces = new Int32Array([0,2,1, 0,1,3, 0,3,2, 1,2,3]);
        
        mesh.buildFromArrays(positions, faces);
        
        const before = mesh.getPositions();
        mesh.meanCurvatureFlowStep(0.001);
        const after = mesh.getPositions();
        
        // Positions should change
        let changed = false;
        for (let i = 0; i < before.length; i++) {
            if (Math.abs(after[i] - before[i]) > 1e-6) {
                changed = true;
                break;
            }
        }
        
        assert(changed, 'Positions should change after smoothing');
    });
    
    // Test 5: Geodesic distance
    test('Heat method: Distance to self is zero', () => {
        const mesh = new module.Mesh();
        const positions = new Float32Array([0,0,0, 1,0,0, 0,1,0, 0,0,1]);
        const faces = new Int32Array([0,2,1, 0,1,3, 0,3,2, 1,2,3]);
        
        mesh.buildFromArrays(positions, faces);
        
        const dist = mesh.computeGeodesicDistance(0);
        
        assertClose(dist[0], 0, 1e-6, 'Distance to self should be 0');
    });
    
    // Test 6: Non-negative distances
    test('Heat method: Non-negative distances', () => {
        const mesh = new module.Mesh();
        const positions = new Float32Array([0,0,0, 1,0,0, 0,1,0, 0,0,1]);
        const faces = new Int32Array([0,2,1, 0,1,3, 0,3,2, 1,2,3]);
        
        mesh.buildFromArrays(positions, faces);
        
        const dist = mesh.computeGeodesicDistance(0);
        
        for (let i = 0; i < dist.length; i++) {
            assert(dist[i] >= -1e-6, `Distance ${i} should be non-negative`);
        }
    });
    
    // Test 7: Normalize
    test('Mesh utilities: Normalize', () => {
        const mesh = new module.Mesh();
        const positions = new Float32Array([0,0,0, 10,0,0, 5,8,0]);
        const faces = new Int32Array([0,1,2]);
        
        mesh.buildFromArrays(positions, faces);
        mesh.normalize();
        
        const normalized = mesh.getPositions();
        
        // Max distance should be ~1
        let maxDist = 0;
        for (let i = 0; i < normalized.length; i += 3) {
            const dist = Math.sqrt(
                normalized[i]**2 + 
                normalized[i+1]**2 + 
                normalized[i+2]**2
            );
            maxDist = Math.max(maxDist, dist);
        }
        
        assertClose(maxDist, 1.0, 0.1, 'Max distance should be ~1');
    });
    
    // Test 8: Conformal parameterization
    test('Conformal parameterization: Returns UV coords', () => {
        const mesh = new module.Mesh();
        const positions = new Float32Array([0,0,0, 1,0,0, 0,1,0, 0,0,1]);
        const faces = new Int32Array([0,2,1, 0,1,3, 0,3,2, 1,2,3]);
        
        mesh.buildFromArrays(positions, faces);
        
        const uv = mesh.spectralConformalParameterization();
        
        assert(uv.length === mesh.numVertices() * 2, 
            'Should return 2D coords for each vertex');
    });
    
    console.log('\n======================');
    console.log(`\nResults: ${passed} passed, ${failed} failed`);
    console.log(`Success rate: ${(passed / (passed + failed) * 100).toFixed(1)}%\n`);
    
    if (failed > 0) {
        console.error('❌ Some tests failed');
        process.exit(1);
    } else {
        console.log('✅ All tests passed!\n');
        process.exit(0);
    }
    
}).catch(err => {
    console.error('❌ Failed to load WASM module:');
    console.error(err);
    process.exit(1);
});
