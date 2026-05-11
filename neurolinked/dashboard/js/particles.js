/**
 * NeuralDust - svävande neurala partiklar utan fallande stjärnor.
 * Den här ersätter bara atmosfären, inte "shooting stars".
 */
import * as THREE from 'three';

export class NeuralDust {
    constructor(scene, count = 4200) {
        this.count = count;
        this.scene = scene;

        const geometry = new THREE.BufferGeometry();
        const positions  = new Float32Array(count * 3);
        const colors     = new Float32Array(count * 3);
        const velocities = new Float32Array(count * 3);

        // Warm/cool neural palette. This keeps atmosphere alive without reintroducing
        // the removed falling-star effect.
        const palette = [
            new THREE.Color(0xff6600),   // warm orange synapse
            new THREE.Color(0xff9944),   // soft amber
            new THREE.Color(0x0088ff),   // electric blue axon
            new THREE.Color(0x00ccff),   // cyan
            new THREE.Color(0xffcc00),   // gold
            new THREE.Color(0xff3366),   // hot pink
            new THREE.Color(0x00e5ff),   // bright cyan
        ];

        for (let i = 0; i < count; i++) {
            const r     = 0.7 + Math.random() * 5.4;
            const theta = Math.random() * Math.PI * 2;
            const phi   = Math.acos(2 * Math.random() - 1);

            positions[i * 3]     = r * Math.sin(phi) * Math.cos(theta);
            positions[i * 3 + 1] = r * Math.sin(phi) * Math.sin(theta) * 0.7 + 0.1;
            positions[i * 3 + 2] = r * Math.cos(phi);

            velocities[i * 3]     = (Math.random() - 0.5) * 0.010;
            velocities[i * 3 + 1] = (Math.random() - 0.5) * 0.007;
            velocities[i * 3 + 2] = (Math.random() - 0.5) * 0.010;

            const c = palette[Math.floor(Math.random() * palette.length)];
            colors[i * 3]     = c.r;
            colors[i * 3 + 1] = c.g;
            colors[i * 3 + 2] = c.b;
        }

        geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
        geometry.setAttribute('color',    new THREE.BufferAttribute(colors, 3));
        this._velocities = velocities;

        // Soft orb texture makes each point read as neural dust instead of square pixels.
        const texCanvas = document.createElement('canvas');
        texCanvas.width  = 32;
        texCanvas.height = 32;
        const tCtx = texCanvas.getContext('2d');
        const grad = tCtx.createRadialGradient(16, 16, 0, 16, 16, 16);
        grad.addColorStop(0,   'rgba(255,255,255,1)');
        grad.addColorStop(0.2, 'rgba(255,200,150,0.6)');
        grad.addColorStop(0.5, 'rgba(255,120,40,0.15)');
        grad.addColorStop(1,   'rgba(0,0,0,0)');
        tCtx.fillStyle = grad;
        tCtx.fillRect(0, 0, 32, 32);
        const dustTexture = new THREE.CanvasTexture(texCanvas);

        const material = new THREE.PointsMaterial({
            size: 0.024,
            map: dustTexture,
            transparent: true,
            opacity: 0.55,
            blending: THREE.AdditiveBlending,
            depthWrite: false,
            sizeAttenuation: true,
            vertexColors: true,
        });

        this.mesh = new THREE.Points(geometry, material);
        scene.add(this.mesh);
    }

    /**
     * @param {number} time - Elapsed seconds
     * @param {number} activity - Global brain activity 0..1
     */
    update(time, activity = 0.1) {
        const positions = this.mesh.geometry.attributes.position.array;
        const vel = this._velocities;

        const normalizedActivity = Math.max(0, Math.min(1, activity));
        const speed = 0.45 + normalizedActivity * 2.2;

        for (let i = 0; i < this.count; i++) {
            const i3 = i * 3;

            positions[i3]     += vel[i3]     * speed;
            positions[i3 + 1] += vel[i3 + 1] * speed;
            positions[i3 + 2] += vel[i3 + 2] * speed;

            // Tiny swirl keeps the field alive while avoiding obvious star streaks.
            const angle = time * 0.04 + i * 0.002;
            positions[i3]     += Math.sin(angle) * 0.0002;
            positions[i3 + 2] += Math.cos(angle) * 0.0002;

            // Respawn near the brain when particles drift too far from the scene.
            const x = positions[i3], y = positions[i3+1], z = positions[i3+2];
            const dist = Math.sqrt(x*x + y*y + z*z);
            if (dist > 6.2) {
                const r     = 0.6 + Math.random() * 1.8;
                const theta = Math.random() * Math.PI * 2;
                const phi   = Math.acos(2 * Math.random() - 1);
                positions[i3]     = r * Math.sin(phi) * Math.cos(theta);
                positions[i3 + 1] = r * Math.sin(phi) * Math.sin(theta) * 0.7 + 0.1;
                positions[i3 + 2] = r * Math.cos(phi);
            }
        }

        this.mesh.geometry.attributes.position.needsUpdate = true;

        // Size and opacity react to brain activity so memory/neuron motion feels linked.
        this.mesh.material.opacity = 0.32 + normalizedActivity * 0.42;
        this.mesh.material.size    = 0.014 + normalizedActivity * 0.026;
    }
}
