export function formatKindLabel(kind) {
    const labels = {
        jarvis_memory: 'Jarvis-minne',
        neurolinked_semantic: 'NeuroLinked-nod',
        graphify_code: 'Graphify-kod',
        obsidian_note: 'Obsidian-note',
        decision: 'Beslut',
        open_issue: 'Öppet issue',
    };
    return labels[kind] || String(kind || 'okänd').replace(/_/g, ' ');
}

export function statusClassForChip(chip) {
    const status = chip?.status;
    return status === 'warn' || status === 'down' ? status : 'ok';
}

export function summarizeKnowledgeNode(node) {
    if (!node) return 'Ingen nod vald.';
    const confidence = Math.round(Number(node.confidence || 0) * 100);
    const pieces = [
        node.label || 'Okänd nod',
        formatKindLabel(node.kind),
        `${confidence}% säkerhet`,
        node.region ? `region: ${node.region}` : '',
        node.path || '',
    ].filter(Boolean);
    return pieces.join(' • ');
}

function escapeHtml(value) {
    return String(value ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

export class KnowledgePanel {
    constructor({ showAlert } = {}) {
        this.showAlert = showAlert || (() => {});
        this.nodes = new Map();
        this.currentMap = null;
        this.selectedNode = null;
        this.statusTimer = null;
        this.els = {};
    }

    start() {
        this.ensureDom();
        this.bindEvents();
        this.registerGlobalBridge();
        this.refresh();
        this.statusTimer = window.setInterval(() => this.refresh({ quiet: true }), 18000);
    }

    ensureDom() {
        if (document.getElementById('knowledge-panel')) {
            this.cacheElements();
            return;
        }

        const panel = document.createElement('section');
        panel.id = 'knowledge-panel';
        panel.className = 'glass-panel knowledge-panel';
        panel.innerHTML = `
            <div class="knowledge-header">
                <div>
                    <span class="knowledge-kicker">KUNSKAPSKARTA</span>
                    <strong>Minne × Graphify × Obsidian</strong>
                </div>
                <button class="control-btn knowledge-collapse" id="knowledge-collapse" title="Dölj/visa kunskapspanelen">–</button>
            </div>
            <div id="knowledge-panel-body">
                <div id="knowledge-status-chips" class="bridge-chip-grid"></div>
                <div class="knowledge-search-row">
                    <input id="knowledge-search-input" type="text" placeholder="Sök minne, kod eller note...">
                    <button class="control-btn" id="knowledge-search-btn">Sök</button>
                </div>
                <div id="knowledge-node-list" class="knowledge-node-list"></div>
                <div id="knowledge-inspector" class="knowledge-inspector">
                    <div class="knowledge-empty">Klicka på en kunskapsnod i listan eller i 3D-vyn.</div>
                </div>
            </div>
        `;
        document.body.appendChild(panel);
        this.cacheElements();
    }

    cacheElements() {
        this.els.panel = document.getElementById('knowledge-panel');
        this.els.body = document.getElementById('knowledge-panel-body');
        this.els.collapse = document.getElementById('knowledge-collapse');
        this.els.searchInput = document.getElementById('knowledge-search-input');
        this.els.searchButton = document.getElementById('knowledge-search-btn');
        this.els.nodeList = document.getElementById('knowledge-node-list');
        this.els.inspector = document.getElementById('knowledge-inspector');
        this.els.statusChips = document.getElementById('knowledge-status-chips');
    }

    bindEvents() {
        this.els.searchButton?.addEventListener('click', () => this.search());
        this.els.searchInput?.addEventListener('keydown', (event) => {
            if (event.key === 'Enter') this.search();
        });
        this.els.collapse?.addEventListener('click', () => {
            const collapsed = this.els.body?.classList.toggle('collapsed');
            this.els.collapse.textContent = collapsed ? '+' : '–';
        });

        window.addEventListener('knowledgeNodeClick', (event) => {
            const id = event.detail?.id;
            if (id) this.selectNode(id, { focus3d: false });
        });
    }

    registerGlobalBridge() {
        // C# and other dashboard modules use these globals as a tiny event bus
        // without importing this panel directly.
        window.jarvisOpenKnowledgeInspector = (id) => this.selectNode(id);
        window.jarvisHighlightKnowledgeNode = (id) => {
            window.dispatchEvent(new CustomEvent('knowledgeNodeFocus', { detail: { id } }));
        };
        window.jarvisKnowledgeEvent = (detail) => {
            window.dispatchEvent(new CustomEvent('jarvisKnowledgeEvent', { detail: detail || {} }));
        };
    }

    async search() {
        const query = this.els.searchInput?.value?.trim() || 'JarvisForm';
        window.jarvisKnowledgeEvent?.({
            type: 'knowledge_search',
            source: 'chat',
            target: query.toLowerCase().includes('obsidian') ? 'obsidian' : 'graphify',
            label: query,
            status: 'ok',
        });
        await this.refresh({ query });
    }

    async refresh({ query = null, quiet = false } = {}) {
        const q = query || this.els.searchInput?.value?.trim() || 'JarvisForm';
        try {
            // Light mode: keep dashboard responsive on 8GB VRAM / 16GB RAM machines.
            const response = await fetch(`/api/knowledge/map?q=${encodeURIComponent(q)}&top_k=16&graph_nodes=600&graph_edges=900`, { cache: 'no-store' });
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const data = await response.json();
            this.currentMap = data;
            const graphNodes = data.graph?.nodes || [];
            this.nodes = new Map([...graphNodes, ...(data.nodes || [])].map((node) => [node.id, node]));
            this.renderStatus(data);
            this.renderList(data.nodes || []);
            window.dispatchEvent(new CustomEvent('knowledgeMapUpdated', { detail: data }));
            if (!quiet) this.showAlert('+', 'Kunskapskartan uppdaterades.', '#4ad3f5');
        } catch (error) {
            this.renderError(error);
        }
    }

    renderStatus(data) {
        if (!this.els.statusChips) return;
        const totals = data?.totals || {};
        const health = data?.health || {};
        const chips = [
            { id: 'jarvis', label: 'Jarvis', status: health.jarvis || 'ok', value: totals.jarvis_memory ?? 0 },
            { id: 'graphify', label: 'Graphify', status: health.graphify || 'warn', value: `${totals.graphify_nodes ?? 0} noder` },
            { id: 'obsidian', label: 'Obsidian', status: health.obsidian || 'warn', value: `${totals.obsidian_matches ?? 0} träffar` },
            { id: 'visual', label: '3D-nät', status: 'ok', value: `${totals.graph_edges ?? 0} kopplingar` },
        ];
        this.updateBridgeCounters(totals, health);
        this.els.statusChips.innerHTML = chips.map((chip) => `
            <div class="bridge-chip ${statusClassForChip(chip)}" title="${escapeHtml(chip.value)}">
                ${escapeHtml(chip.label)} <span>${escapeHtml(chip.value)}</span>
            </div>
        `).join('');
    }

    updateBridgeCounters(totals, health) {
        const graphCount = Number(totals.graphify_nodes ?? 0);
        const vaultCount = Number(totals.obsidian_matches ?? 0);
        const visualCount = Number(totals.visual_nodes ?? 0);
        const graphEdges = Number(totals.graph_edges ?? 0);
        this.setTextById('memory-graph-count', `${graphCount.toLocaleString('sv-SE')} noder`);
        this.setTextById('memory-vault-count', `${vaultCount.toLocaleString('sv-SE')} träffar`);
        this.setTextById(
            'jarvis-knowledge-sync',
            `${visualCount.toLocaleString('sv-SE')} noder / ${graphEdges.toLocaleString('sv-SE')} länkar`
        );
        this.updateNeuralActivityBars(this.currentMap?.neural_activity || {});
    }

    updateNeuralActivityBars(activity) {
        const setBar = (id, value, title) => {
            const el = document.getElementById(id);
            if (!el) return;
            const pct = Math.max(0, Math.min(100, Number(value || 0) * 100));
            el.style.width = `${pct.toFixed(0)}%`;
            el.title = title;
        };

        // These bars make the left neural activity panel reflect real memory,
        // Graphify and Obsidian load instead of being only a decorative meter.
        setBar('bar-memory', activity.memory_load, `${activity.memory_nodes ?? 0} minnesnoder`);
        setBar('bar-graph', activity.synapse_load, `${activity.knowledge_synapses ?? 0} kunskapskopplingar`);
        setBar('bar-obsidian', activity.obsidian_load, `${activity.obsidian_nodes ?? 0} Obsidian-träffar`);
        window.dispatchEvent(new CustomEvent('knowledgeActivityUpdated', { detail: activity }));
    }

    setTextById(id, value) {
        const el = document.getElementById(id);
        if (el) el.textContent = value;
    }

    renderList(nodes) {
        if (!this.els.nodeList) return;
        const graphFallback = this.currentMap?.graph?.nodes || [];
        const visibleNodes = nodes.length
            ? nodes
            : graphFallback
                .slice()
                .sort((a, b) => Number(b.degree || 0) - Number(a.degree || 0))
                .slice(0, 16);
        if (!visibleNodes.length) {
            this.els.nodeList.innerHTML = '<div class="knowledge-empty">Inga noder hittades.</div>';
            return;
        }
        this.els.nodeList.innerHTML = visibleNodes.slice(0, 16).map((node) => `
            <button class="knowledge-node-row" data-node-id="${escapeHtml(node.id)}">
                <span class="knowledge-dot kind-${escapeHtml(node.kind)}"></span>
                <span class="knowledge-node-main">
                    <strong>${escapeHtml(node.label)}</strong>
                    <small>${escapeHtml(formatKindLabel(node.kind))} • ${escapeHtml(node.region || '--')}</small>
                </span>
                <span class="knowledge-score">${Math.round(Number(node.confidence || 0) * 100)}%</span>
            </button>
        `).join('');

        for (const row of this.els.nodeList.querySelectorAll('.knowledge-node-row')) {
            row.addEventListener('click', () => this.selectNode(row.dataset.nodeId));
        }
    }

    async selectNode(id, { focus3d = true } = {}) {
        const node = this.nodes.get(id);
        if (!node) return;
        this.selectedNode = node;
        this.renderInspector(node);
        if (focus3d) {
            window.dispatchEvent(new CustomEvent('knowledgeNodeFocus', { detail: { id } }));
        }
    }

    renderInspector(node) {
        if (!this.els.inspector) return;
        const confidence = Math.round(Number(node.confidence || 0) * 100);
        const freshness = Math.round(Number(node.freshness || 0) * 100);
        this.els.inspector.innerHTML = `
            <div class="knowledge-inspector-title">
                <span>${escapeHtml(formatKindLabel(node.kind))}</span>
                <strong>${escapeHtml(node.label)}</strong>
            </div>
            <p>${escapeHtml(node.summary || summarizeKnowledgeNode(node))}</p>
            <div class="knowledge-meta-grid">
                <span>Källa</span><strong>${escapeHtml(node.source || '--')}</strong>
                <span>Region</span><strong>${escapeHtml(node.region || '--')}</strong>
                <span>Säkerhet</span><strong>${confidence}%</strong>
                <span>Färskhet</span><strong>${freshness}%</strong>
                <span>Sökväg</span><strong title="${escapeHtml(node.path || '')}">${escapeHtml(node.path || '--')}</strong>
            </div>
            <div class="knowledge-action-row">
                <button class="control-btn knowledge-action" data-action="focus">PULS</button>
                ${node.kind === 'obsidian_note' && node.path ? '<button class="control-btn knowledge-action" data-action="read-note">LÄS</button>' : ''}
                ${node.kind === 'jarvis_memory' ? '<button class="control-btn knowledge-action" data-action="recall">ÅTERKALLA</button>' : ''}
            </div>
            <pre id="knowledge-note-preview" class="knowledge-note-preview"></pre>
        `;

        for (const button of this.els.inspector.querySelectorAll('.knowledge-action')) {
            button.addEventListener('click', () => this.handleAction(button.dataset.action, node));
        }
    }

    async handleAction(action, node) {
        if (action === 'focus') {
            window.jarvisKnowledgeEvent?.({ type: 'node_focus', source: node.source, target: node.region, label: node.label, status: 'ok' });
            window.dispatchEvent(new CustomEvent('knowledgeNodeFocus', { detail: { id: node.id } }));
            return;
        }

        if (action === 'recall') {
            window.jarvisKnowledgeEvent?.({ type: 'memory_recall', source: 'hippocampus', target: 'chat', label: node.label, status: 'ok' });
            this.showAlert('+', 'Minnet markerades för återkallning.', '#44ffaa');
            return;
        }

        if (action === 'read-note' && node.path) {
            try {
                window.jarvisKnowledgeEvent?.({ type: 'obsidian_read', source: 'obsidian', target: 'prefrontal', label: node.label, status: 'ok' });
                const response = await fetch(`/api/obsidian/note?path=${encodeURIComponent(node.path)}&max_chars=2200`, { cache: 'no-store' });
                const data = await response.json();
                if (!response.ok) throw new Error(data.error || `HTTP ${response.status}`);
                const preview = this.els.inspector?.querySelector('#knowledge-note-preview');
                if (preview) preview.textContent = data.content || 'Anteckningen är tom.';
            } catch (error) {
                this.showAlert('!', error?.message || 'Kunde inte läsa Obsidian-noten.', '#ff8844');
            }
        }
    }

    renderError(error) {
        const message = String(error?.message || error || '');
        const friendly = message.includes('404')
            ? 'Kunskapskartan finns inte i den NeuroLinked-server som körs just nu. Starta om Jarvis så laddas den nya serverkoden.'
            : `Kunskapskartan svarade inte: ${message}`;
        if (this.els.nodeList) {
            this.els.nodeList.innerHTML = `<div class="knowledge-empty warn">${escapeHtml(friendly)}</div>`;
        }
    }
}
