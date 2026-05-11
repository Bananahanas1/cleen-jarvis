/**
 * Brain3D - svensk 3D-visualisering av Jarvis neurala karta.
 */
import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { UnrealBloomPass } from 'three/addons/postprocessing/UnrealBloomPass.js';
import { EffectComposer } from 'three/addons/postprocessing/EffectComposer.js';
import { RenderPass } from 'three/addons/postprocessing/RenderPass.js';

// Reference-inspired colors: electric blue axons + warm orange/gold synapses
const REGION_COLORS = {
    sensory_cortex: 0xff6600,   // orange - sensory fire
    feature_layer: 0x00c8ff,   // electric blue
    association: 0xff8c00,   // amber
    concept_layer: 0xffcc00,   // gold
    predictive: 0xff3399,   // hot pink
    motor_cortex: 0xffaa00,   // orange - changed from red-orange
    cerebellum: 0x00ff88,   // mint green
    reflex_arc: 0xff8800,   // orange - changed from red
    brainstem: 0xff6600,   // orange
    hippocampus: 0x00e5ff,   // cyan
    prefrontal: 0xaa88ff,   // lavender
};

const REGION_LAYOUT = {
    prefrontal: { x: -2.1, y: 3.5, z: 0.0 },
    motor_cortex: { x: 1.8, y: 3.2, z: 0.6 },
    sensory_cortex: { x: 3.5, y: 0.7, z: 1.4 },
    feature_layer: { x: 1.1, y: -1.1, z: 0.7 },
    association: { x: -0.3, y: 1.1, z: 0.0 },
    concept_layer: { x: -1.8, y: 2.1, z: -0.7 },
    predictive: { x: -3.2, y: -0.3, z: -0.7 },
    hippocampus: { x: -1.8, y: -1.4, z: 0.7 },
    cerebellum: { x: 0.3, y: -2.8, z: -0.7 },
    brainstem: { x: 0.0, y: -1.8, z: 0.0 },
    reflex_arc: { x: 1.8, y: -2.1, z: 1.1 },
};

const KNOWLEDGE_KIND_COLORS = {
    jarvis_memory: 0x44ffaa,
    neurolinked_semantic: 0x00e5ff,
    graphify_code: 0xffaa33,
    obsidian_note: 0xaa88ff,
    decision: 0xffcc00,
    open_issue: 0xff5577,
};

const KNOWLEDGE_ANCHORS = {
    chat: { x: -0.25, y: -3.65, z: 2.0 },
    jarvis_bridge: { x: 3.6, y: -2.8, z: 1.4 },
    memory_bridge: { x: -3.6, y: -2.5, z: 1.2 },
    graphify: { x: 0.55, y: 1.05, z: 1.55 },
    obsidian: { x: -2.7, y: 3.1, z: 1.2 },
};

const KNOWLEDGE_EVENT_COLORS = {
    memory_save: 0x44ffaa,
    memory_recall: 0x00e5ff,
    knowledge_search: 0xffaa33,
    obsidian_read: 0xaa88ff,
    assistant_response: 0x58e664,
    bridge_status: 0x4ad3f5,
    bridge_activity: 0xff8844,
};

const GRAPH_POINT_BASE_SIZE = 0.118;
const GRAPH_POINT_HIGHLIGHT_SIZE = 0.225;
const GRAPH_POINT_DEGREE_SCALE = 0.00085;
// Light mode: keep dashboard responsive on 8GB VRAM / 16GB RAM machines.
const GRAPH_IDLE_EDGE_LIMIT = 900;
const GRAPH_IDLE_EDGE_OPACITY = 0.012;
const GRAPH_IDLE_EDGE_ACTIVITY_OPACITY = 0.010;
const GRAPH_FOCUS_EDGE_OPACITY = 0.17;
const THOUGHT_LANE_OPACITY = 0.055;
const OBSIDIAN_LANE_OPACITY = 0.16;

const CONNECTIONS = [
    ['sensory_cortex', 'feature_layer'],
    ['feature_layer', 'association'],
    ['association', 'concept_layer'],
    ['concept_layer', 'prefrontal'],
    ['prefrontal', 'motor_cortex'],
    ['motor_cortex', 'cerebellum'],
    ['brainstem', 'reflex_arc'],
    ['brainstem', 'cerebellum'],
    ['hippocampus', 'association'],
    ['hippocampus', 'prefrontal'],
    ['predictive', 'association'],
    ['predictive', 'concept_layer'],
    ['sensory_cortex', 'reflex_arc'],
    ['association', 'hippocampus'],
    ['prefrontal', 'predictive'],
    ['feature_layer', 'concept_layer'],
    ['sensory_cortex', 'association'],
    ['motor_cortex', 'reflex_arc'],
    ['brainstem', 'hippocampus'],
    ['prefrontal', 'concept_layer'],
];

const REGION_DESCRIPTIONS = {
    sensory_cortex: 'Primär sensorisk bearbetning - rå indata från omgivningen',
    feature_layer: 'Mönsterigenkänning och särdragsanalys',
    association: 'Kopplar ihop signaler mellan flera sinnen',
    concept_layer: 'Bygger abstrakta begrepp',
    predictive: 'Förutsägelser och förväntningar',
    motor_cortex: 'Planering och utförande av handlingar',
    cerebellum: 'Timing, koordination och finjustering',
    reflex_arc: 'Snabba reflexiva svar',
    brainstem: 'Autonom reglering och vakenhet',
    hippocampus: 'Minnesbildning och konsolidering',
    prefrontal: 'Planering, beslutsfattande och självkontroll',
};

const SVENSKA_REGIONBESKRIVNINGAR = {
    sensory_cortex: 'Primär sensorisk bearbetning - rå indata från omgivningen',
    feature_layer: 'Mönsterigenkänning och särdragsanalys',
    association: 'Kopplar ihop signaler mellan flera sinnen',
    concept_layer: 'Bygger abstrakta begrepp',
    predictive: 'Förutsägelser och förväntningar',
    motor_cortex: 'Planering och utförande av handlingar',
    cerebellum: 'Timing, koordination och finjustering',
    reflex_arc: 'Snabba reflexiva svar',
    brainstem: 'Autonom reglering och vakenhet',
    hippocampus: 'Minnesbildning och konsolidering',
    prefrontal: 'Planering, beslutsfattande och självkontroll',
};

const SVENSKA_REGIONNAMN = {
    sensory_cortex: 'sensorisk bark',
    feature_layer: 'mönsterlager',
    association: 'associationsområde',
    concept_layer: 'begreppslager',
    predictive: 'prediktivt område',
    motor_cortex: 'motorisk bark',
    cerebellum: 'lillhjärna',
    reflex_arc: 'reflexbåge',
    brainstem: 'hjärnstam',
    hippocampus: 'hippocampus',
    prefrontal: 'prefrontal bark',
};

function regionLabel(regionName) {
    return SVENSKA_REGIONNAMN[regionName] || regionName.replace(/_/g, ' ');
}

function isTypingTarget(target) {
    if (!(target instanceof Element)) return false;
    if (target.isContentEditable) return true;

    const tagName = target.tagName;
    if (tagName === 'INPUT' || tagName === 'TEXTAREA' || tagName === 'SELECT') {
        return true;
    }

    return Boolean(target.closest('input, textarea, select, [contenteditable="true"]'));
}

function hashString(value) {
    const text = String(value || 'knowledge');
    let hash = 2166136261;
    for (let i = 0; i < text.length; i++) {
        hash ^= text.charCodeAt(i);
        hash = Math.imul(hash, 16777619);
    }
    return hash >>> 0;
}

function seededUnit(value, salt = '') {
    const seed = hashString(`${value}:${salt}`);
    const raw = Math.sin(seed) * 10000;
    return raw - Math.floor(raw);
}

// Glowing orb texture for synaptic nodes
function createSynapseTexture() {
    const size = 128;
    const canvas = document.createElement('canvas');
    canvas.width = size;
    canvas.height = size;
    const ctx = canvas.getContext('2d');
    const half = size / 2;
    // Bright core + wide soft glow like reference images
    const g = ctx.createRadialGradient(half, half, 0, half, half, half);
    g.addColorStop(0, 'rgba(255,255,255,1.0)');
    g.addColorStop(0.3, 'rgba(255,180,50,1.0)');
    g.addColorStop(0.6, 'rgba(255,100,20,0.5)');
    g.addColorStop(1, 'rgba(0,0,0,0.0)');
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, size, size);
    const tex = new THREE.CanvasTexture(canvas);
    tex.needsUpdate = true;
    return tex;
}

// Smaller cool-blue texture for axon connection dots
function createAxonTexture() {
    const size = 64;
    const canvas = document.createElement('canvas');
    canvas.width = size;
    canvas.height = size;
    const ctx = canvas.getContext('2d');
    const half = size / 2;
    const g = ctx.createRadialGradient(half, half, 0, half, half, half);
    g.addColorStop(0, 'rgba(180,240,255,1.0)');
    g.addColorStop(0.2, 'rgba(80,200,255,0.7)');
    g.addColorStop(0.5, 'rgba(0,150,255,0.2)');
    g.addColorStop(1, 'rgba(0,0,0,0.0)');
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, size, size);
    const tex = new THREE.CanvasTexture(canvas);
    tex.needsUpdate = true;
    return tex;
}

function createStarTexture() {
    const size = 48;
    const canvas = document.createElement('canvas');
    canvas.width = size;
    canvas.height = size;
    const ctx = canvas.getContext('2d');
    const half = size / 2;
    const g = ctx.createRadialGradient(half, half, 0, half, half, half);
    g.addColorStop(0.0, 'rgba(255,255,255,1.0)');
    g.addColorStop(0.18, 'rgba(210,245,255,0.85)');
    g.addColorStop(0.48, 'rgba(100,180,255,0.18)');
    g.addColorStop(1.0, 'rgba(0,0,0,0.0)');
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, size, size);
    const tex = new THREE.CanvasTexture(canvas);
    tex.needsUpdate = true;
    return tex;
}

