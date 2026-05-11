/**
 * BrainTimeline — Compact premium EEG-style activity widget
 * Positioned bottom-left, above the Jarvis bridge panel.
 * CSS handles glassmorphism (GPU), canvas only draws lines.
 */
export class BrainTimeline {
    constructor() {
        this.history = {};
        this.maxSamples = 80;
        this.sampleInterval = 600; // ms between samples
        this._lastSample = 0;
        this._dirty = false;

        this._regionColors = {
            sensory_cortex: '#ff6600',
            feature_layer:  '#00c8ff',
            association:    '#ff8c00',
            concept_layer:  '#ffcc00',
            predictive:     '#ff3399',
            motor_cortex:   '#ffaa00',
            cerebellum:     '#00ff88',
            reflex_arc:     '#ff8800',
            brainstem:      '#ff6600',
            hippocampus:    '#00e5ff',
            prefrontal:     '#aa88ff',
        };

        this._build();
    }

    _build() {
        // Outer container — CSS glass, no canvas background needed
        this.container = document.createElement('div');
        this.container.id = 'timeline-container';
        this.container.style.cssText = `
            position: fixed;
            top: clamp(252px, 31vh, 282px);
            left: 15px;
            width: 260px;
            height: 72px;
            z-index: 10;
            background: rgba(3, 6, 14, 0.82);
            backdrop-filter: blur(6px);
            -webkit-backdrop-filter: blur(6px);
            border: 1px solid rgba(255, 136, 68, 0.14);
            border-radius: 10px;
            overflow: hidden;
            pointer-events: none;
        `;

        // Header bar
        const header = document.createElement('div');
        header.style.cssText = `
            position: absolute; top: 0; left: 0; right: 0;
            padding: 4px 8px;
            font-family: 'Consolas', monospace;
            font-size: 8px;
            letter-spacing: 2px;
            color: rgba(255,136,68,0.55);
            display: flex;
            justify-content: space-between;
            align-items: center;
            z-index: 1;
        `;
        this._statusDot = document.createElement('span');
        this._statusDot.style.cssText = `
            display:inline-block; width:5px; height:5px;
            border-radius:50%; background:#44ffaa;
            box-shadow:0 0 5px rgba(68,255,170,0.8);
            animation: pulse 2s ease-in-out infinite;
        `;
        header.innerHTML = '<span>NEURAL AKTIVITET</span>';
        header.appendChild(this._statusDot);
        this.container.appendChild(header);

        // Canvas — transparent bg, only lines
        this.canvas = document.createElement('canvas');
        this.canvas.width = 260;
        this.canvas.height = 72;
        this.canvas.style.cssText = 'position:absolute;top:0;left:0;width:100%;height:100%;';
        this.container.appendChild(this.canvas);

        document.body.appendChild(this.container);
        this.ctx = this.canvas.getContext('2d');
    }

    update(state, now) {
        if (now - this._lastSample < this.sampleInterval) return;
        this._lastSample = now;

        const regionFiring = state?.region_firing || {};
        for (const name of Object.keys(this._regionColors)) {
            if (!this.history[name]) this.history[name] = [];
            this.history[name].push(regionFiring[name] || 0);
            if (this.history[name].length > this.maxSamples)
                this.history[name].shift();
        }
        this._draw();
    }

    _draw() {
        const ctx = this.ctx;
        const W = this.canvas.width;
        const H = this.canvas.height;

        // Clear only — background is CSS (faster)
        ctx.clearRect(0, 0, W, H);

        // Subtle bottom axis
        ctx.strokeStyle = 'rgba(255,136,68,0.08)';
        ctx.lineWidth = 0.5;
        ctx.beginPath();
        ctx.moveTo(4, H - 6);
        ctx.lineTo(W - 4, H - 6);
        ctx.stroke();

        const padTop = 18;
        const padBot = 8;
        const graphH = H - padTop - padBot;
        const graphW = W - 8;

        ctx.save();
        for (const [name, samples] of Object.entries(this.history)) {
            if (samples.length < 2) continue;
            ctx.beginPath();
            ctx.strokeStyle = this._regionColors[name];
            ctx.lineWidth = 0.9;
            ctx.globalAlpha = 0.5;
            for (let i = 0; i < samples.length; i++) {
                const x = 4 + (i / (this.maxSamples - 1)) * graphW;
                const y = padTop + graphH - Math.min(samples[i] * graphH * 8, graphH);
                i === 0 ? ctx.moveTo(x, y) : ctx.lineTo(x, y);
            }
            ctx.stroke();
        }
        ctx.restore();
    }
}
