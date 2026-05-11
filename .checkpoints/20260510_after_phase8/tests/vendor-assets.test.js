// Verifierar att Three.js-vendor och graphify-out finns på plats för Fas 3.
const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
let failures = 0;

const requiredFiles = [
  "dashboard/vendor/three.module.js",
  "dashboard/vendor/controls/OrbitControls.js",
  "dashboard/vendor/postprocessing/EffectComposer.js",
  "dashboard/vendor/postprocessing/RenderPass.js",
  "dashboard/vendor/postprocessing/UnrealBloomPass.js",
  "dashboard/vendor/postprocessing/ShaderPass.js",
  "dashboard/vendor/shaders/CopyShader.js",
  "dashboard/vendor/shaders/LuminosityHighPassShader.js",
  "graphify-out/graph.json",
  "graphify-out/manifest.json"
];

for (const rel of requiredFiles) {
  const abs = path.join(root, rel);
  if (!fs.existsSync(abs)) {
    failures += 1;
    console.log(`FAIL missing vendor asset: ${rel}`);
  } else {
    const sz = fs.statSync(abs).size;
    console.log(`PASS ${rel} (${(sz / 1024).toFixed(1)} KB)`);
  }
}

// graph.json must be parseable JSON with nodes/edges arrays.
try {
  const graphPath = path.join(root, "graphify-out/graph.json");
  const raw = fs.readFileSync(graphPath, "utf8");
  const graph = JSON.parse(raw);
  if (!Array.isArray(graph.nodes)) {
    failures += 1;
    console.log("FAIL graph.json missing nodes array");
  } else if (graph.nodes.length === 0) {
    failures += 1;
    console.log("FAIL graph.json has zero nodes");
  } else {
    console.log(`PASS graph.json has ${graph.nodes.length} nodes`);
  }
  if (!Array.isArray(graph.edges) && !Array.isArray(graph.links)) {
    failures += 1;
    console.log("FAIL graph.json missing edges/links array");
  } else {
    const edges = graph.edges || graph.links;
    console.log(`PASS graph.json has ${edges.length} edges/links`);
  }
} catch (e) {
  failures += 1;
  console.log(`FAIL graph.json parse error: ${e.message}`);
}

if (failures > 0) {
  console.log(`\n${failures} failure(s)`);
  process.exit(1);
} else {
  console.log("\nAll vendor-assets checks passed.");
}
