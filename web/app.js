const TAU = Math.PI * 2;

const fixtures = {
  tetrahedron: {
    label: "Tetrahedron",
    vertices: [
      [0, 0, 1.15],
      [-1, -0.72, -0.72],
      [1, -0.72, -0.72],
      [0, 1.04, -0.72],
    ],
    faces: [[0, 2, 1], [0, 3, 2], [0, 1, 3], [1, 2, 3]],
  },
  octahedron: {
    label: "Octahedron",
    vertices: [[1, 0, 0], [-1, 0, 0], [0, 1, 0], [0, -1, 0], [0, 0, 1], [0, 0, -1]],
    faces: [[0, 2, 4], [2, 1, 4], [1, 3, 4], [3, 0, 4], [2, 0, 5], [1, 2, 5], [3, 1, 5], [0, 3, 5]],
  },
  "open-grid": {
    label: "Open grid",
    vertices: [
      [-1, -1, 0], [0, -1, 0.12], [1, -1, 0],
      [-1, 0, -0.08], [0, 0, 0.28], [1, 0, -0.08],
      [-1, 1, 0], [0, 1, 0.12], [1, 1, 0],
    ],
    faces: [[0, 1, 4], [0, 4, 3], [1, 2, 5], [1, 5, 4], [3, 4, 7], [3, 7, 6], [4, 5, 8], [4, 8, 7]],
  },
};

const algorithms = {
  gaussian: {
    label: "Gaussian curvature",
    title: "Discrete Gaussian Curvature",
    formula: "K<sub>i</sub> = 2π − Σ<sub>j</sub> θ<sub>ij</sub>",
    maturity: "Partial",
    note: "Angle defect at vertex <i>i</i>. Positive <i>K</i> on locally spherical regions, negative <i>K</i> on saddle regions. Boundary vertices use π as the reference angle.",
    numericLabel: "Angle defect tolerance (rad)",
    parameterName: "Tolerance",
    legend: "Gaussian curvature <i>K</i><sub>i</sub>",
  },
  laplacian: {
    label: "Cotan Laplacian",
    title: "Cotan Laplacian",
    formula: "(L f)<sub>i</sub> = ½ Σ (cot α + cot β)(f<sub>j</sub> − f<sub>i</sub>)",
    maturity: "Partial",
    note: "The matrix uses symmetric per-edge assembly. Boundary edges contribute one opposite cotangent; rows sum to zero under the repository sign convention.",
    numericLabel: "Cotangent denominator tolerance",
    parameterName: "Tolerance",
    legend: "Operator weight preview",
  },
  flow: {
    label: "Mean curvature flow",
    title: "Mean Curvature Flow",
    formula: "(M − tL) X<sup>n+1</sup> = M X<sup>n</sup>",
    maturity: "Partial",
    note: "Implicit integration improves stability, but large timesteps still erase detail and can expose poor conditioning on low-quality meshes.",
    numericLabel: "Timestep",
    parameterName: "Timestep",
    legend: "Vertex displacement",
  },
  heat: {
    label: "Heat method",
    title: "Heat Method Geodesics",
    formula: "(M − tL)u = Mδ,  X = −∇u / |∇u|",
    maturity: "Partial",
    note: "The WASM path diffuses heat, integrates weak divergence, and solves a constrained Poisson problem. Source vertex 0 is used in this workbench.",
    numericLabel: "Heat time multiplier",
    parameterName: "Multiplier",
    legend: "Geodesic distance",
  },
  parameterization: {
    label: "Conformal parameterization",
    title: "Boundary-Circle Parameterization",
    formula: "L u = 0  with  u|<sub>∂M</sub> fixed",
    maturity: "Experimental",
    note: "A single boundary loop is mapped by arc length to the unit circle, followed by a strong-Dirichlet harmonic solve. The legacy LSCM API is not yet a true LSCM assembly.",
    numericLabel: "Constraint tolerance",
    parameterName: "Tolerance",
    legend: "UV coordinate field",
  },
  hodge: {
    label: "Hodge decomposition",
    title: "Hodge Decomposition",
    formula: "ω = dα + δβ + γ",
    maturity: "Experimental",
    note: "Exact and coexact projections are available. Harmonic basis construction still requires a complete tree–cotree implementation for positive-genus meshes.",
    numericLabel: "Projection tolerance",
    parameterName: "Tolerance",
    legend: "1-form component preview",
  },
};

