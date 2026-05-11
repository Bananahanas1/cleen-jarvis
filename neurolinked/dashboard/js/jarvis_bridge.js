/**
 * JarvisBridgeDashboard - owns the visible Jarvis bridge and memory bridge.
 *
 * The C# app owns model access and tools; this module only renders runtime
 * truth from NeuroLinked endpoints and dashboard chat events.
 */
export class JarvisBridgeDashboard {
    constructor({ showAlert, sendCommand } = {}) {
        this.showAlert = showAlert || (() => {});
        this.sendCommand = sendCommand || (() => {});
        this.latestMemoryId = null;
        this.statusTimer = null;
        this.localEvents = [];

        this.els = {
            jarvisDot: document.getElementById('jarvis-dot'),
            jarvisConn: document.getElementById('jarvis-conn-text'),
            jarvisStatusText: document.getElementById('jarvis-status-text'),
            jarvisLatency: document.getElementById('jarvis-latency'),
            jarvisQueue: document.getElementById('jarvis-queue'),
            jarvisModel: document.getElementById('jarvis-model'),
            jarvisMode: document.getElementById('jarvis-mode'),
            jarvisLast: document.getElementById('jarvis-last-event'),
            jarvisError: document.getElementById('jarvis-last-error'),
            jarvisCapabilities: document.getElementById('jarvis-capabilities'),
            jarvisActivity: document.getElementById('jarvis-activity-log'),
            jarvisMemories: document.getElementById('jarvis-memories'),
            jarvisNeurons: document.getElementById('jarvis-neurons'),
            jarvisStage: document.getElementById('jarvis-stage'),
            refresh: document.getElementById('btn-jarvis-refresh'),

            memoryDot: document.getElementById('claude-dot'),
            memoryConn: document.getElementById('claude-conn-text'),
            memoryInteractions: document.getElementById('claude-interactions'),
            memoryScreen: document.getElementById('claude-screen'),
            memoryMotion: document.getElementById('claude-motion'),
            memoryAi: document.getElementById('memory-ai-model'),
            memoryShort: document.getElementById('memory-short-term'),
            memoryLong: document.getElementById('memory-long-term'),
            memoryLastSaved: document.getElementById('memory-last-saved'),
            memoryLastSource: document.getElementById('memory-last-source'),
            memoryQuality: document.getElementById('memory-quality'),
            memoryRecall: document.getElementById('memory-recall'),
            memoryPreview: document.getElementById('memory-preview'),
            forget: document.getElementById('btn-forget-memory'),
        };

        this.els.refresh?.addEventListener('click', () => {
            this.showAlert('+', 'Uppdaterar Jarvis-bryggan...', '#4ad3f5');
            this.pollStatus();
        });

        this.els.forget?.addEventListener('click', () => {
            this.forgetLatestMemory();
        });
    }

    start() {
        this.pollStatus();
        this.statusTimer = window.setInterval(() => this.pollStatus(), 6000);
    }

    addMessage(role, text) {
        const normalized = this.normalizeAssistantText(role, text);
        this.renderChatMessage(role, normalized.text);
        this.recordLocalEvent(normalized.event, normalized.level);
        window.jarvisKnowledgeEvent?.({
            type: role === 'assistant' ? 'assistant_response' : 'chat_message',
            source: role === 'assistant' ? 'jarvis_bridge' : 'chat',
            target: role === 'assistant' ? 'chat' : 'memory_bridge',
            label: normalized.event,
            status: normalized.level,
        });

        if (normalized.level === 'warn') {
            this.setBridgeHealth('warn', 'FALLBACK/KVOT');
            this.setText(this.els.jarvisError, normalized.event);
        } else if (role === 'assistant') {
            this.setBridgeHealth('ok', 'ANSLUTEN');
            this.setText(this.els.jarvisLast, 'Svar skickat till dashboarden');
        }

        return normalized.text;
    }

    normalizeAssistantText(role, text) {
        const raw = String(text || '');
        if (role !== 'assistant') {
            return { text: raw, event: 'Användarmeddelande skickat', level: 'ok' };
        }

        const quotaPattern = /(insufficient_quota|exceeded your current quota|HTTP 429|usage\/kvot|usage limit|quota)/i;
        if (quotaPattern.test(raw)) {
            return {
                level: 'warn',
                event: 'OpenAI-gränsen är nådd',
                text: [
                    'OpenAI-gränsen är nådd just nu.',
                    'Jag kan fortsätta med gratis textfallback via Groq eller lokal Ollama/Qwen3 om den är igång.',
                    'Bilder, filer, datorstyrning och Codex-agenten kräver fortfarande OpenAI.'
                ].join('\n')
            };
        }

        return { text: raw, event: 'Jarvis svarade', level: 'ok' };
    }