function createNebulaTexture(colorA, colorB, colorC) {
    const size = 512;
    const canvas = document.createElement('canvas');
    canvas.width = size;
    canvas.height = size;
    const ctx = canvas.getContext('2d');

    // Layer soft gradients and dark holes, but never use additive canvas blending:
    // WebView2/GPU compositing can otherwise accumulate the nebula into a white blob.
    ctx.clearRect(0, 0, size, size);
    const base = ctx.createRadialGradient(220, 230, 0, 250, 250, 235);
    base.addColorStop(0.00, colorA);
    base.addColorStop(0.28, colorB);
    base.addColorStop(0.68, colorC);
    base.addColorStop(1.00, 'rgba(0,0,0,0)');
    ctx.fillStyle = base;
    ctx.fillRect(0, 0, size, size);

    for (let i = 0; i < 12; i++) {
        const x = 80 + Math.random() * 360;
        const y = 70 + Math.random() * 360;
        const r = 35 + Math.random() * 120;
        const cloud = ctx.createRadialGradient(x, y, 0, x, y, r);
        cloud.addColorStop(0, i % 2 === 0 ? colorA : colorB);
        cloud.addColorStop(0.62, i % 3 === 0 ? colorC : 'rgba(80,120,180,0.018)');
        cloud.addColorStop(1, 'rgba(0,0,0,0)');
        ctx.globalCompositeOperation = 'source-over';
        ctx.fillStyle = cloud;
        ctx.fillRect(0, 0, size, size);
    }

    ctx.globalCompositeOperation = 'destination-out';
    for (let i = 0; i < 26; i++) {
        const x = Math.random() * size;
        const y = Math.random() * size;
        const r = 18 + Math.random() * 80;
        const hole = ctx.createRadialGradient(x, y, 0, x, y, r);
        hole.addColorStop(0, 'rgba(0,0,0,0.42)');
        hole.addColorStop(1, 'rgba(0,0,0,0)');
        ctx.fillStyle = hole;
        ctx.fillRect(0, 0, size, size);
    }
    ctx.globalCompositeOperation = 'source-over';

    const tex = new THREE.CanvasTexture(canvas);
    tex.needsUpdate = true;
    return tex;
}

// ── Tube helpers ────────────────────────────────────────────────────────────
const _T_SEGS = 20, _C_RS = 8, _G_RS = 5;
const _tv = new THREE.Vector3(), _tn = new THREE.Vector3(),
      _tb = new THREE.Vector3(), _ta = new THREE.Vector3(),
      _tp = new THREE.Vector3();
const _pts2  = Array.from({ length: _T_SEGS + 2 }, () => new THREE.Vector3());
const _tans2 = Array.from({ length: _T_SEGS + 2 }, () => new THREE.Vector3());

function _rotV(v, axis, angle) {
    const c = Math.cos(angle), s = Math.sin(angle), d = axis.dot(v);
    const cx = axis.y*v.z - axis.z*v.y, cy = axis.z*v.x - axis.x*v.z, cz = axis.x*v.y - axis.y*v.x;
    v.x = v.x*c + cx*s + axis.x*d*(1-c);
    v.y = v.y*c + cy*s + axis.y*d*(1-c);
    v.z = v.z*c + cz*s + axis.z*d*(1-c);
}
function _allocTube(pSegs, rSegs) {
    const vN = (pSegs+1)*rSegs;
    const pos = new Float32Array(vN*3), nor = new Float32Array(vN*3), uv = new Float32Array(vN*2);
    const idx = [];
    for (let i = 0; i < pSegs; i++) for (let j = 0; j < rSegs; j++) {
        const a = i*rSegs+j, b = i*rSegs+(j+1)%rSegs,
              c = (i+1)*rSegs+(j+1)%rSegs, d = (i+1)*rSegs+j;
        idx.push(a,b,d, b,c,d);
    }
    for (let i = 0; i <= pSegs; i++) for (let j = 0; j < rSegs; j++) {
        const vi = (i*rSegs+j)*2; uv[vi] = i/pSegs; uv[vi+1] = j/rSegs;
    }
    const geo = new THREE.BufferGeometry();
    const pa = new THREE.BufferAttribute(pos, 3); pa.usage = THREE.DynamicDrawUsage;
    const na = new THREE.BufferAttribute(nor, 3); na.usage = THREE.DynamicDrawUsage;
    geo.setAttribute('position', pa); geo.setAttribute('normal', na);
    geo.setAttribute('uv', new THREE.BufferAttribute(uv, 2)); geo.setIndex(idx);
    return geo;
}
function _fillTube(geo, curve, radius, pSegs, rSegs) {
    const posArr = geo.attributes.position.array, norArr = geo.attributes.normal.array;
    const {v0, v1, v2} = curve;
    for (let i = 0; i <= pSegs; i++) {
        const t = i/pSegs;
        curve.getPoint(t, _pts2[i]);
        const s = 1-t;
        _tans2[i].set(2*s*(v1.x-v0.x)+2*t*(v2.x-v1.x), 2*s*(v1.y-v0.y)+2*t*(v2.y-v1.y), 2*s*(v1.z-v0.z)+2*t*(v2.z-v1.z)).normalize();
    }
    _tv.copy(_tans2[0]); _tn.set(0,1,0);
    if (Math.abs(_tv.dot(_tn)) > 0.9) _tn.set(1,0,0);
    _tb.crossVectors(_tv, _tn).normalize(); _tn.crossVectors(_tb, _tv).normalize();
    for (let i = 0; i <= pSegs; i++) {
        if (i > 0) {
            _tp.copy(_tans2[i-1]); _tv.copy(_tans2[i]);
            const cosA = Math.max(-1, Math.min(1, _tp.dot(_tv)));
            if (cosA < 0.9999) { _ta.crossVectors(_tp, _tv).normalize(); _rotV(_tn, _ta, Math.acos(cosA)); _tb.crossVectors(_tv, _tn).normalize(); _tn.crossVectors(_tb, _tv).normalize(); }
        }
        for (let j = 0; j < rSegs; j++) {
            const th = (j/rSegs)*Math.PI*2, cs = Math.cos(th), sn = Math.sin(th);
            const nx = cs*_tn.x+sn*_tb.x, ny = cs*_tn.y+sn*_tb.y, nz = cs*_tn.z+sn*_tb.z;
            const vi = (i*rSegs+j)*3;
            posArr[vi]  =_pts2[i].x+nx*radius; posArr[vi+1]=_pts2[i].y+ny*radius; posArr[vi+2]=_pts2[i].z+nz*radius;
            norArr[vi]  =nx; norArr[vi+1]=ny; norArr[vi+2]=nz;
        }
    }
    geo.attributes.position.needsUpdate = true; geo.attributes.normal.needsUpdate = true;
    geo.computeBoundingSphere();
}
// ────────────────────────────────────────────────────────────────────────────

export class Brain3D {
    constructor(container) {
        this.container = container;
        this.time = 0;
        this.regionMeshes = {};
        this.regionGlows = {};
        this.regionData = {};
        this.labels = {};
        this.lastState = null;
        this.graphView = false;
        this.signals = [];
        this.maxSignals = 200;
        this.cosmosLayers = [];
        this.nebulaSprites = [];
        this.knowledgeNodes = new Map();
        this.knowledgeNodeMeshes = [];
        this.knowledgeNodeHitMeshes = [];
        this.knowledgeGraphNodes = new Map();
        this.knowledgeGraphNodeArray = [];
        this.knowledgeGraphPointMesh = null;
        this.knowledgeGraphLineMesh = null;
        this.knowledgeGraphEdges = [];
        this.knowledgeGraphFocusLineMesh = null;
        this.knowledgeGraphFocusSprite = null;
        this.knowledgeActivity = { memory_load: 0, graph_load: 0, obsidian_load: 0, synapse_load: 0 };
        this.knowledgeThoughtLineMesh = null;
        this.obsidianThoughtLineMesh = null;
        this.obsidianThoughtRoutes = [];
        this.obsidianPulseTimer = 0;
        this.obsidianPulseCursor = 0;
        this.knowledgeRegionStats = {};
        this.regionHotspots = {};
        this.knowledgePackets = [];

        // Scene
        this.scene = new THREE.Scene();
        this.scene.background = new THREE.Color(0x010204);
        this.knowledgeGroup = new THREE.Group();
        this.knowledgeGroup.name = 'knowledge-constellation';
        this.knowledgeGraphGroup = new THREE.Group();
        this.knowledgeGraphGroup.name = 'knowledge-spiderweb';
        this.knowledgeNodeGroup = new THREE.Group();
        this.knowledgeNodeGroup.name = 'knowledge-nodes';
        this.knowledgePacketGroup = new THREE.Group();
        this.knowledgePacketGroup.name = 'knowledge-packets';
        this.knowledgeGroup.add(this.knowledgeGraphGroup, this.knowledgeNodeGroup, this.knowledgePacketGroup);
        this.scene.add(this.knowledgeGroup);
        // Removed fog for clarity

        // Camera
        this.camera = new THREE.PerspectiveCamera(
            50, window.innerWidth / window.innerHeight, 0.01, 100
        );
        this.camera.position.set(0, 0, 9.0);

        // Renderer
        this.renderer = new THREE.WebGLRenderer({
            antialias: true, alpha: false, powerPreference: 'high-performance'
        });
        this.renderer.setSize(window.innerWidth, window.innerHeight);
        this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
        // Keep the WebView2 surface opaque. A transparent composer can expose
        // the browser's white backing store before WebGL/post-processing settles.
        this.renderer.setClearColor(0x010204, 1);
        this.renderer.domElement.style.background = '#010204';
        this.renderer.outputColorSpace = THREE.SRGBColorSpace;
        this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
        this.renderer.toneMappingExposure = 1.15;
        container.appendChild(this.renderer.domElement);

        // Post-processing is kept available, but direct rendering is the safe
        // default in WebView2. UnrealBloomPass can wash the whole canvas white
        // on some Windows/WebView GPU combinations before the scene settles.
        this.usePostProcessing = false;
        this.composer = new EffectComposer(this.renderer);
        this.composer.addPass(new RenderPass(this.scene, this.camera));
        this.bloomPass = new UnrealBloomPass(
            new THREE.Vector2(window.innerWidth, window.innerHeight),
            0.45,  // strength — reduced for clarity
            0.2,   // radius — sharper
            0.4    // threshold — higher so only bright parts glow
        );
        this.composer.addPass(this.bloomPass);

        // Controls
        this.controls = new OrbitControls(this.camera, this.renderer.domElement);
        this.controls.enableDamping = true;
        this.controls.dampingFactor = 0.06;
        this.controls.rotateSpeed = 0.5;
        this.controls.zoomToCursor = true;
        this.controls.autoRotate = false;
        this.controls.autoRotateSpeed = 0.18;
        this.controls.target.set(0, 0.1, 0);
        this.controls.minDistance = 1.5;
        this.controls.maxDistance = 12;

        // Lights — warm point light at center like glowing soma
        this.scene.add(new THREE.AmbientLight(0x050820, 0.4));
        const coreLight = new THREE.PointLight(0xff6600, 1.5, 6);
        coreLight.position.set(0, 0.2, 0);
        this.scene.add(coreLight);
        const blueLight = new THREE.PointLight(0x0088ff, 0.8, 8);
        blueLight.position.set(-1, 0.8, -0.5);
        this.scene.add(blueLight);
        this._coreLight = coreLight;

        // A procedural 3D cosmos gives depth behind the neural map without
        // bringing back the old falling/shooting-star effect.
        this._createCosmosBackdrop();

        // Textures
        this.synapseTexture = createSynapseTexture();
        this.axonTexture = createAxonTexture();

        // Label container
        this.labelContainer = document.createElement('div');
        this.labelContainer.id = 'region-labels';
        this.labelContainer.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;pointer-events:none;z-index:5;';
        document.body.appendChild(this.labelContainer);

        // Axon tubes (3D animated, Phong-lit)
        this._connectionLines = [];
        this._axonData = [];
        this._buildAxonLines();

        // Signal particles
        this._initSignalSystem();
        this._initKnowledgeEvents();

        // Graph view button
        this._createGraphViewButton();

        window.addEventListener('resize', () => this._onResize());
        this.raycaster = new THREE.Raycaster();
        this.raycaster.params.Points = { threshold: 0.42 };
        this.mouse = new THREE.Vector2();
        container.addEventListener('click', (e) => this._onClick(e));
        this._initKeyboardShortcuts();
    }