const elements = Object.fromEntries([
  "mesh-select", "algorithm-select", "numeric-label", "numeric-input", "numeric-range",
  "decrement", "increment", "reset-button", "run-button", "mesh-canvas", "wireframe-button",
  "fit-button", "expand-button", "legend-title", "algorithm-title", "algorithm-formula",
  "algorithm-maturity", "algorithm-note", "metric-vertices", "metric-edges", "metric-faces",
  "metric-euler", "metric-boundaries", "metric-gauss", "status-mesh", "status-algorithm",
  "status-parameter", "ready-label", "engine-label", "computation-label", "elapsed-label",
].map((id) => [id, document.getElementById(id)]));

const canvas = elements["mesh-canvas"];
const context = canvas.getContext("2d", { alpha: false });
const state = {
  fixtureKey: "tetrahedron",
  algorithmKey: "gaussian",
  vertices: [],
  faces: [],
  field: [],
  rotationX: -0.52,
  rotationY: 0.6,
  zoom: 1,
  panX: 0,
  panY: 0,
  wireframe: true,
  dragging: false,
  pointerX: 0,
  pointerY: 0,
  wasm: null,
  wasmMesh: null,
  runStarted: 0,
};

function cloneFixture(key) {
  const source = fixtures[key];
  return {
    vertices: source.vertices.map((vertex) => [...vertex]),
    faces: source.faces.map((face) => [...face]),
  };
}

function subtract(a, b) { return [a[0] - b[0], a[1] - b[1], a[2] - b[2]]; }
function dot(a, b) { return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]; }
function cross(a, b) { return [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]]; }
function length(vector) { return Math.hypot(vector[0], vector[1], vector[2]); }

function topology(vertices, faces) {
  const edgeUses = new Map();
  const boundaryAdjacency = new Map();
  for (const face of faces) {
    for (let index = 0; index < 3; index += 1) {
      const a = face[index];
      const b = face[(index + 1) % 3];
      const key = a < b ? `${a}:${b}` : `${b}:${a}`;
      edgeUses.set(key, (edgeUses.get(key) ?? 0) + 1);
    }
  }
  for (const [key, uses] of edgeUses) {
    if (uses !== 1) continue;
    const [a, b] = key.split(":").map(Number);
    if (!boundaryAdjacency.has(a)) boundaryAdjacency.set(a, []);
    if (!boundaryAdjacency.has(b)) boundaryAdjacency.set(b, []);
    boundaryAdjacency.get(a).push(b);
    boundaryAdjacency.get(b).push(a);
  }
  let boundaryLoops = 0;
  const visited = new Set();
  for (const start of boundaryAdjacency.keys()) {
    if (visited.has(start)) continue;
    boundaryLoops += 1;
    const stack = [start];
    visited.add(start);
    while (stack.length) {
      const current = stack.pop();
      for (const neighbor of boundaryAdjacency.get(current) ?? []) {
        if (!visited.has(neighbor)) { visited.add(neighbor); stack.push(neighbor); }
      }
    }
  }
  return {
    edges: edgeUses.size,
    euler: vertices.length - edgeUses.size + faces.length,
    boundaryLoops,
    boundaryVertices: new Set(boundaryAdjacency.keys()),
    edgeUses,
  };
}

