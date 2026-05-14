import * as THREE from "three";

export class AmyOrb {
  private readonly renderer: THREE.WebGLRenderer;
  private readonly scene = new THREE.Scene();
  private readonly camera = new THREE.PerspectiveCamera(45, 1, 0.1, 100);
  private readonly group = new THREE.Group();
  private pulse = 0.5;

  constructor(private readonly canvas: HTMLCanvasElement) {
    this.renderer = new THREE.WebGLRenderer({ canvas, antialias: true, alpha: true });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.camera.position.set(0, 0, 7);
    this.scene.add(this.group);

    const core = new THREE.Mesh(
      new THREE.IcosahedronGeometry(1.15, 4),
      new THREE.MeshStandardMaterial({
        color: 0x6ff6ff,
        emissive: 0x1d6c84,
        roughness: 0.28,
        metalness: 0.35,
        transparent: true,
        opacity: 0.88
      })
    );
    this.group.add(core);

    const ringMaterial = new THREE.MeshBasicMaterial({ color: 0xffd36a, transparent: true, opacity: 0.6 });
    for (let i = 0; i < 3; i += 1) {
      const ring = new THREE.Mesh(new THREE.TorusGeometry(1.7 + i * 0.28, 0.012, 12, 160), ringMaterial.clone());
      ring.rotation.set(Math.PI / (2 + i), i * 0.7, i * 0.4);
      this.group.add(ring);
    }

    const particles = new THREE.BufferGeometry();
    const positions = new Float32Array(900);
    for (let i = 0; i < positions.length; i += 3) {
      const r = 2.0 + Math.random() * 1.8;
      const a = Math.random() * Math.PI * 2;
      const b = Math.acos(2 * Math.random() - 1);
      positions[i] = r * Math.sin(b) * Math.cos(a);
      positions[i + 1] = r * Math.sin(b) * Math.sin(a);
      positions[i + 2] = r * Math.cos(b);
    }
    particles.setAttribute("position", new THREE.BufferAttribute(positions, 3));
    this.group.add(
      new THREE.Points(
        particles,
        new THREE.PointsMaterial({ color: 0xd987ff, size: 0.018, transparent: true, opacity: 0.72 })
      )
    );

    this.scene.add(new THREE.AmbientLight(0xb6f4ff, 0.65));
    const key = new THREE.DirectionalLight(0xffffff, 1.8);
    key.position.set(3, 5, 4);
    this.scene.add(key);

    window.addEventListener("resize", () => this.resize());
    this.resize();
    this.animate();
  }

  setReadiness(configuredProviders: number) {
    this.pulse = 0.55 + configuredProviders * 0.12;
  }

  private resize() {
    const box = this.canvas.getBoundingClientRect();
    this.camera.aspect = Math.max(1, box.width) / Math.max(1, box.height);
    this.camera.updateProjectionMatrix();
    this.renderer.setSize(Math.max(1, box.width), Math.max(1, box.height), false);
  }

  private animate = () => {
    const t = performance.now() * 0.001;
    this.group.rotation.y = t * 0.18;
    this.group.rotation.x = Math.sin(t * 0.35) * 0.18;
    this.group.scale.setScalar(1 + Math.sin(t * 2.2) * 0.025 * this.pulse);
    this.renderer.render(this.scene, this.camera);
    requestAnimationFrame(this.animate);
  };
}