    _createCosmosBackdrop() {
        this.cosmosGroup = new THREE.Group();
        this.cosmosGroup.name = 'procedural-cosmos';
        this.scene.add(this.cosmosGroup);

        const starTexture = createStarTexture();
        const layers = [
            { count: 1300, radiusMin: 13, radiusMax: 23, size: 0.035, opacity: 0.38, tint: 0xbfeaff },
            { count: 900, radiusMin: 18, radiusMax: 34, size: 0.052, opacity: 0.30, tint: 0xffd29a },
            { count: 550, radiusMin: 25, radiusMax: 46, size: 0.085, opacity: 0.20, tint: 0xd8b9ff },
        ];

        for (const layer of layers) {
            const geometry = new THREE.BufferGeometry();
            const positions = new Float32Array(layer.count * 3);
            const colors = new Float32Array(layer.count * 3);
            const tint = new THREE.Color(layer.tint);

            for (let i = 0; i < layer.count; i++) {
                const radius = layer.radiusMin + Math.random() * (layer.radiusMax - layer.radiusMin);
                const theta = Math.random() * Math.PI * 2;
                const phi = Math.acos(2 * Math.random() - 1);
                const i3 = i * 3;
                positions[i3] = radius * Math.sin(phi) * Math.cos(theta);
                positions[i3 + 1] = radius * Math.sin(phi) * Math.sin(theta) * 0.72;
                positions[i3 + 2] = -Math.abs(radius * Math.cos(phi)) - 4;

                const color = tint.clone().lerp(new THREE.Color(0xffffff), Math.random() * 0.55);
                colors[i3] = color.r;
                colors[i3 + 1] = color.g;
                colors[i3 + 2] = color.b;
            }

            geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
            geometry.setAttribute('color', new THREE.BufferAttribute(colors, 3));
            const material = new THREE.PointsMaterial({
                size: layer.size,
                map: starTexture,
                transparent: true,
                opacity: layer.opacity,
                blending: THREE.AdditiveBlending,
                depthWrite: false,
                vertexColors: true,
                sizeAttenuation: true,
            });
            const stars = new THREE.Points(geometry, material);
            this.cosmosGroup.add(stars);
            this.cosmosLayers.push(stars);
        }

        const nebulae = [
            {
                position: [-11.2, 4.3, -24],
                scale: [7.2, 3.1, 1],
                colors: ['rgba(0,210,255,0.045)', 'rgba(190,80,255,0.030)', 'rgba(255,145,40,0.012)'],
                opacity: 0.038,
                rotation: -0.22,
            },
            {
                position: [10.4, -4.1, -27],
                scale: [8.4, 3.5, 1],
                colors: ['rgba(255,100,20,0.040)', 'rgba(255,205,80,0.024)', 'rgba(0,220,255,0.010)'],
                opacity: 0.032,
                rotation: 0.38,
            },
            {
                position: [0.5, 8.8, -34],
                scale: [9.5, 2.8, 1],
                colors: ['rgba(90,150,255,0.026)', 'rgba(70,200,255,0.032)', 'rgba(220,70,255,0.010)'],
                opacity: 0.026,
                rotation: 0.05,
            },
        ];

        for (const nebula of nebulae) {
            const texture = createNebulaTexture(...nebula.colors);
            const material = new THREE.SpriteMaterial({
                map: texture,
                color: 0xffffff,
                transparent: true,
                opacity: nebula.opacity,
                blending: THREE.NormalBlending,
                depthWrite: false,
                depthTest: false,
            });
            const sprite = new THREE.Sprite(material);
            sprite.position.set(...nebula.position);
            sprite.scale.set(...nebula.scale);
            sprite.material.rotation = nebula.rotation;
            sprite.userData.baseOpacity = nebula.opacity;
            this.cosmosGroup.add(sprite);
            this.nebulaSprites.push(sprite);
        }
    }

    _animateCosmos(dt) {
        if (!this.cosmosGroup) return;
        this.cosmosGroup.rotation.y += dt * 0.004;
        this.cosmosGroup.rotation.x = Math.sin(this.time * 0.025) * 0.025;

        for (let i = 0; i < this.nebulaSprites.length; i++) {
            const nebula = this.nebulaSprites[i];
            const baseOpacity = nebula.userData.baseOpacity || 0.12;
            nebula.material.opacity = baseOpacity * (0.82 + Math.sin(this.time * 0.16 + i * 1.7) * 0.18);
            nebula.material.rotation += dt * (0.002 + i * 0.0008);
        }

        for (let i = 0; i < this.cosmosLayers.length; i++) {
            const layer = this.cosmosLayers[i];
            layer.rotation.y += dt * (0.0015 + i * 0.0008);
            layer.material.opacity = layer.material.opacity * 0.985 + (0.22 + i * 0.06) * 0.015;
        }
    }

    // ===== DATA PATHS =====
    // Thin carrier paths represent brain-region links; moving particles below carry
    // the "data transport" energy so the lines do not become thick blue tubes.

    _buildAxonLines() {
        // Remove old meshes
        for (const d of this._axonData) {
            this.scene.remove(d.coreTube); this.scene.remove(d.glowTube);
            d.coreTube.geometry.dispose(); d.glowTube.geometry.dispose();
        }
        this._connectionLines = [];
        this._axonData = [];

        for (const [a, b] of CONNECTIONS) {
            const la = REGION_LAYOUT[a], lb = REGION_LAYOUT[b];
            if (!la || !lb) continue;

            const va = new THREE.Vector3(la.x, la.y, la.z);
            const vb = new THREE.Vector3(lb.x, lb.y, lb.z);
            const bendOffset = new THREE.Vector3(
                (Math.random() - 0.5) * 0.5,
                0.15 + (Math.random() - 0.5) * 0.35,
                (Math.random() - 0.5) * 0.5
            );
            const mid = va.clone().add(vb).multiplyScalar(0.5).add(bendOffset);
            const curve = new THREE.QuadraticBezierCurve3(va.clone(), mid.clone(), vb.clone());

            // Core: thin electrical wire, bright enough to show connection but not dominate.
            const coreGeo = _allocTube(_T_SEGS, _C_RS);
            _fillTube(coreGeo, curve, 0.0045, _T_SEGS, _C_RS);
            const coreColor = new THREE.Color(0x63f2ff).lerp(new THREE.Color(REGION_COLORS[a] || 0x44aaff), 0.10);
            const coreMat = new THREE.MeshPhongMaterial({
                color: coreColor, emissive: coreColor, emissiveIntensity: 0.18,
                shininess: 90, specular: new THREE.Color(0x9df7ff),
                transparent: true, opacity: 0.34,
                blending: THREE.AdditiveBlending, depthWrite: false, side: THREE.DoubleSide
            });
            const coreTube = new THREE.Mesh(coreGeo, coreMat);
            coreTube.userData.regions = [a, b];
            coreTube.visible = false;
            this.scene.add(coreTube);

            // Glow: tight soft halo around the carrier path.
            const glowGeo = _allocTube(_T_SEGS, _G_RS);
            _fillTube(glowGeo, curve, 0.014, _T_SEGS, _G_RS);
            const glowTube = new THREE.Mesh(glowGeo, new THREE.MeshBasicMaterial({
                color: 0x22d7ff, transparent: true, opacity: 0.035,
                blending: THREE.AdditiveBlending, depthWrite: false, side: THREE.DoubleSide
            }));
            glowTube.visible = false;
            this.scene.add(glowTube);

            const data = {
                regions: [a, b], curve,
                va: va.clone(), vb: vb.clone(), bendOffset,
                coreTube, glowTube,
                wf:    0.28 + Math.random() * 0.44,
                wa:    0.045 + Math.random() * 0.055,
                phase: Math.random() * Math.PI * 2
            };
            this._axonData.push(data);
            this._connectionLines.push(coreTube);   // kept for legacy opacity refs
        }
    }

    // ===== SIGNAL PARTICLES =====

    _initSignalSystem() {
        const geo = new THREE.BufferGeometry();
        const positions = new Float32Array(this.maxSignals * 3);
        const colors = new Float32Array(this.maxSignals * 3);
        geo.setAttribute('position', new THREE.BufferAttribute(positions, 3));
        geo.setAttribute('color', new THREE.BufferAttribute(colors, 3));

        const mat = new THREE.PointsMaterial({
            size: 0.065,
            map: this.axonTexture,
            transparent: true,
            opacity: 0.92,
            blending: THREE.AdditiveBlending,
            depthWrite: false,
            sizeAttenuation: true,
            vertexColors: true,
        });
        // Second, larger glow layer for signals
        const glowGeo = new THREE.BufferGeometry();
        const glowPos = new Float32Array(this.maxSignals * 3);
        glowGeo.setAttribute('position', new THREE.BufferAttribute(glowPos, 3));
        const glowMat = new THREE.PointsMaterial({
            size: 0.18,
            map: this.axonTexture,
            transparent: true,
            opacity: 0.18,
            blending: THREE.AdditiveBlending,
            depthWrite: false,
            sizeAttenuation: true,
            vertexColors: true,
        });
        glowGeo.setAttribute('color', new THREE.BufferAttribute(new Float32Array(this.maxSignals * 3), 3));
        this.signalGlowMesh = new THREE.Points(glowGeo, glowMat);
        this.scene.add(this.signalGlowMesh);

        this.signalMesh = new THREE.Points(geo, mat);
        this.scene.add(this.signalMesh);

        for (let i = 0; i < this.maxSignals; i++) {
            this.signals.push({
                active: false,
                from: new THREE.Vector3(),
                mid: new THREE.Vector3(),
                to: new THREE.Vector3(),
                pos: new THREE.Vector3(),
                progress: 0,
                speed: 0,
                color: new THREE.Color(),
                curve: null,
                reverse: false,
            });
        }
    }

