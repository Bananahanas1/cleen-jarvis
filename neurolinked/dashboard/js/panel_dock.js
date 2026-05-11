const DEFAULT_MARGIN = 15;

function clamp(value, min, max) {
    return Math.max(min, Math.min(max, value));
}

export function snapRectToViewport(rect, viewport, options = {}) {
    const margin = options.margin ?? DEFAULT_MARGIN;
    const edge = options.edge ?? 30;
    const grid = options.grid ?? 10;
    const maxLeft = Math.max(margin, viewport.width - rect.width - margin);
    const maxTop = Math.max(margin, viewport.height - rect.height - margin);

    let left = clamp(rect.left, margin, maxLeft);
    let top = clamp(rect.top, margin, maxTop);

    if (Math.abs(left - margin) <= edge) {
        left = margin;
    } else if (Math.abs(left - maxLeft) <= edge) {
        left = maxLeft;
    } else {
        left = margin + Math.round((left - margin) / grid) * grid;
    }

    if (Math.abs(top - margin) <= edge) {
        top = margin;
    } else if (Math.abs(top - maxTop) <= edge) {
        top = maxTop;
    } else {
        top = margin + Math.round((top - margin) / grid) * grid;
    }

    return {
        left: clamp(left, margin, maxLeft),
        top: clamp(top, margin, maxTop),
    };
}

const DEFAULT_PANELS = [
    ['stats-panel', '.stats-title'],
    ['regions-panel', '.stats-title'],
    ['claude-panel', '.stats-title'],
    ['jarvis-panel', '.stats-title'],
    ['safety-panel', '.stats-title'],
    ['knowledge-panel', '.knowledge-header'],
    ['jarvis-chat-panel', '#jarvis-chat-header'],
    ['region-info', '.info-title'],
];

const DEFAULT_MINIMIZE_PANELS = [
    ['stats-panel', '.stats-title'],
    ['regions-panel', '.stats-title'],
    ['claude-panel', '.stats-title'],
    ['jarvis-panel', '.stats-title'],
    ['safety-panel', '.stats-title'],
    ['region-info', '.info-title'],
];

function isInteractiveTarget(target) {
    return Boolean(target?.closest?.('button, input, textarea, select, a, [contenteditable="true"]'));
}

function readStoredPosition(id) {
    try {
        return JSON.parse(localStorage.getItem(`jarvis.panelDock.${id}`) || 'null');
    } catch {
        return null;
    }
}

function writeStoredPosition(id, position) {
    try {
        localStorage.setItem(`jarvis.panelDock.${id}`, JSON.stringify(position));
    } catch {
        // Local storage is optional; dragging should still work if it is blocked.
    }
}

function readStoredCollapsed(id) {
    try {
        return localStorage.getItem(`jarvis.panelCollapsed.${id}`) === 'true';
    } catch {
        return false;
    }
}

function writeStoredCollapsed(id, collapsed) {
    try {
        localStorage.setItem(`jarvis.panelCollapsed.${id}`, collapsed ? 'true' : 'false');
    } catch {
        // Collapsing is still useful even if localStorage is unavailable.
    }
}

export function setPanelCollapsed(panel, collapsed) {
    if (!panel || panel.id === 'jarvis-chat-panel') return false;
    panel.classList.toggle('panel-collapsed', Boolean(collapsed));
    panel.dataset.collapsed = collapsed ? 'true' : 'false';
    const button = panel.querySelector('.panel-collapse-toggle');
    if (button) {
        button.textContent = collapsed ? '+' : '-';
        button.title = collapsed ? 'Visa mer' : 'Minimera';
        button.setAttribute('aria-expanded', collapsed ? 'false' : 'true');
    }
    writeStoredCollapsed(panel.id, Boolean(collapsed));
    return true;
}

export function makePanelMinimizable(panel, { headerSelector } = {}) {
    if (!panel || panel.id === 'jarvis-chat-panel') return false;
    const header = headerSelector ? panel.querySelector(headerSelector) : panel.firstElementChild;
    if (!header) return false;

    let button = panel.querySelector('.panel-collapse-toggle');
    if (!button) {
        button = document.createElement('button');
        button.type = 'button';
        button.className = 'control-btn panel-collapse-toggle';
        button.textContent = '-';
        button.title = 'Minimera';
        button.setAttribute('aria-label', 'Minimera eller visa panelen');
        button.setAttribute('aria-expanded', 'true');
        header.appendChild(button);
    }

    if (button.dataset.boundPanelCollapse !== 'true') {
        button.dataset.boundPanelCollapse = 'true';
        button.addEventListener('click', (event) => {
            event.preventDefault();
            event.stopPropagation();
            setPanelCollapsed(panel, panel.dataset.collapsed !== 'true');
        });
    }

    panel.dataset.minimizable = 'true';
    setPanelCollapsed(panel, readStoredCollapsed(panel.id));
    return true;
}