function gaussianCurvature(vertices, faces) {
  const topo = topology(vertices, faces);
  const angleSums = new Array(vertices.length).fill(0);
  for (const face of faces) {
    for (let local = 0; local < 3; local += 1) {
      const vertex = face[local];
      const u = subtract(vertices[face[(local + 1) % 3]], vertices[vertex]);
      const v = subtract(vertices[face[(local + 2) % 3]], vertices[vertex]);
      const denominator = length(u) * length(v);
      if (denominator <= 1e-12) continue;
      const cosine = Math.max(-1, Math.min(1, dot(u, v) / denominator));
      angleSums[vertex] += Math.acos(cosine);
    }
  }
  return angleSums.map((sum, index) => (topo.boundaryVertices.has(index) ? Math.PI : TAU) - sum);
}

function graphDistances(vertices, faces, source = 0) {
  const adjacency = Array.from({ length: vertices.length }, () => new Map());
  for (const face of faces) {
    for (let i = 0; i < 3; i += 1) {
      const a = face[i]; const b = face[(i + 1) % 3];
      const weight = length(subtract(vertices[a], vertices[b]));
      adjacency[a].set(b, Math.min(adjacency[a].get(b) ?? Infinity, weight));
      adjacency[b].set(a, Math.min(adjacency[b].get(a) ?? Infinity, weight));
    }
  }
  const distances = new Array(vertices.length).fill(Infinity); distances[source] = 0;
  const open = new Set(vertices.map((_, index) => index));
  while (open.size) {
    let current = -1;
    for (const candidate of open) if (current < 0 || distances[candidate] < distances[current]) current = candidate;
    if (current < 0 || !Number.isFinite(distances[current])) break;
    open.delete(current);
    for (const [neighbor, weight] of adjacency[current]) {
      distances[neighbor] = Math.min(distances[neighbor], distances[current] + weight);
    }
  }
  return distances;
}

function previewField() {
  if (state.algorithmKey === "gaussian") return gaussianCurvature(state.vertices, state.faces);
  if (state.algorithmKey === "heat") return graphDistances(state.vertices, state.faces);
  if (state.algorithmKey === "parameterization") return state.vertices.map((vertex) => vertex[0]);
  if (state.algorithmKey === "laplacian") {
    const counts = new Array(state.vertices.length).fill(0);
    const seen = new Set();
    for (const face of state.faces) for (let i = 0; i < 3; i += 1) {
      const a = face[i]; const b = face[(i + 1) % 3]; const key = a < b ? `${a}:${b}` : `${b}:${a}`;
      if (!seen.has(key)) { seen.add(key); counts[a] += 1; counts[b] += 1; }
    }
    const average = counts.reduce((sum, value) => sum + value, 0) / counts.length;
    return counts.map((value) => value - average);
  }
  return state.vertices.map((vertex) => vertex[2]);
}

function rotateVertex([x, y, z]) {
  const cosY = Math.cos(state.rotationY); const sinY = Math.sin(state.rotationY);
  const x1 = x * cosY + y * sinY; const y1 = -x * sinY + y * cosY;
  const cosX = Math.cos(state.rotationX); const sinX = Math.sin(state.rotationX);
  return [x1, y1 * cosX - z * sinX, y1 * sinX + z * cosX];
}

function colorFor(value, maximum, light = 1) {
  const normalized = Math.max(-1, Math.min(1, value / (maximum || 1)));
  const cold = [27, 100, 184]; const neutral = [245, 240, 221]; const hot = [174, 20, 50];
  const from = normalized < 0 ? cold : neutral; const to = normalized < 0 ? neutral : hot;
  const t = Math.abs(normalized);
  const rgb = from.map((channel, index) => Math.round((channel + (to[index] - channel) * t) * light));
  return `rgb(${rgb.map((channel) => Math.max(0, Math.min(255, channel))).join(",")})`;
}