    _spawnSignal(fromRegion, toRegion, color) {
        const from = REGION_LAYOUT[fromRegion];
        const to = REGION_LAYOUT[toRegion];
        if (!from || !to) return;
        const link = this._axonData.find(d =>
            (d.regions[0] === fromRegion && d.regions[1] === toRegion) ||
            (d.regions[0] === toRegion && d.regions[1] === fromRegion)
        );
        // Reuse inactive signal objects instead of allocating new particles every frame.
        for (const sig of this.signals) {
            if (!sig.active) {
                sig.active = true;
                sig.from.set(from.x, from.y, from.z);
                sig.to.set(to.x, to.y, to.z);
                sig.mid.set(
                    (from.x + to.x) / 2 + (Math.random() - 0.5) * 0.4,
                    (from.y + to.y) / 2 + (Math.random() - 0.5) * 0.3,
                    (from.z + to.z) / 2 + (Math.random() - 0.5) * 0.4,
                );
                sig.pos.copy(sig.from);
                sig.progress = 0;
                sig.speed = 0.45 + Math.random() * 0.85;
                sig.curve = link?.curve || null;
                sig.reverse = Boolean(link && link.regions[0] !== fromRegion);
                sig.color.set(0x8ff7ff).lerp(new THREE.Color(color), 0.22);
                return;
            }
        }
    }

    _updateSignals(dt) {
        const posArr = this.signalMesh.geometry.attributes.position.array;
        const colArr = this.signalMesh.geometry.attributes.color.array;

        for (let i = 0; i < this.maxSignals; i++) {
            const sig = this.signals[i];
            if (!sig.active) {
                posArr[i * 3 + 1] = -100;
                sig.curve = null;
                continue;
            }
            sig.progress += dt * sig.speed;
            if (sig.progress >= 1) {
                sig.active = false;
                posArr[i * 3 + 1] = -100;
                sig.curve = null;
                continue;
            }
            const t = sig.progress, t1 = 1 - t;
            if (sig.curve) {
                // Follow the live curved axon so pulses visibly move between regions.
                sig.curve.getPoint(sig.reverse ? 1 - t : t, sig.pos);
                posArr[i * 3] = sig.pos.x;
                posArr[i * 3 + 1] = sig.pos.y;
                posArr[i * 3 + 2] = sig.pos.z;
            } else {
                posArr[i * 3] = t1 * t1 * sig.from.x + 2 * t1 * t * sig.mid.x + t * t * sig.to.x;
                posArr[i * 3 + 1] = t1 * t1 * sig.from.y + 2 * t1 * t * sig.mid.y + t * t * sig.to.y;
                posArr[i * 3 + 2] = t1 * t1 * sig.from.z + 2 * t1 * t * sig.mid.z + t * t * sig.to.z;
            }
            const pulse = 0.45 + Math.sin(t * Math.PI) * 0.75;
            colArr[i * 3] = Math.min(sig.color.r * pulse, 1);
            colArr[i * 3 + 1] = Math.min(sig.color.g * pulse, 1);
            colArr[i * 3 + 2] = Math.min(sig.color.b * pulse, 1);
        }
        this.signalMesh.geometry.attributes.position.needsUpdate = true;
        this.signalMesh.geometry.attributes.color.needsUpdate = true;
        // Mirror the bright signal mesh into a larger glow mesh for readable pulses.
        if (this.signalGlowMesh) {
            const gPosArr = this.signalGlowMesh.geometry.attributes.position.array;
            const gColArr = this.signalGlowMesh.geometry.attributes.color.array;
            for (let i = 0; i < this.maxSignals * 3; i++) {
                gPosArr[i] = posArr[i];
                gColArr[i] = colArr[i];
            }
            this.signalGlowMesh.geometry.attributes.position.needsUpdate = true;
            this.signalGlowMesh.geometry.attributes.color.needsUpdate = true;
        }
    }

    // ===== SHARED KNOWLEDGE CONSTELLATION =====

    _initKnowledgeEvents() {
        window.addEventListener('knowledgeMapUpdated', (event) => {
            this.setKnowledgeMap(event.detail || {});
        });
        window.addEventListener('knowledgeNodeFocus', (event) => {
            this.focusKnowledgeNode(event.detail?.id);
        });
        window.addEventListener('jarvisKnowledgeEvent', (event) => {
            this.spawnKnowledgePacket(event.detail || {});
        });
        window.addEventListener('knowledgeActivityUpdated', (event) => {
            this.knowledgeActivity = event.detail || this.knowledgeActivity || {};
        });
    }

    _knowledgeColor(node) {
        if (node?.status === 'down') return 0xff5577;
        if (node?.status === 'warn') return 0xffaa33;
        return KNOWLEDGE_KIND_COLORS[node?.kind] || 0x9df7ff;
    }

    _graphNodeColor(node) {
        if (node?.color) return new THREE.Color(node.color);
        return new THREE.Color(this._knowledgeColor(node));
    }

    _createKnowledgeGraphPointMaterial() {
        // Shader points let thousands of real graph nodes breathe together
        // without creating thousands of individual meshes.
        return new THREE.ShaderMaterial({
            uniforms: {
                uTime: { value: 0 },
                uActivity: { value: 0.2 },
                uTexture: { value: this.axonTexture },
            },
            vertexShader: `
                attribute float size;
                attribute float phase;
                varying vec3 vColor;
                varying float vPulse;
                uniform float uTime;
                uniform float uActivity;
                void main() {
                    vColor = color;
                    vPulse = 1.0 + sin(uTime * 2.35 + phase) * 0.22 + uActivity * 0.28;
                    vec4 mvPosition = modelViewMatrix * vec4(position, 1.0);
                    gl_PointSize = size * vPulse * (260.0 / max(0.35, -mvPosition.z));
                    gl_Position = projectionMatrix * mvPosition;
                }
            `,
            fragmentShader: `
                uniform sampler2D uTexture;
                varying vec3 vColor;
                varying float vPulse;
                void main() {
                    vec4 tex = texture2D(uTexture, gl_PointCoord);
                    float alpha = tex.a * (0.46 + clamp(vPulse - 0.9, 0.0, 0.7) * 0.18);
                    gl_FragColor = vec4(vColor * (0.82 + vPulse * 0.12), alpha);
                }
            `,
            transparent: true,
            depthWrite: false,
            blending: THREE.NormalBlending,
            vertexColors: true,
        });
    }

    _layoutVector(key, fallback = 'association') {
        const normalized = String(key || fallback).toLowerCase();
        const layout = REGION_LAYOUT[normalized] || KNOWLEDGE_ANCHORS[normalized] || REGION_LAYOUT[fallback] || REGION_LAYOUT.association;
        return new THREE.Vector3(layout.x, layout.y, layout.z);
    }

    _endpointPosition(value) {
        const key = String(value || '').toLowerCase();
        if (key.startsWith('anchor:')) {
            return this._layoutVector(key.slice('anchor:'.length));
        }
        const nodeRecord = this.knowledgeNodes.get(String(value || ''));
        if (nodeRecord) return nodeRecord.mesh.position.clone();
        const graphRecord = this.knowledgeGraphNodes.get(String(value || ''));
        if (graphRecord) return graphRecord.position.clone();
        if (REGION_LAYOUT[key]) return this._layoutVector(key);
        if (KNOWLEDGE_ANCHORS[key]) return this._layoutVector(key);
        if (key.includes('obsidian')) return this._layoutVector('obsidian');
        if (key.includes('graph')) return this._layoutVector('graphify');
        if (key.includes('chat')) return this._layoutVector('chat');
        if (key.includes('bridge') || key.includes('memory')) return this._layoutVector('memory_bridge');
        return this._layoutVector('association');
    }

    _nodePositionForKnowledge(node, index) {
        const fallbackRegion = node?.kind === 'jarvis_memory'
            ? 'hippocampus'
            : node?.kind === 'obsidian_note'
                ? 'prefrontal'
                : 'association';
        const anchor = this._layoutVector(node?.region || fallbackRegion, fallbackRegion);
        const id = node?.id || `${node?.label || 'node'}:${index}`;
        const angle = seededUnit(id, 'angle') * Math.PI * 2;
        const radius = 0.52 + seededUnit(id, 'radius') * 0.95;
        const lift = (seededUnit(id, 'lift') - 0.5) * 0.85;
        const depth = (seededUnit(id, 'depth') - 0.5) * 0.75;
        return new THREE.Vector3(
            anchor.x + Math.cos(angle) * radius,
            anchor.y + lift,
            anchor.z + Math.sin(angle) * radius + depth
        );
    }

    _clearKnowledgeGraph() {
        if (this.knowledgeGraphPointMesh) {
            this.knowledgeGraphGroup.remove(this.knowledgeGraphPointMesh);
            this.knowledgeGraphPointMesh.geometry.dispose();
            this.knowledgeGraphPointMesh.material.dispose();
            this.knowledgeGraphPointMesh = null;
        }
        if (this.knowledgeGraphLineMesh) {
            this.knowledgeGraphGroup.remove(this.knowledgeGraphLineMesh);
            this.knowledgeGraphLineMesh.geometry.dispose();
            this.knowledgeGraphLineMesh.material.dispose();
            this.knowledgeGraphLineMesh = null;
        }
        if (this.knowledgeGraphFocusLineMesh) {
            this.knowledgeGraphGroup.remove(this.knowledgeGraphFocusLineMesh);
            this.knowledgeGraphFocusLineMesh.geometry.dispose();
            this.knowledgeGraphFocusLineMesh.material.dispose();
            this.knowledgeGraphFocusLineMesh = null;
        }
        if (this.knowledgeGraphFocusSprite) {
            this.knowledgeGraphFocusSprite.material.opacity = 0;
        }
        this.knowledgeGraphNodes.clear();
        this.knowledgeGraphNodeArray = [];
        this.knowledgeGraphEdges = [];
    }

