/**
 * Dashboard - uppdaterar hjärnans baspaneler med live-tillstånd.
 *
 * JarvisBridgeDashboard äger Jarvis-/minnespanelerna. Den här klassen ska
 * därför bara spegla hjärnsimulationen och enkla sensorer från websocketen.
 */
export class Dashboard {
    constructor() {
        this.els = {
            neurons: document.getElementById('neurons-val'),
            synapses: document.getElementById('synapses-val'),
            step: document.getElementById('step-val'),
            rate: document.getElementById('rate-val'),
            stage: document.getElementById('stage-badge'),
            barDa: document.getElementById('bar-da'),
            barAch: document.getElementById('bar-ach'),
            barNe: document.getElementById('bar-ne'),
            bar5ht: document.getElementById('bar-5ht'),
            regionList: document.getElementById('region-list'),
            safetyStatus: document.getElementById('safety-status'),
            claudeInteractions: document.getElementById('claude-interactions'),
            claudeScreen: document.getElementById('claude-screen'),
            claudeMotion: document.getElementById('claude-motion'),
            claudeDot: document.getElementById('claude-dot'),
            claudeConnText: document.getElementById('claude-conn-text'),
        };
        this._regionEls = {};
    }

    updateState(state) {
        if (!state) return;

        this._setText(this.els.neurons, this._formatNum(state.total_neurons));
        this._setText(this.els.synapses, this._formatNum(state.total_synapses));
        this._setText(this.els.step, this._formatNum(state.step));
        this._setText(this.els.rate, `${(state.steps_per_second || 0).toFixed(0)} Hz`);

        if (this.els.stage && state.development_stage) {
            this.els.stage.textContent = this._translateStage(state.development_stage);
            this.els.stage.className = `stage-badge stage-${state.development_stage}`;
        }

        const nm = state.neuromodulators || {};
        this._setBar(this.els.barDa, nm.dopamine);
        this._setBar(this.els.barAch, nm.acetylcholine);
        this._setBar(this.els.barNe, nm.norepinephrine);
        this._setBar(this.els.bar5ht, nm.serotonin);

        this._updateRegions(state.regions, state.region_firing);
        this._updateSafety(state.safety);

        if (state.claude) {
            this._setText(this.els.claudeInteractions, state.claude.interactions || 0);
            if (this.els.claudeDot) this.els.claudeDot.style.background = '#00ff88';
            this._setText(this.els.claudeConnText, 'ANSLUTEN');
        }

        if (state.screen_observer) {
            this._setText(this.els.claudeScreen, state.screen_observer.active ? 'PÅ' : 'AV');
            if (state.screen_observer.motion !== undefined) {
                this._setText(this.els.claudeMotion, `${(state.screen_observer.motion * 100).toFixed(0)}%`);
            }
        }
    }

    _updateRegions(regions, firingRates) {
        if (!regions || !this.els.regionList) return;

        const regionColors = {
            sensory_cortex: '#00ffff',
            feature_layer: '#00ccff',
            association: '#ff6600',
            concept_layer: '#ffcc00',
            predictive: '#ff00ff',
            motor_cortex: '#ff3366',
            cerebellum: '#66ff66',
            reflex_arc: '#ff4444',
            brainstem: '#ff8800',
            hippocampus: '#00ff88',
            prefrontal: '#aa88ff',
        };

        for (const [name, data] of Object.entries(regions)) {
            let el = this._regionEls[name];
            if (!el) {
                el = document.createElement('div');
                el.className = 'region-row';
                el.innerHTML = `
                    <span class="region-name" style="color: ${regionColors[name] || '#aaa'}">${this._translateRegion(name).toUpperCase()}</span>
                    <div class="region-bar-track">
                        <div class="region-bar-fill" style="background: ${regionColors[name] || '#aaa'}"></div>
                    </div>
                    <span class="region-rate">0%</span>
                `;
                el.addEventListener('click', () => {
                    window.dispatchEvent(new CustomEvent('regionClick', {
                        detail: {
                            name,
                            label: this._translateRegion(name),
                            color: (regionColors[name] || '#ffffff').replace('#', ''),
                            description: `Neural region: ${this._translateRegion(name)}`,
                            count: data.neuron_count || 0,
                        }
                    }));
                });
                this.els.regionList.appendChild(el);
                this._regionEls[name] = el;
            }

            const rate = data.firing_rate || firingRates?.[name] || 0;
            const barFill = el.querySelector('.region-bar-fill');
            const rateSpan = el.querySelector('.region-rate');
            if (barFill) barFill.style.width = `${Math.min(rate * 100, 100)}%`;
            if (rateSpan) rateSpan.textContent = `${(rate * 100).toFixed(1)}%`;
        }
    }

    _updateSafety(safety) {
        if (!safety || !this.els.safetyStatus) return;

        const isOk = !safety.emergency_stop && safety.block_rate < 0.1;
        const icon = isOk ? '&#10003;' : '&#9888;';
        const cls = isOk ? 'safety-ok' : 'safety-warn';
        const text = safety.emergency_stop
            ? 'NÖDSTOPP'
            : isOk
                ? `NORMALT (${safety.passed} godkända)`
                : `VARNING: ${(safety.block_rate * 100).toFixed(1)}% blockerade`;

        this.els.safetyStatus.innerHTML = `
            <div class="safety-status">
                <span class="safety-icon ${cls}">${icon}</span>
                <span class="${cls}">${text}</span>
            </div>
        `;
    }

    _setText(el, val) {
        if (el) el.textContent = val;
    }

    _setBar(el, val) {
        if (el && val !== undefined) {
            el.style.width = `${Math.min(Math.max(val * 100, 0), 100)}%`;
        }
    }

    _formatNum(n) {
        if (n === undefined || n === null) return '--';
        return n.toLocaleString('sv-SE');
    }

    _translateStage(stage) {
        const stages = {
            EMBRYONIC: 'EMBRYONAL',
            JUVENILE: 'UNG',
            ADOLESCENT: 'VÄXANDE',
            MATURE: 'MOGEN',
        };
        return stages[stage] || stage;
    }

    _translateRegion(name) {
        const labels = {
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
        return labels[name] || name.replace(/_/g, ' ');
    }
}