function resizeCanvas() {
  const rect = canvas.getBoundingClientRect();
  const ratio = Math.min(window.devicePixelRatio || 1, 2);
  const width = Math.max(1, Math.round(rect.width * ratio));
  const height = Math.max(1, Math.round(rect.height * ratio));
  if (canvas.width !== width || canvas.height !== height) { canvas.width = width; canvas.height = height; }
  context.setTransform(ratio, 0, 0, ratio, 0, 0);
  draw();
}

function draw() {
  const width = canvas.clientWidth; const height = canvas.clientHeight;
  context.fillStyle = "#f7f6f2"; context.fillRect(0, 0, width, height);
  if (!state.vertices.length) return;

  const transformed = state.vertices.map(rotateVertex);
  const bounds = transformed.reduce((result, vertex) => ({
    minX: Math.min(result.minX, vertex[0]), maxX: Math.max(result.maxX, vertex[0]),
    minZ: Math.min(result.minZ, vertex[2]), maxZ: Math.max(result.maxZ, vertex[2]),
  }), { minX: Infinity, maxX: -Infinity, minZ: Infinity, maxZ: -Infinity });
  const extent = Math.max(bounds.maxX - bounds.minX, bounds.maxZ - bounds.minZ, 0.1);
  const reservedBottom = width < 700 ? 100 : 120;
  const scale = Math.min(width * 0.68, (height - reservedBottom) * 0.76) / extent * state.zoom;
  const centerX = width / 2 + state.panX;
  const centerY = (height - reservedBottom) / 2 + 15 + state.panY;
  const projected = transformed.map((vertex) => ({
    x: centerX + vertex[0] * scale,
    y: centerY - vertex[2] * scale,
    depth: vertex[1],
  }));
  const maximum = Math.max(...state.field.map((value) => Math.abs(value)), 1e-9);
  const faces = state.faces.map((face) => ({
    face,
    depth: face.reduce((sum, index) => sum + projected[index].depth, 0) / 3,
    field: face.reduce((sum, index) => sum + state.field[index], 0) / 3,
  })).sort((a, b) => a.depth - b.depth);

  for (const item of faces) {
    const [a, b, c] = item.face.map((index) => projected[index]);
    const p0 = state.vertices[item.face[0]]; const p1 = state.vertices[item.face[1]]; const p2 = state.vertices[item.face[2]];
    const normal = cross(subtract(p1, p0), subtract(p2, p0));
    const lighting = 0.82 + 0.18 * Math.abs(normal[2] / (length(normal) || 1));
    context.beginPath(); context.moveTo(a.x, a.y); context.lineTo(b.x, b.y); context.lineTo(c.x, c.y); context.closePath();
    context.fillStyle = colorFor(item.field, maximum, lighting); context.fill();
    if (state.wireframe) { context.strokeStyle = "rgba(20,31,38,.78)"; context.lineWidth = 1; context.stroke(); }
  }

  if (state.wireframe) for (const point of projected) {
    context.beginPath(); context.arc(point.x, point.y, width < 600 ? 2.1 : 2.6, 0, TAU);
    context.fillStyle = "#101820"; context.fill();
  }
}

function updateDiagnostics() {
  const topo = topology(state.vertices, state.faces);
  const curvature = gaussianCurvature(state.vertices, state.faces);
  const total = curvature.reduce((sum, value) => sum + value, 0);
  const error = Math.abs(total - TAU * topo.euler);
  elements["metric-vertices"].textContent = state.vertices.length;
  elements["metric-edges"].textContent = topo.edges;
  elements["metric-faces"].textContent = state.faces.length;
  elements["metric-euler"].textContent = topo.euler;
  elements["metric-boundaries"].textContent = topo.boundaryLoops;
  elements["metric-gauss"].textContent = error.toExponential(2);
}

function updateAlgorithm() {
  const metadata = algorithms[state.algorithmKey];
  elements["algorithm-title"].textContent = metadata.title;
  elements["algorithm-formula"].innerHTML = metadata.formula;
  elements["algorithm-maturity"].textContent = metadata.maturity;
  elements["algorithm-note"].innerHTML = metadata.note;
  elements["numeric-label"].textContent = metadata.numericLabel;
  elements["legend-title"].innerHTML = metadata.legend;
  elements["status-algorithm"].textContent = metadata.label;
  updateParameterStatus();
  state.field = previewField();
  draw();
}