    setKnowledgeGraph(graph = {}) {
        this._clearKnowledgeGraph();
        const nodes = Array.isArray(graph.nodes) ? graph.nodes : [];
        const edges = Array.isArray(graph.edges) ? graph.edges : [];
        if (!nodes.length) return;

        const positions = new Float32Array(nodes.length * 3);
        const colors = new Float32Array(nodes.length * 3);
        const sizes = new Float32Array(nodes.length);
        const phases = new Float32Array(nodes.length);
        for (let index = 0; index < nodes.length; index++) {
            const node = nodes[index];
            const p = node.position || {};
            const position = new THREE.Vector3(
                Number(p.x || 0),
                Number(p.y || 0),
                Number(p.z || 0)
            );
            const color = this._graphNodeColor(node);
            positions[index * 3] = position.x;
            positions[index * 3 + 1] = position.y;
            positions[index * 3 + 2] = position.z;
            colors[index * 3] = color.r;
            colors[index * 3 + 1] = color.g;
            colors[index * 3 + 2] = color.b;
            // These sizes are shader units, not pixels. Keeping them below one
            // prevents thousands of real Graphify nodes from stacking into a
            // white WebView2 blob while still leaving every node clickable.
            sizes[index] = (node.highlighted ? GRAPH_POINT_HIGHLIGHT_SIZE : GRAPH_POINT_BASE_SIZE) +
                Math.min(Number(node.degree || 0), 120) * GRAPH_POINT_DEGREE_SCALE;
            phases[index] = seededUnit(node.id, 'pulse') * Math.PI * 2;
            this.knowledgeGraphNodes.set(String(node.id), { node, position, index });
            this.knowledgeGraphNodeArray[index] = node;
        }

        const pointGeometry = new THREE.BufferGeometry();
        pointGeometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
        pointGeometry.setAttribute('color', new THREE.BufferAttribute(colors, 3));
        pointGeometry.setAttribute('size', new THREE.BufferAttribute(sizes, 1));
        pointGeometry.setAttribute('phase', new THREE.BufferAttribute(phases, 1));
        const pointMaterial = this._createKnowledgeGraphPointMaterial();
        this.knowledgeGraphPointMesh = new THREE.Points(pointGeometry, pointMaterial);
        this.knowledgeGraphPointMesh.name = 'graphify-real-nodes';
        this.knowledgeGraphGroup.add(this.knowledgeGraphPointMesh);

        const linePositions = [];
        const lineColors = [];
        this.knowledgeGraphEdges = edges;
        for (const edge of this._selectVisibleKnowledgeEdges(edges)) {
            const source = this.knowledgeGraphNodes.get(String(edge.source));
            const target = this.knowledgeGraphNodes.get(String(edge.target));
            if (!source || !target) continue;
            const sourceColor = this._graphNodeColor(source.node);
            const targetColor = this._graphNodeColor(target.node);
            linePositions.push(
                source.position.x, source.position.y, source.position.z,
                target.position.x, target.position.y, target.position.z
            );
            lineColors.push(
                sourceColor.r, sourceColor.g, sourceColor.b,
                targetColor.r, targetColor.g, targetColor.b
            );
        }
        if (linePositions.length) {
            const lineGeometry = new THREE.BufferGeometry();
            lineGeometry.setAttribute('position', new THREE.Float32BufferAttribute(linePositions, 3));
            lineGeometry.setAttribute('color', new THREE.Float32BufferAttribute(lineColors, 3));
            const lineMaterial = new THREE.LineBasicMaterial({
                vertexColors: true,
                transparent: true,
                opacity: GRAPH_IDLE_EDGE_OPACITY,
                depthWrite: false,
                blending: THREE.NormalBlending,
            });
            this.knowledgeGraphLineMesh = new THREE.LineSegments(lineGeometry, lineMaterial);
            this.knowledgeGraphLineMesh.name = 'graphify-real-links';
            this.knowledgeGraphGroup.add(this.knowledgeGraphLineMesh);
        }
    }

    _selectVisibleKnowledgeEdges(edges, maxEdges = GRAPH_IDLE_EDGE_LIMIT) {
        const scored = [];
        for (const edge of edges) {
            const source = this.knowledgeGraphNodes.get(String(edge.source));
            const target = this.knowledgeGraphNodes.get(String(edge.target));
            if (!source || !target) continue;
            const distance = source.position.distanceTo(target.position);
            const sourceDegree = Number(source.node.degree || 1);
            const targetDegree = Number(target.node.degree || 1);
            const score =
                (Math.sqrt(sourceDegree) + Math.sqrt(targetDegree)) / (1 + distance * 1.35) +
                seededUnit(edge.id || `${edge.source}:${edge.target}`, 'edge') * 0.18;
            scored.push({ edge, score });
        }

        // The raw graph has thousands of valid links. Drawing only the strongest
        // carriers keeps the spiderweb readable while focus mode still exposes
        // exact neighbors for a clicked node.
        scored.sort((a, b) => b.score - a.score);
        return scored.slice(0, maxEdges).map((item) => item.edge);
    }

    _disposeThoughtLaneMesh(mesh) {
        if (!mesh) return;
        this.knowledgeGraphGroup.remove(mesh);
        mesh.geometry.dispose();
        mesh.material.dispose();
    }

    _clearThoughtLanes() {
        this._disposeThoughtLaneMesh(this.knowledgeThoughtLineMesh);
        this._disposeThoughtLaneMesh(this.obsidianThoughtLineMesh);
        this.knowledgeThoughtLineMesh = null;
        this.obsidianThoughtLineMesh = null;
        this.obsidianThoughtRoutes = [];
        this.obsidianPulseTimer = 0;
        this.obsidianPulseCursor = 0;
    }

    _makeThoughtLaneMesh(lanes, opacity, name) {
        const positions = [];
        const colors = [];
        for (const lane of lanes) {
            const source = this._endpointPosition(lane.source);
            const target = this._endpointPosition(lane.target);
            if (!source || !target || source.distanceToSquared(target) < 0.001) continue;
            const color = new THREE.Color(lane.color || 0xaa88ff);
            positions.push(source.x, source.y, source.z, target.x, target.y, target.z);
            colors.push(color.r, color.g, color.b, color.r, color.g, color.b);
        }
        if (!positions.length) return null;
        const geometry = new THREE.BufferGeometry();
        geometry.setAttribute('position', new THREE.Float32BufferAttribute(positions, 3));
        geometry.setAttribute('color', new THREE.Float32BufferAttribute(colors, 3));
        const material = new THREE.LineBasicMaterial({
            vertexColors: true,
            transparent: true,
            opacity,
            depthWrite: false,
            blending: THREE.NormalBlending,
        });
        const mesh = new THREE.LineSegments(geometry, material);
        mesh.name = name;
        this.knowledgeGraphGroup.add(mesh);
        return mesh;
    }

    _buildThoughtLanes(edges = [], nodes = []) {
        this._clearThoughtLanes();
        const generalLanes = [];
        const obsidianLanes = [];
        const obsidianRecords = [];

        for (const edge of edges) {
            const targetNode = this.knowledgeNodes.get(String(edge.target || ''));
            const sourceNode = this.knowledgeNodes.get(String(edge.source || ''));
            const isObsidian = targetNode?.node?.kind === 'obsidian_note' || sourceNode?.node?.kind === 'obsidian_note' ||
                String(edge.source || '').includes('obsidian') || String(edge.target || '').includes('obsidian');
            const lane = {
                source: edge.source,
                target: edge.target,
                color: isObsidian ? 0xaa88ff : this._knowledgeColor(targetNode?.node || sourceNode?.node || {}),
            };
            if (isObsidian) obsidianLanes.push(lane);
            else generalLanes.push(lane);
        }

        for (const node of nodes) {
            if (node?.kind !== 'obsidian_note') continue;
            const record = this.knowledgeNodes.get(String(node.id));
            if (!record) continue;
            obsidianRecords.push(record);
            // Obsidian notes become thought-tree leaves: they breathe between
            // the vault anchor, the owning brain region, and their note peers.
            obsidianLanes.push({ source: 'anchor:obsidian', target: node.id, color: 0xaa88ff });
            obsidianLanes.push({ source: node.id, target: `anchor:${node.region || 'prefrontal'}`, color: 0x8ff7ff });
        }

        const sortedObsidian = obsidianRecords.sort((a, b) => String(a.node.path || a.node.label).localeCompare(String(b.node.path || b.node.label)));
        for (let i = 1; i < sortedObsidian.length; i++) {
            const previous = sortedObsidian[i - 1].node;
            const current = sortedObsidian[i].node;
            obsidianLanes.push({ source: previous.id, target: current.id, color: 0xd8b9ff });
        }

        // Keep the lanes finite and readable. The panel can list many more nodes,
        // but the 3D scene should feel like connected thought paths, not cable salad.
        this.knowledgeThoughtLineMesh = this._makeThoughtLaneMesh(generalLanes.slice(0, 150), THOUGHT_LANE_OPACITY, 'knowledge-thought-lanes');
        this.obsidianThoughtLineMesh = this._makeThoughtLaneMesh(obsidianLanes.slice(0, 110), OBSIDIAN_LANE_OPACITY, 'obsidian-thought-tree-lanes');
        this.obsidianThoughtRoutes = obsidianLanes.slice(0, 110);
    }

    _buildKnowledgeRegionStats(nodes = []) {
        const stats = {};
        for (const node of nodes) {
            const region = node?.region || 'association';
            if (!stats[region]) stats[region] = { total: 0, obsidian: 0, memory: 0, graphify: 0 };
            stats[region].total += 1;
            if (node.kind === 'obsidian_note') stats[region].obsidian += 1;
            if (node.kind === 'jarvis_memory') stats[region].memory += 1;
            if (node.kind === 'graphify_code') stats[region].graphify += 1;
        }
        this.knowledgeRegionStats = stats;
        this._updateRegionKnowledgeLabels();
    }

    _updateRegionKnowledgeLabels() {
        for (const [regionName, label] of Object.entries(this.labels)) {
            const stats = this.knowledgeRegionStats?.[regionName] || { total: 0, obsidian: 0, memory: 0, graphify: 0 };
            const lane = label.querySelector('.rl-thoughts');
            if (!lane) continue;
            lane.textContent = `OBS ${stats.obsidian} tankar • ${stats.total} noder`;
            lane.style.opacity = stats.total > 0 ? '1' : '0.42';
        }
    }

    _updateObsidianThoughtPulses(dt) {
        if (!this.obsidianThoughtRoutes.length) return;
        const obsidianLoad = Number(this.knowledgeActivity?.obsidian_load || 0);
        this.obsidianPulseTimer += dt;
        const cadence = Math.max(0.26, 0.72 - obsidianLoad * 0.34);
        if (this.obsidianPulseTimer < cadence) return;

        // Rotating routes make the Obsidian tree look alive without flooding the
        // scene; each pulse is still anchored to a real note or mapped brain area.
        this.obsidianPulseTimer = 0;
        const route = this.obsidianThoughtRoutes[this.obsidianPulseCursor % this.obsidianThoughtRoutes.length];
        this.obsidianPulseCursor += 1;
        this.spawnKnowledgePacket({
            type: 'obsidian_read',
            source: route.source,
            target: route.target,
            label: 'Obsidian tanke-bana',
            status: 'ok',
        });
    }