    renderChatMessage(role, text) {
        const panel = document.getElementById('jarvis-chat-panel');
        if (!panel) return;
        panel.style.display = 'flex';

        const msgs = document.getElementById('jarvis-chat-msgs');
        const thinking = document.getElementById('jarvis-chat-thinking');
        if (!msgs) return;
        if (thinking) thinking.style.display = 'none';

        const div = document.createElement('div');
        div.className = `jarvis-msg ${role === 'user' ? 'user' : 'assistant'}`;
        div.textContent = text;
        msgs.appendChild(div);
        msgs.scrollTop = msgs.scrollHeight;
    }

    async pollStatus() {
        const started = performance.now();
        try {
            const response = await fetch('/api/jarvis/status', { cache: 'no-store' });
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const status = await response.json();
            const latencyMs = Math.max(1, Math.round(performance.now() - started));
            this.renderStatus(status, latencyMs);
            window.jarvisKnowledgeEvent?.({
                type: 'bridge_status',
                source: 'jarvis_bridge',
                target: 'memory_bridge',
                label: status.bridge?.last_event || 'Jarvis-bryggan synkar',
                status: status.bridge?.health || 'ok',
            });
        } catch (error) {
            this.setBridgeHealth('down', 'FRÅNKOPPLAD');
            this.setMemoryHealth('down', 'FRÅNKOPPLAD');
            this.setText(this.els.jarvisError, error?.message || 'Ingen status från NeuroLinked');
            window.jarvisKnowledgeEvent?.({
                type: 'bridge_status',
                source: 'jarvis_bridge',
                target: 'memory_bridge',
                label: error?.message || 'Ingen status fran NeuroLinked',
                status: 'down',
            });
        }
    }

    renderStatus(status, latencyMs) {
        const bridge = status.bridge || {};
        const memory = status.memory_bridge || {};
        const capabilities = status.capabilities || [];
        const activity = status.recent_activity || [];

        this.setBridgeHealth(bridge.health || 'ok', bridge.label || 'ANSLUTEN');
        this.setMemoryHealth(memory.health || 'ok', memory.label || 'ANSLUTEN');

        this.setText(this.els.jarvisStatusText, bridge.description || 'Jarvis och NeuroLinked synkar.');
        this.setText(this.els.jarvisLatency, `${latencyMs} ms`);
        this.setText(this.els.jarvisQueue, `${status.pending_inputs ?? bridge.pending_inputs ?? 0}`);
        this.setText(this.els.jarvisModel, status.model_display || 'gpt-5.4-mini + gpt-5-codex + Ollama qwen3:1.7b');
        this.setText(this.els.jarvisMode, bridge.mode || 'Chat + agent + verktyg');
        this.setText(this.els.jarvisLast, bridge.last_event || this.localEvents[0]?.label || 'Väntar på händelser');
        this.setText(this.els.jarvisError, bridge.last_error || 'Inga fel');
        this.setText(this.els.jarvisMemories, this.formatNum(memory.total_entries ?? status.knowledge_entries ?? 0));
        this.setText(this.els.jarvisNeurons, this.formatNum(status.neurons ?? status.total_neurons ?? 0));
        this.setText(this.els.jarvisStage, this.translateStage(status.stage ?? status.development_stage));

        this.setText(this.els.memoryInteractions, this.formatNum(memory.interactions ?? status.interaction_count ?? 0));
        this.setText(this.els.memoryAi, status.agent_model || 'Jarvis Codex');
        this.setText(this.els.memoryShort, this.formatNum(memory.short_term ?? status.pending_inputs ?? 0));
        this.setText(this.els.memoryLong, this.formatNum(memory.total_entries ?? status.knowledge_entries ?? 0));
        this.setText(this.els.memoryLastSaved, memory.last_saved || 'Inget nytt ännu');
        this.setText(this.els.memoryLastSource, memory.last_source || '--');
        this.setText(this.els.memoryQuality, memory.quality || 'Väntar');
        this.setText(this.els.memoryRecall, memory.last_recall || 'Ingen återkallning ännu');
        this.setText(this.els.memoryPreview, memory.preview || 'När Jarvis sparar eller återkallar något visas det här.');

        this.latestMemoryId = memory.latest_id || null;
        this.renderCapabilities(capabilities);
        this.renderActivity(activity);

        if (status.screen_observer) {
            this.setText(this.els.memoryScreen, status.screen_observer.active ? 'PÅ' : 'AV');
            if (status.screen_observer.motion !== undefined) {
                this.setText(this.els.memoryMotion, `${Math.round(status.screen_observer.motion * 100)}%`);
            }
        }
    }