function updateParameterStatus() {
  const metadata = algorithms[state.algorithmKey];
  const value = elements["numeric-input"].value.replace("e-", "e−");
  elements["status-parameter"].textContent = `${metadata.parameterName}: ${value}${state.algorithmKey === "gaussian" ? " rad" : ""}`;
}

function setFixture(key) {
  const fixture = cloneFixture(key);
  state.fixtureKey = key; state.vertices = fixture.vertices; state.faces = fixture.faces;
  elements["status-mesh"].textContent = fixtures[key].label;
  state.field = previewField();
  updateDiagnostics();
  fitView();
  syncWasmMesh();
}

function fitView() {
  state.rotationX = state.fixtureKey === "open-grid" ? -0.82 : -0.52;
  state.rotationY = state.fixtureKey === "open-grid" ? 0.28 : 0.6;
  state.zoom = 1; state.panX = 0; state.panY = 0; draw();
}

function setBusy(busy, message = "idle") {
  elements["run-button"].disabled = busy;
  elements["ready-label"].textContent = busy ? "Computing" : "Ready";
  elements["computation-label"].textContent = `Computation: ${message}`;
  if (!busy && state.runStarted) {
    const elapsed = (performance.now() - state.runStarted) / 1000;
    elements["elapsed-label"].textContent = `00:00:${elapsed.toFixed(2).padStart(5, "0")}`;
  }
}

function syncWasmMesh() {
  if (!state.wasm) return;
  try {
    if (state.wasmMesh?.delete) state.wasmMesh.delete();
    state.wasmMesh = new state.wasm.Mesh();
    state.wasmMesh.buildFromArrays(new Float64Array(state.vertices.flat()), new Int32Array(state.faces.flat()));
  } catch (error) {
    console.warn("WASM mesh synchronization failed", error);
    state.wasmMesh = null;
  }
}

async function runAnalysis() {
  state.runStarted = performance.now(); setBusy(true, "running");
  await new Promise((resolve) => requestAnimationFrame(resolve));
  try {
    if (state.wasmMesh) {
      if (state.algorithmKey === "gaussian") state.field = Array.from(state.wasmMesh.computeGaussianCurvature());
      else if (state.algorithmKey === "heat") state.field = Array.from(state.wasmMesh.computeGeodesicDistance(0));
      else if (state.algorithmKey === "flow") {
        state.wasmMesh.meanCurvatureFlowStep(Number(elements["numeric-input"].value) || 1e-6);
        const positions = Array.from(state.wasmMesh.getPositions());
        state.vertices = Array.from({ length: positions.length / 3 }, (_, index) => positions.slice(index * 3, index * 3 + 3));
        state.field = previewField();
      } else if (state.algorithmKey === "parameterization") {
        const topo = topology(state.vertices, state.faces);
        const uv = topo.boundaryLoops ? state.wasmMesh.boundaryCircleParameterization() : state.wasmMesh.spectralConformalParameterization();
        state.field = Array.from({ length: uv.length / 2 }, (_, index) => uv[index * 2]);
      } else state.field = previewField();
    } else {
      state.field = previewField();
      await new Promise((resolve) => setTimeout(resolve, 140));
    }
    updateDiagnostics(); draw(); setBusy(false, "complete");
  } catch (error) {
    console.error(error);
    elements["ready-label"].textContent = "Analysis failed";
    elements["computation-label"].textContent = "Computation: error";
    elements["run-button"].disabled = false;
  }
}