    _focusKnowledgeGraphNode(id) {
        const record = this.knowledgeGraphNodes.get(String(id || ''));
        if (!record) return false;

        if (!this.knowledgeGraphFocusSprite) {
            this.knowledgeGraphFocusSprite = new THREE.Sprite(new THREE.SpriteMaterial({
                map: this.axonTexture,
                color: 0xffffff,
                transparent: true,
                opacity: 0.0,
                blending: THREE.AdditiveBlending,
                depthWrite: false,
            }));
            this.knowledgeGraphFocusSprite.name = 'knowledge-graph-focus';
            this.knowledgeGraphGroup.add(this.knowledgeGraphFocusSprite);
        }

        const color = this._graphNodeColor(record.node);
        this.knowledgeGraphFocusSprite.material.color.copy(color);
        this.knowledgeGraphFocusSprite.material.opacity = 0.85;
        this.knowledgeGraphFocusSprite.position.copy(record.position);
        this.knowledgeGraphFocusSprite.scale.setScalar(0.46);
        this._setKnowledgeGraphFocusLinks(record);

        this._cameraTarget = record.position.clone();
        this._cameraGoto = record.position.clone().add(new THREE.Vector3(0.0, 0.22, 3.4));
        this._cameraAnim = 0;
        this._cameraStart = this.camera.position.clone();
        this._controlsTargetStart = this.controls.target.clone();

        this.spawnKnowledgePacket({
            type: 'node_focus',
            source: 'chat',
            target: record.node.id,
            label: record.node.label,
            status: 'ok',
        });
        return true;
    }

    _setKnowledgeGraphFocusLinks(record) {
        if (this.knowledgeGraphFocusLineMesh) {
            this.knowledgeGraphGroup.remove(this.knowledgeGraphFocusLineMesh);
            this.knowledgeGraphFocusLineMesh.geometry.dispose();
            this.knowledgeGraphFocusLineMesh.material.dispose();
            this.knowledgeGraphFocusLineMesh = null;
        }

        const nodeId = String(record.node.id);
        const linePositions = [];
        const lineColors = [];
        const focusColor = this._graphNodeColor(record.node);
        let count = 0;
        for (const edge of this.knowledgeGraphEdges) {
            const sourceId = String(edge.source);
            const targetId = String(edge.target);
            if (sourceId !== nodeId && targetId !== nodeId) continue;
            const otherId = sourceId === nodeId ? targetId : sourceId;
            const other = this.knowledgeGraphNodes.get(otherId);
            if (!other) continue;
            const otherColor = this._graphNodeColor(other.node);
            linePositions.push(
                record.position.x, record.position.y, record.position.z,
                other.position.x, other.position.y, other.position.z
            );
            lineColors.push(
                focusColor.r, focusColor.g, focusColor.b,
                otherColor.r, otherColor.g, otherColor.b
            );
            count += 1;
            if (count >= 140) break;
        }

        if (!linePositions.length) return;
        const geometry = new THREE.BufferGeometry();
        geometry.setAttribute('position', new THREE.Float32BufferAttribute(linePositions, 3));
        geometry.setAttribute('color', new THREE.Float32BufferAttribute(lineColors, 3));
        const material = new THREE.LineBasicMaterial({
            vertexColors: true,
            transparent: true,
            opacity: GRAPH_FOCUS_EDGE_OPACITY,
            depthWrite: false,
            blending: THREE.NormalBlending,
        });
        this.knowledgeGraphFocusLineMesh = new THREE.LineSegments(geometry, material);
        this.knowledgeGraphFocusLineMesh.name = 'knowledge-focus-neighborhood';
        this.knowledgeGraphGroup.add(this.knowledgeGraphFocusLineMesh);
    }

    setKnowledgeMap(data) {
        this.setKnowledgeGraph(data?.graph || {});
        this._clearThoughtLanes();

        for (const record of this.knowledgeNodes.values()) {
            this.knowledgeNodeGroup.remove(record.mesh);
            this.knowledgeNodeGroup.remove(record.glow);
            if (record.hit) this.knowledgeNodeGroup.remove(record.hit);
            record.mesh.geometry.dispose();
            record.mesh.material.dispose();
            record.glow.material.dispose();
            record.hit?.geometry?.dispose();
            record.hit?.material?.dispose();
        }

        this.knowledgeNodes.clear();
        this.knowledgeNodeMeshes = [];
        this.knowledgeNodeHitMeshes = [];

        const nodes = Array.isArray(data?.nodes) ? data.nodes.slice(0, 90) : [];
        for (let index = 0; index < nodes.length; index++) {
            const node = nodes[index];
            const color = this._knowledgeColor(node);
            const confidence = Math.max(0.15, Math.min(1, Number(node.confidence || 0.65)));
            const isObsidian = node.kind === 'obsidian_note';
            const isMemory = node.kind === 'jarvis_memory' || node.kind === 'neurolinked_semantic';
            const radius = (0.030 + confidence * 0.032) * (isObsidian ? 1.22 : isMemory ? 1.12 : 1);

            const geometry = new THREE.SphereGeometry(radius, 16, 10);
            const material = new THREE.MeshBasicMaterial({
                color,
                transparent: true,
                opacity: isObsidian ? 0.90 : 0.84,
                depthWrite: false,
                blending: THREE.AdditiveBlending,
            });
            const mesh = new THREE.Mesh(geometry, material);
            mesh.position.copy(this._nodePositionForKnowledge(node, index));
            mesh.userData.knowledgeNode = node;

            // The visible dot stays crisp, while this invisible proxy gives the
            // user a forgiving click target exactly centered on the same point.
            const hitRadius = Math.max(radius * 2.45, 0.13);
            const hit = new THREE.Mesh(
                new THREE.SphereGeometry(hitRadius, 18, 12),
                new THREE.MeshBasicMaterial({
                    transparent: true,
                    opacity: 0,
                    colorWrite: false,
                    depthWrite: false,
                })
            );
            hit.position.copy(mesh.position);
            hit.userData.knowledgeNode = node;
            hit.userData.hitProxy = true;

            const glow = new THREE.Sprite(new THREE.SpriteMaterial({
                map: this.axonTexture,
                color,
                transparent: true,
                opacity: 0.18,
                blending: THREE.AdditiveBlending,
                depthWrite: false,
            }));
            glow.position.copy(mesh.position);
            glow.scale.setScalar((0.16 + confidence * 0.17) * (isObsidian ? 1.18 : 1));

            const record = {
                node,
                mesh,
                glow,
                hit,
                base: mesh.position.clone(),
                phase: seededUnit(node.id, 'phase') * Math.PI * 2,
                focusUntil: 0,
            };
            this.knowledgeNodes.set(node.id, record);
            this.knowledgeNodeMeshes.push(mesh);
            this.knowledgeNodeHitMeshes.push(hit);
            this.knowledgeNodeGroup.add(mesh, glow, hit);
        }

        this._buildThoughtLanes(Array.isArray(data?.edges) ? data.edges : [], nodes);
        this._buildKnowledgeRegionStats(nodes);

        if (nodes.length > 0) {
            this.spawnKnowledgePacket({
                type: 'knowledge_map',
                source: 'graphify',
                target: nodes[0].id,
                label: 'Kunskapskarta uppdaterad',
                status: 'ok',
            });
        }
    }

    focusKnowledgeNode(id) {
        if (this._focusKnowledgeGraphNode(id)) return;

        const record = this.knowledgeNodes.get(String(id || ''));
        if (!record) return;

        record.focusUntil = performance.now() + 2200;
        const target = record.mesh.position.clone();
        this._cameraTarget = target.clone();
        this._cameraGoto = target.clone().add(new THREE.Vector3(0.0, 0.12, 2.8));
        this._cameraAnim = 0;
        this._cameraStart = this.camera.position.clone();
        this._controlsTargetStart = this.controls.target.clone();

        this.spawnKnowledgePacket({
            type: 'node_focus',
            source: 'chat',
            target: record.node.id,
            label: record.node.label,
            status: 'ok',
        });
    }

    spawnKnowledgePacket(detail = {}) {
        if (!this.knowledgePacketGroup || !this.axonTexture) return;
        const from = this._endpointPosition(detail.source || 'jarvis_bridge');
        const targetKey = detail.target || detail.node_id || detail.id || 'association';
        const to = this._endpointPosition(targetKey);
        if (from.distanceToSquared(to) < 0.01) {
            to.add(new THREE.Vector3(0.55, 0.18, 0.25));
        }

        const colorHex = detail.status === 'down'
            ? 0xff5577
            : detail.status === 'warn'
                ? 0xffaa33
                : KNOWLEDGE_EVENT_COLORS[detail.type] || 0x8ff7ff;
        const routeText = `${detail.type || ''}:${detail.source || ''}:${targetKey || ''}`.toLowerCase();
        const isObsidianPulse = routeText.includes('obsidian');
        const material = new THREE.SpriteMaterial({
            map: this.axonTexture,
            color: colorHex,
            transparent: true,
            opacity: isObsidianPulse ? 0.98 : 0.92,
            blending: THREE.AdditiveBlending,
            depthWrite: false,
        });
        const sprite = new THREE.Sprite(material);
        sprite.scale.setScalar(isObsidianPulse ? 0.15 : 0.11);
        sprite.position.copy(from);
        this.knowledgePacketGroup.add(sprite);

        const mid = from.clone().add(to).multiplyScalar(0.5);
        mid.y += 0.35 + seededUnit(detail.label || detail.type || Date.now(), 'arc') * 0.65;
        mid.z += (seededUnit(detail.label || detail.type || Date.now(), 'z') - 0.5) * 0.65;

        this.knowledgePackets.push({
            sprite,
            from,
            mid,
            to,
            progress: 0,
            baseScale: isObsidianPulse ? 0.11 : 0.07,
            pulseScale: isObsidianPulse ? 0.17 : 0.10,
            speed: isObsidianPulse ? 0.52 + Math.random() * 0.30 : 0.72 + Math.random() * 0.45,
        });

        while (this.knowledgePackets.length > 90) {
            this._disposeKnowledgePacket(this.knowledgePackets.shift());
        }
    }

    _disposeKnowledgePacket(packet) {
        if (!packet) return;
        this.knowledgePacketGroup.remove(packet.sprite);
        packet.sprite.material.dispose();
    }