export function makePanelDockable(panel, { handleSelector, onSnap } = {}) {
    if (!panel || panel.dataset.dockable === 'true') return false;
    const handle = handleSelector ? panel.querySelector(handleSelector) : panel;
    if (!handle) return false;

    panel.dataset.dockable = 'true';
    panel.classList.add('dockable-panel');
    handle.classList.add('panel-drag-handle');
    handle.title = handle.title || 'Dra panelen och släpp nära en kant för att snäppa fast den';

    const stored = readStoredPosition(panel.id);
    if (stored && Number.isFinite(stored.left) && Number.isFinite(stored.top)) {
        panel.style.left = `${stored.left}px`;
        panel.style.top = `${stored.top}px`;
        panel.style.right = 'auto';
        panel.style.bottom = 'auto';
        panel.style.transform = 'none';
    }

    let drag = null;
    const startDrag = (event) => {
        if (event.button !== undefined && event.button !== 0) return;
        if (isInteractiveTarget(event.target)) return;
        const rect = panel.getBoundingClientRect();
        drag = {
            pointerId: event.pointerId,
            offsetX: event.clientX - rect.left,
            offsetY: event.clientY - rect.top,
            width: rect.width,
            height: rect.height,
        };
        panel.classList.add('panel-dragging');
        panel.style.left = `${rect.left}px`;
        panel.style.top = `${rect.top}px`;
        panel.style.right = 'auto';
        panel.style.bottom = 'auto';
        panel.style.transform = 'none';
        handle.setPointerCapture?.(event.pointerId);
        event.preventDefault();
    };

    const moveDrag = (event) => {
        if (!drag) return;
        const left = event.clientX - drag.offsetX;
        const top = event.clientY - drag.offsetY;
        panel.style.left = `${left}px`;
        panel.style.top = `${top}px`;
    };

    const endDrag = (event) => {
        if (!drag) return;
        const raw = {
            left: event.clientX - drag.offsetX,
            top: event.clientY - drag.offsetY,
            width: drag.width,
            height: drag.height,
        };
        const snapped = snapRectToViewport(raw, { width: window.innerWidth, height: window.innerHeight });
        panel.classList.remove('panel-dragging');
        panel.classList.add('panel-snapping');
        panel.style.left = `${snapped.left}px`;
        panel.style.top = `${snapped.top}px`;
        writeStoredPosition(panel.id, snapped);
        window.setTimeout(() => panel.classList.remove('panel-snapping'), 240);
        handle.releasePointerCapture?.(drag.pointerId);
        drag = null;
        onSnap?.(panel.id, snapped);
    };

    handle.addEventListener('pointerdown', startDrag);
    handle.addEventListener('pointermove', moveDrag);
    handle.addEventListener('pointerup', endDrag);
    handle.addEventListener('pointercancel', endDrag);
    return true;
}

export function initPanelMinimizers({ panels = DEFAULT_MINIMIZE_PANELS, onToggle } = {}) {
    const attachAll = () => {
        for (const [id, headerSelector] of panels) {
            if (id !== 'jarvis-chat-panel') {
                const panel = document.getElementById(id);
                const changed = makePanelMinimizable(panel, { headerSelector });
                if (changed) onToggle?.(id, panel?.dataset?.collapsed === 'true');
            }
        }
    };

    attachAll();
    return { refresh: attachAll };
}

export function initPanelDock({ panels = DEFAULT_PANELS, minimizerPanels = DEFAULT_MINIMIZE_PANELS, onSnap } = {}) {
    const attachAll = () => {
        for (const [id, handleSelector] of panels) {
            const panel = document.getElementById(id);
            makePanelDockable(panel, { handleSelector, onSnap });
        }
        initPanelMinimizers({ panels: minimizerPanels }).refresh();
    };

    attachAll();
    const observer = new MutationObserver(attachAll);
    observer.observe(document.body, { childList: true, subtree: true });

    window.addEventListener('resize', () => {
        for (const [id] of panels) {
            const panel = document.getElementById(id);
            if (!panel || panel.dataset.dockable !== 'true') continue;
            const rect = panel.getBoundingClientRect();
            const snapped = snapRectToViewport(rect, { width: window.innerWidth, height: window.innerHeight });
            panel.style.left = `${snapped.left}px`;
            panel.style.top = `${snapped.top}px`;
            writeStoredPosition(id, snapped);
        }
    });

    return { refresh: attachAll, disconnect: () => observer.disconnect() };
}