    renderCapabilities(capabilities) {
        if (!this.els.jarvisCapabilities) return;
        this.els.jarvisCapabilities.innerHTML = '';
        for (const capability of capabilities) {
            const chip = document.createElement('div');
            chip.className = `bridge-chip ${capability.status || 'ok'}`;
            chip.textContent = capability.label || capability.id || 'Okänd';
            chip.title = capability.description || chip.textContent;
            this.els.jarvisCapabilities.appendChild(chip);
        }
    }

    renderActivity(serverActivity) {
        if (!this.els.jarvisActivity) return;
        const merged = [
            ...this.localEvents,
            ...serverActivity.map((entry) => ({
                label: this.formatActivity(entry),
                level: 'ok',
            })),
        ].slice(0, 5);

        this.els.jarvisActivity.innerHTML = '';
        for (const entry of merged) {
            const item = document.createElement('div');
            item.className = `bridge-log-row ${entry.level || 'ok'}`;
            item.textContent = entry.label;
            this.els.jarvisActivity.appendChild(item);
        }
    }

    async forgetLatestMemory() {
        if (!this.latestMemoryId) {
            this.showAlert('!', 'Det finns inget senaste minne att glömma.', '#ff8844');
            return;
        }

        if (!window.confirm('Glöm senaste minnet i NeuroLinked? Detta tar bara bort posten från kunskapslagret.')) {
            return;
        }

        try {
            const response = await fetch(`/api/jarvis/knowledge/${this.latestMemoryId}`, { method: 'DELETE' });
            const data = await response.json();
            if (!response.ok || data.ok === false) {
                throw new Error(data.error || 'Kunde inte glömma minnet.');
            }
            this.showAlert('+', 'Senaste minnet glömdes.', '#44ffaa');
            this.recordLocalEvent('Minnespost glömdes', 'warn');
            this.latestMemoryId = null;
            await this.pollStatus();
        } catch (error) {
            this.showAlert('!', error?.message || 'Kunde inte glömma minnet.', '#ff8844');
        }
    }

    recordLocalEvent(label, level = 'ok') {
        this.localEvents.unshift({ label, level, time: Date.now() });
        this.localEvents = this.localEvents.slice(0, 5);
        this.renderActivity([]);
        window.jarvisKnowledgeEvent?.({
            type: 'bridge_activity',
            source: 'memory_bridge',
            target: 'hippocampus',
            label,
            status: level,
        });
    }

    formatActivity(entry) {
        const source = entry.source || entry.obs_type || entry.type || 'händelse';
        const type = entry.obs_type || entry.type || 'input';
        return `${this.translateSource(source)}: ${this.translateEventType(type)}`;
    }

    setBridgeHealth(level, label) {
        this.setHealth(this.els.jarvisDot, this.els.jarvisConn, level, label);
    }

    setMemoryHealth(level, label) {
        this.setHealth(this.els.memoryDot, this.els.memoryConn, level, label);
    }

    setHealth(dot, textEl, level, label) {
        const color = level === 'down' ? '#667' : level === 'warn' ? '#ffaa33' : '#44ffaa';
        const glow = level === 'down' ? 'none' : `0 0 8px ${color}aa`;
        if (dot) {
            dot.style.background = color;
            dot.style.boxShadow = glow;
        }
        this.setText(textEl, label);
    }

    setText(el, value) {
        if (el) el.textContent = value ?? '--';
    }

    formatNum(value) {
        if (value === undefined || value === null || value === '') return '--';
        const number = Number(value);
        return Number.isFinite(number) ? number.toLocaleString('sv-SE') : String(value);
    }

    translateStage(stage) {
        const stages = {
            EMBRYONIC: 'EMBRYONAL',
            JUVENILE: 'UNG',
            ADOLESCENT: 'VÄXANDE',
            MATURE: 'MOGEN',
        };
        return stages[stage] || stage || '--';
    }

    translateSource(source) {
        const sources = {
            jarvis: 'Jarvis',
            jarvis_user: 'Användare',
            jarvis_assistant: 'Jarvis-svar',
            dashboard: 'Dashboard',
            screen_observer: 'Skärm',
            user: 'Användare',
        };
        return sources[source] || String(source).replace(/_/g, ' ');
    }

    translateEventType(type) {
        const types = {
            input: 'ny input',
            text: 'textminne',
            action: 'verktygshändelse',
            context: 'kontext',
        };
        return types[type] || String(type).replace(/_/g, ' ');
    }
}