    _animateKnowledgeNodes() {
        const now = performance.now();
        if (this.knowledgeGraphFocusSprite) {
            const pulse = 1 + Math.sin(this.time * 5.2) * 0.16;
            this.knowledgeGraphFocusSprite.scale.setScalar(0.42 * pulse);
            this.knowledgeGraphFocusSprite.material.opacity *= 0.985;
        }
        if (this.knowledgeGraphPointMesh?.material?.uniforms) {
            this.knowledgeGraphPointMesh.material.uniforms.uTime.value = this.time;
            this.knowledgeGraphPointMesh.material.uniforms.uActivity.value = Number(this.knowledgeActivity?.synapse_load || 0.2);
        }
        if (this.knowledgeGraphLineMesh) {
            this.knowledgeGraphLineMesh.material.opacity = GRAPH_IDLE_EDGE_OPACITY +
                Number(this.knowledgeActivity?.synapse_load || 0) * GRAPH_IDLE_EDGE_ACTIVITY_OPACITY;
        }
        if (this.knowledgeGraphFocusLineMesh) {
            this.knowledgeGraphFocusLineMesh.material.opacity = GRAPH_FOCUS_EDGE_OPACITY +
                Math.sin(this.time * 3.8) * 0.035;
        }
        if (this.knowledgeThoughtLineMesh) {
            this.knowledgeThoughtLineMesh.material.opacity = THOUGHT_LANE_OPACITY +
                Number(this.knowledgeActivity?.memory_load || 0) * 0.030;
        }
        if (this.obsidianThoughtLineMesh) {
            this.obsidianThoughtLineMesh.material.opacity = OBSIDIAN_LANE_OPACITY +
                Math.max(0, Math.sin(this.time * 1.8)) * 0.045;
        }
        for (const hotspot of Object.values(this.regionHotspots)) {
            const firing = this.lastState?.region_firing?.[hotspot.regionName] || 0;
            const pulse = 1 + Math.sin(this.time * 2.4 + hotspot.phase) * 0.16 + Math.min(firing * 5, 0.7);
            hotspot.mesh.scale.setScalar(hotspot.baseScale * pulse);
            hotspot.mesh.material.opacity = 0.58 + Math.min(firing * 2.2, 0.34);
        }
        for (const record of this.knowledgeNodes.values()) {
            const focusBoost = record.focusUntil > now ? 1.55 : 1;
            const drift = Math.sin(this.time * 0.95 + record.phase) * 0.025;
            record.mesh.position.set(
                record.base.x,
                record.base.y + drift,
                record.base.z + Math.cos(this.time * 0.72 + record.phase) * 0.018
            );
            record.glow.position.copy(record.mesh.position);
            record.hit?.position?.copy(record.mesh.position);

            const pulse = 1 + Math.sin(this.time * 2.1 + record.phase) * 0.07;
            record.mesh.scale.setScalar(pulse * focusBoost);
            const confidence = Number(record.node.confidence || 0.65);
            const isObsidian = record.node.kind === 'obsidian_note';
            record.glow.scale.setScalar(((0.16 + confidence * 0.17) * (isObsidian ? 1.18 : 1)) * focusBoost);
            record.glow.material.opacity = (record.focusUntil > now ? 0.34 : isObsidian ? 0.16 : 0.11) +
                Math.max(0, Math.sin(this.time * 1.7 + record.phase)) * (isObsidian ? 0.08 : 0.05);
        }
    }

    _updateKnowledgePackets(dt) {
        for (let i = this.knowledgePackets.length - 1; i >= 0; i--) {
            const packet = this.knowledgePackets[i];
            packet.progress += dt * packet.speed;
            if (packet.progress >= 1) {
                this._disposeKnowledgePacket(packet);
                this.knowledgePackets.splice(i, 1);
                continue;
            }

            const t = packet.progress;
            const s = 1 - t;
            packet.sprite.position.set(
                s * s * packet.from.x + 2 * s * t * packet.mid.x + t * t * packet.to.x,
                s * s * packet.from.y + 2 * s * t * packet.mid.y + t * t * packet.to.y,
                s * s * packet.from.z + 2 * s * t * packet.mid.z + t * t * packet.to.z
            );
            packet.sprite.material.opacity = Math.max(0.05, Math.sin(t * Math.PI) * 0.95);
            packet.sprite.scale.setScalar(packet.baseScale + Math.sin(t * Math.PI) * packet.pulseScale);
        }
    }

    // ===== NEURON INIT =====

    initNeurons(positions) {
        for (const m of Object.values(this.regionMeshes)) this.scene.remove(m);
        for (const g of Object.values(this.regionGlows)) this.scene.remove(g);
        for (const hotspot of Object.values(this.regionHotspots)) {
            this.scene.remove(hotspot.mesh);
            hotspot.mesh.geometry.dispose();
            hotspot.mesh.material.dispose();
        }
        this.regionMeshes = {};
        this.regionGlows = {};
        this.regionHotspots = {};
        this.regionData = positions;
        this.labelContainer.innerHTML = '';
        this.labels = {};
        this._basePositions = {};

        for (const [regionName, regionInfo] of Object.entries(positions)) {
            const pts = regionInfo.positions;
            if (!pts || pts.length === 0) continue;

            const layout = REGION_LAYOUT[regionName] || { x: 0, y: 0, z: 0 };
            const center = regionInfo.center || [0, 0, 0];
            const color = REGION_COLORS[regionName] || 0xff6600;

            const geo = new THREE.BufferGeometry();
            const posArray = new Float32Array(pts.length * 3);
            for (let i = 0; i < pts.length; i++) {
                posArray[i * 3] = (pts[i][0] - center[0]) * 1.2 + layout.x;
                posArray[i * 3 + 1] = (pts[i][1] - center[1]) * 1.2 + layout.y;
                posArray[i * 3 + 2] = (pts[i][2] - center[2]) * 1.2 + layout.z;
            }
            geo.setAttribute('position', new THREE.BufferAttribute(posArray, 3));
            this._basePositions[regionName] = new Float32Array(posArray);

            const mat = new THREE.PointsMaterial({
                color,
                size: 0.024,
                map: this.synapseTexture,
                transparent: true,
                opacity: 0.9,
                blending: THREE.AdditiveBlending,
                depthWrite: false,
                sizeAttenuation: true,
            });
            const pts3d = new THREE.Points(geo, mat);
            pts3d.userData = { regionName, center: [layout.x, layout.y, layout.z], count: regionInfo.count };
            this.scene.add(pts3d);
            this.regionMeshes[regionName] = pts3d;

            // Wide soft glow layer
            const glowGeo = geo.clone();
            const glowMat = new THREE.PointsMaterial({
                color,
                size: 0.065,
                map: this.synapseTexture,
                transparent: true,
                opacity: 0.12,
                blending: THREE.AdditiveBlending,
                depthWrite: false,
                sizeAttenuation: true,
            });
            const glowPts = new THREE.Points(glowGeo, glowMat);
            this.scene.add(glowPts);
            this.regionGlows[regionName] = glowPts;
            this._createRegionHotspot(regionName, regionInfo.count || pts.length, color);

            this._createLabel(regionName, regionInfo.count);
        }
        console.log(`[Brain3D] ${Object.keys(this.regionMeshes).length} regions initialized`);
    }

    _createRegionHotspot(regionName, count, color) {
        const layout = REGION_LAYOUT[regionName];
        if (!layout) return;
        const geometry = new THREE.SphereGeometry(0.055, 16, 10);
        const material = new THREE.MeshBasicMaterial({
            color,
            transparent: true,
            opacity: 0.74,
            blending: THREE.AdditiveBlending,
            depthWrite: false,
        });
        const mesh = new THREE.Mesh(geometry, material);
        mesh.position.set(layout.x, layout.y, layout.z);
        mesh.userData = {
            isRegionHotspot: true,
            regionName,
            count,
        };
        this.scene.add(mesh);
        this.regionHotspots[regionName] = {
            mesh,
            regionName,
            phase: seededUnit(regionName, 'region-hotspot') * Math.PI * 2,
            baseScale: 1.0,
        };
    }

    // ===== LABELS =====

    _createLabel(regionName, count) {
        const label = document.createElement('div');
        label.className = 'region-label-3d';
        const hex = '#' + (REGION_COLORS[regionName] || 0xff6600).toString(16).padStart(6, '0');
        const name = regionLabel(regionName).toUpperCase();
        const countStr = count >= 1000 ? (count / 1000).toFixed(1) + 'K' : count;
        label.innerHTML = `
            <div class="rl-name" style="color:${hex}">${name}</div>
            <div class="rl-stats">${countStr} neuroner · <span class="rl-firing">aktivitet 0,0%</span></div>
            <div class="rl-thoughts" style="margin-top:2px;color:#c4a7ff;font-size:8px;letter-spacing:1.1px;">OBS 0 tankar â€¢ 0 noder</div>
        `;
        label.style.cssText = `
            position:absolute; pointer-events:auto; cursor:pointer;
            padding:4px 10px; background:rgba(0,0,0,0.85);
            border:1px solid ${hex}66; border-radius:5px;
            font-family:'JetBrains','Consolas',monospace; white-space:nowrap;
            transition:opacity 0.3s;
        `;
        label.addEventListener('click', () => {
            window.dispatchEvent(new CustomEvent('regionClick', {
                detail: {
                    name: regionName,
                    label: regionLabel(regionName),
                    color: hex.replace('#', ''),
                    description: SVENSKA_REGIONBESKRIVNINGAR[regionName] || `Neural region: ${regionLabel(regionName)}`,
                    count,
                }
            }));
        });
        this.labelContainer.appendChild(label);
        this.labels[regionName] = label;
    }

    // ===== GRAPH VIEW BUTTON =====

    _createGraphViewButton() {
        const btn = document.createElement('div');
        btn.id = 'graph-view-btn';
        btn.innerHTML = '<span style="font-size:10px;letter-spacing:1.5px;color:#ff8844;cursor:pointer;">DÖLJ ETIKETTER</span>';
        btn.title = 'Visa/dölj regionetiketter';
        btn.innerHTML = '<span style="font-size:10px;letter-spacing:1.5px;color:#ff8844;cursor:pointer;">DÖLJ ETIKETTER</span>';
        btn.title = 'Visa/dölj regionetiketter';
        btn.style.cssText = `
            position:fixed; top:15px; left:50%; transform:translateX(-50%);
            padding:6px 14px; z-index:20; cursor:pointer;
            background:rgba(8,12,20,0.75); backdrop-filter:blur(20px);
            border:1px solid rgba(255,100,20,0.2); border-radius:8px;
        `;
        this._labelsVisible = true;
        btn.addEventListener('click', () => {
            this._labelsVisible = !this._labelsVisible;
            btn.querySelector('span').textContent = this._labelsVisible ? 'DÖLJ ETIKETTER' : 'VISA ETIKETTER';
            btn.querySelector('span').textContent = this._labelsVisible ? 'DÖLJ ETIKETTER' : 'VISA ETIKETTER';
            if (this.labelContainer) {
                this.labelContainer.style.display = this._labelsVisible ? '' : 'none';
            }
            this.graphView = !this._labelsVisible;
            for (const d of this._axonData) {
                d.coreTube.visible = false;
                d.glowTube.visible = false;
            }
        });
        document.body.appendChild(btn);
    }