function updateRange(value) {
  const exponent = Math.max(-12, Math.min(-3, Number(value)));
  elements["numeric-range"].value = exponent;
  elements["numeric-input"].value = `1.0e${exponent}`;
  elements["numeric-range"].style.background = `linear-gradient(90deg, var(--blue) 0 ${((exponent + 12) / 9) * 100}%, #70808a ${((exponent + 12) / 9) * 100}% 100%)`;
  updateParameterStatus();
}

function bindEvents() {
  elements["mesh-select"].addEventListener("change", (event) => setFixture(event.target.value));
  elements["algorithm-select"].addEventListener("change", (event) => { state.algorithmKey = event.target.value; updateAlgorithm(); });
  elements["numeric-range"].addEventListener("input", (event) => updateRange(event.target.value));
  elements["numeric-input"].addEventListener("change", () => updateParameterStatus());
  elements.decrement.addEventListener("click", () => updateRange(Number(elements["numeric-range"].value) - 1));
  elements.increment.addEventListener("click", () => updateRange(Number(elements["numeric-range"].value) + 1));
  elements["reset-button"].addEventListener("click", () => setFixture(state.fixtureKey));
  elements["run-button"].addEventListener("click", runAnalysis);
  elements["wireframe-button"].addEventListener("click", () => {
    state.wireframe = !state.wireframe;
    elements["wireframe-button"].classList.toggle("active", state.wireframe);
    elements["wireframe-button"].setAttribute("aria-pressed", state.wireframe); draw();
  });
  elements["fit-button"].addEventListener("click", fitView);
  elements["expand-button"].addEventListener("click", async () => {
    if (document.fullscreenElement) await document.exitFullscreen();
    else await document.querySelector(".viewport-panel").requestFullscreen();
  });
  canvas.addEventListener("pointerdown", (event) => {
    state.dragging = true; state.pointerX = event.clientX; state.pointerY = event.clientY; canvas.setPointerCapture(event.pointerId);
  });
  canvas.addEventListener("pointermove", (event) => {
    if (!state.dragging) return;
    const dx = event.clientX - state.pointerX; const dy = event.clientY - state.pointerY;
    state.pointerX = event.clientX; state.pointerY = event.clientY;
    if (event.shiftKey) { state.panX += dx; state.panY += dy; }
    else { state.rotationY += dx * 0.008; state.rotationX += dy * 0.008; }
    draw();
  });
  const endDrag = () => { state.dragging = false; };
  canvas.addEventListener("pointerup", endDrag); canvas.addEventListener("pointercancel", endDrag);
  canvas.addEventListener("wheel", (event) => {
    event.preventDefault(); state.zoom = Math.max(0.45, Math.min(2.8, state.zoom * Math.exp(-event.deltaY * 0.001))); draw();
  }, { passive: false });
  document.querySelector(".menu-button").addEventListener("click", (event) => {
    const nav = document.querySelector(".primary-nav"); const open = nav.classList.toggle("open");
    event.currentTarget.setAttribute("aria-expanded", open);
  });
  document.querySelectorAll(".primary-nav a").forEach((link) => link.addEventListener("click", () => document.querySelector(".primary-nav").classList.remove("open")));
  window.addEventListener("resize", resizeCanvas);
  document.addEventListener("fullscreenchange", resizeCanvas);
}

function loadWasm() {
  const script = document.createElement("script");
  script.src = "/web/wasm/ddg.js";
  script.onload = async () => {
    try {
      if (typeof window.DDGModule !== "function") throw new Error("DDGModule export not found");
      state.wasm = await window.DDGModule({ locateFile: (file) => `/web/wasm/${file}` });
      elements["engine-label"].textContent = "Engine: C++ / WASM";
      syncWasmMesh();
    } catch (error) { console.warn("WASM module unavailable; using JS reference", error); }
  };
  script.onerror = () => { elements["engine-label"].textContent = "Engine: JS reference"; };
  document.head.append(script);
}

bindEvents();
updateRange(-6);
setFixture("tetrahedron");
updateAlgorithm();
resizeCanvas();
loadWasm();