    // ===== STATE UPDATE =====

    updateState(state) {
        if (!state) return;
        this.lastState = state;
        const regionFiring = state.region_firing || {};
        const regions = state.regions || {};

        for (const [name, mesh] of Object.entries(this.regionMeshes)) {
            const firing = regionFiring[name] || 0;
            const regionInfo = regions[name] || {};

            // Pulse brightness with firing rate
            mesh.material.opacity = 0.58 + Math.min(firing * 8, 0.42);
            mesh.material.size = 0.022 + firing * 0.035;

            const glow = this.regionGlows[name];
            if (glow) {
                glow.material.opacity = 0.10 + Math.min(firing * 1.8, 0.18);
                glow.material.size = 0.060 + firing * 0.055;
            }

            // Update label firing rate
            const label = this.labels[name];
            if (label) {
                const span = label.querySelector('.rl-firing');
                if (span) {
                    const rate = regionInfo.firing_rate || firing || 0;
                    span.textContent = `aktivitet ${(rate * 100).toFixed(1).replace('.', ',')}%`;
                }
            }
        }

        // Data paths stay subtle; moving pulses carry the visual energy.
        for (const d of this._axonData) {
            d.coreTube.visible = false;
            d.glowTube.visible = false;
        }

        // Spawn signal particles
        for (const [a, b] of CONNECTIONS) {
            const firingA = regionFiring[a] || 0;
            const firingB = regionFiring[b] || 0;
            const activity = (firingA + firingB) / 2;
            if (activity > 0.005 && Math.random() < activity * 3.2) {
                const colorA = REGION_COLORS[a] || 0xff6600;
                const colorB = REGION_COLORS[b] || 0xff6600;
                this._spawnSignal(a, b, firingA > firingB ? colorA : colorB);
            }
        }
    }

    // ===== AXON ANIMATION =====

    _animateAxons() {
        const t = this.time;
        for (const d of this._axonData) {
            // Mid-point = base midpoint + stored arc offset + 3D sine wave
            _tv.addVectors(d.va, d.vb).multiplyScalar(0.5).add(d.bendOffset);
            d.curve.v1.copy(_tv);
            d.curve.v1.x += Math.sin(t * d.wf          + d.phase)       * d.wa;
            d.curve.v1.y += Math.cos(t * d.wf * 0.79   + d.phase)       * d.wa * 0.65;
            d.curve.v1.z += Math.sin(t * d.wf * 1.21   + d.phase * 1.3) * d.wa;
            if (d.coreTube.visible) {
                _fillTube(d.coreTube.geometry, d.curve, 0.0045, _T_SEGS, _C_RS);
            }
            if (d.glowTube.visible) {
                _fillTube(d.glowTube.geometry, d.curve, 0.014, _T_SEGS, _G_RS);
            }
        }
    }

    // ===== ANIMATION =====

    animate(dt) {
        this.time += dt;
        const regionFiring = this.lastState?.region_firing || {};

        // Pulse core light like a beating heart
        this._coreLight.intensity = 1.0 + Math.sin(this.time * 1.8) * 0.5;
        this._coreLight.color.setHex(
            Math.sin(this.time * 0.7) > 0 ? 0xff6600 : 0xff8833
        );

        for (const [name, mesh] of Object.entries(this.regionMeshes)) {
            const firing = regionFiring[name] || 0;
            const phase = this.time * 1.2 + name.length * 0.7;
            mesh.material.opacity *= 0.85 + Math.sin(phase) * 0.15;

            // Jitter active neurons — "thinking" micro-motion
            if (firing > 0.003 && this._basePositions[name]) {
                const pos = mesh.geometry.attributes.position.array;
                const base = this._basePositions[name];
                const jit = Math.min(firing * 0.2, 0.025);
                const stride = Math.max(1, Math.floor(pos.length / (300 * 3)));
                for (let i = 0; i < pos.length; i += stride * 3) {
                    pos[i] = base[i] + (Math.random() - 0.5) * jit;
                    pos[i + 1] = base[i + 1] + (Math.random() - 0.5) * jit;
                    pos[i + 2] = base[i + 2] + (Math.random() - 0.5) * jit;
                }
                mesh.geometry.attributes.position.needsUpdate = true;
            }

            const glow = this.regionGlows[name];
            if (glow) glow.material.opacity *= 0.88 + Math.sin(phase + 1) * 0.12;
        }

        this._animateAxons();
        this._animateCosmos(dt);
        this._updateSignals(dt);
        this._animateKnowledgeNodes();
        this._updateObsidianThoughtPulses(dt);
        this._updateKnowledgePackets(dt);
        this._updateLabelPositions();

        // Smooth camera animation (zoom-to-region)
        if (this._cameraAnim !== undefined && this._cameraGoto) {
            this._cameraAnim = Math.min(1, this._cameraAnim + dt * 2.0);
            const t = 1 - Math.pow(1 - this._cameraAnim, 3); // ease out cubic
            this.camera.position.lerpVectors(this._cameraStart, this._cameraGoto, t);
            this.controls.target.lerpVectors(this._controlsTargetStart, this._cameraTarget, t);
            if (this._cameraAnim >= 1) this._cameraGoto = null;
        }

        this.controls.update();
        if (this.usePostProcessing) {
            this.composer.render();
        } else {
            this.renderer.render(this.scene, this.camera);
        }
    }

    _updateLabelPositions() {
        const W = window.innerWidth, H = window.innerHeight;
        for (const [name, label] of Object.entries(this.labels)) {
            const layout = REGION_LAYOUT[name];
            if (!layout) continue;
            const pos = new THREE.Vector3(layout.x, layout.y, layout.z);
            pos.project(this.camera);
            if (pos.z > 1) { label.style.display = 'none'; continue; }
            const x = (pos.x * 0.5 + 0.5) * W;
            const y = (-pos.y * 0.5 + 0.5) * H;
            label.style.display = 'block';
            label.style.left = `${x}px`;
            label.style.top = `${y}px`;
            label.style.transform = 'translate(-50%,-100%) translateY(-10px)';
            // Labels remain visible regardless of distance as requested
            label.style.opacity = 1;
        }
    }

    _onResize() {
        const w = window.innerWidth, h = window.innerHeight;
        this.camera.aspect = w / h;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(w, h);
        if (this.composer) {
            this.composer.setSize(w, h);
        }
    }

    _onClick(event) {
        const rect = this.renderer.domElement.getBoundingClientRect();
        this.mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
        this.mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
        this.raycaster.setFromCamera(this.mouse, this.camera);
        const knowledgeProxyHits = this.raycaster.intersectObjects(this.knowledgeNodeHitMeshes);
        if (knowledgeProxyHits.length > 0) {
            const node = knowledgeProxyHits[0].object.userData.knowledgeNode;
            if (node?.id) {
                this.focusKnowledgeNode(node.id);
                window.dispatchEvent(new CustomEvent('knowledgeNodeClick', { detail: { id: node.id, node } }));
                return;
            }
        }
        if (this.knowledgeGraphPointMesh) {
            const graphHits = this.raycaster.intersectObject(this.knowledgeGraphPointMesh);
            if (graphHits.length > 0) {
                const node = this.knowledgeGraphNodeArray[graphHits[0].index];
                if (node?.id) {
                    this.focusKnowledgeNode(node.id);
                    window.dispatchEvent(new CustomEvent('knowledgeNodeClick', { detail: { id: node.id, node } }));
                    return;
                }
            }
        }
        const knowledgeHits = this.raycaster.intersectObjects(this.knowledgeNodeMeshes);
        if (knowledgeHits.length > 0) {
            const node = knowledgeHits[0].object.userData.knowledgeNode;
            if (node?.id) {
                this.focusKnowledgeNode(node.id);
                window.dispatchEvent(new CustomEvent('knowledgeNodeClick', { detail: { id: node.id, node } }));
                return;
            }
        }
        const hotspotHits = this.raycaster.intersectObjects(Object.values(this.regionHotspots).map((item) => item.mesh));
        if (hotspotHits.length > 0) {
            const info = hotspotHits[0].object.userData;
            this._zoomToRegion(info.regionName);
            window.dispatchEvent(new CustomEvent('regionClick', {
                detail: {
                    name: info.regionName,
                    label: regionLabel(info.regionName),
                    color: (REGION_COLORS[info.regionName] || 0xff6600).toString(16).padStart(6, '0'),
                    description: SVENSKA_REGIONBESKRIVNINGAR[info.regionName] || `Neural region: ${regionLabel(info.regionName)}`,
                    count: info.count,
                }
            }));
            return;
        }
        const hits = this.raycaster.intersectObjects(Object.values(this.regionMeshes));
        if (hits.length > 0) {
            const info = hits[0].object.userData;
            this._zoomToRegion(info.regionName);
            window.dispatchEvent(new CustomEvent('regionClick', {
                detail: {
                    name: info.regionName,
                    label: regionLabel(info.regionName),
                    color: (REGION_COLORS[info.regionName] || 0xff6600).toString(16).padStart(6, '0'),
                    description: SVENSKA_REGIONBESKRIVNINGAR[info.regionName] || `Neural region: ${regionLabel(info.regionName)}`,
                    count: info.count,
                }
            }));
        }
    }

    _zoomToRegion(regionName) {
        const layout = REGION_LAYOUT[regionName];
        if (!layout) return;
        const target = new THREE.Vector3(layout.x, layout.y, layout.z);
        const offset = target.clone().add(new THREE.Vector3(0, 0, 3.5));
        // Smooth camera animation
        this._cameraTarget = target.clone();
        this._cameraGoto = offset.clone();
        this._cameraAnim = 0;
        this._cameraStart = this.camera.position.clone();
        this._controlsTargetStart = this.controls.target.clone();
    }

    _initKeyboardShortcuts() {
        window.addEventListener('keydown', (e) => {
            if (isTypingTarget(e.target) || e.ctrlKey || e.metaKey || e.altKey) {
                return;
            }

            switch(e.key) {
                case ' ':
                    e.preventDefault();
                    this.controls.autoRotate = !this.controls.autoRotate;
                    break;
                case 'r': case 'R':
                    this._cameraGoto = new THREE.Vector3(0, 0, 9.0);
                    this._cameraTarget = new THREE.Vector3(0, 0.1, 0);
                    this._cameraAnim = 0;
                    this._cameraStart = this.camera.position.clone();
                    this._controlsTargetStart = this.controls.target.clone();
                    break;
                case 'f': case 'F':
                    if (!document.fullscreenElement) document.documentElement.requestFullscreen();
                    else document.exitFullscreen();
                    break;
            }
        });
    }
}
