/* ============================================================
   JARVIS FX PACK — logic for Tier 1-3 visual upgrades
   Defensive: kör allt i try/catch så en feature inte kan ta
   ner resten. Pluggar in efter DOMContentLoaded.
   ============================================================ */

(function () {
  "use strict";

  const FX = {
    enabled: {
      // boot AV som default — fryser i WebView2 vid tung dashboard-init.
      // Kan slås PÅ manuellt i Inställningar → UI-effekter.
      boot:      readBool("fx_boot", false),
      ticker:    readBool("fx_ticker", true),
      coords:    readBool("fx_coords", true),
      radar:     readBool("fx_radar", true),
      // orbAudio + typeon AV som default — använder MutationObserver
      // på #messages som kan loopa oändligt om observer triggar sig själv.
      // FIXAD i v6 men kvar AV tills bekräftat stabilt.
      orbAudio:  readBool("fx_orb_audio", false),
      hexGrid:   readBool("fx_hex_grid", false),
      telemetry: readBool("fx_telemetry", true),
      sound:     readBool("fx_sound", false),
      // glitch PÅ igen — men initGlitch() exkluderar mapPanel + brainPanel
      // för att undvika FPS-drop på deras WebGL-canvas.
      glitch:    readBool("fx_glitch", true),
      particles: readBool("fx_particles", true),
      typeon:    readBool("fx_typeon", false),
      parallax:  readBool("fx_parallax", true),
    },
  };
  window.JarvisFX = FX;

  function readBool(key, def) {
    try {
      const v = localStorage.getItem(key);
      if (v === null) return def;
      return v === "1" || v === "true";
    } catch (e) { return def; }
  }
  function writeBool(key, val) {
    try { localStorage.setItem(key, val ? "1" : "0"); } catch (e) {}
  }

  // ---------- 1. BOOT SEQUENCE ----------
  // CSS-driven reveal (inga setTimeout-kedjor som kan "frysa")
  // 3 separata safety-nets för att garantera overlay tas bort
  function initBoot() {
    if (!FX.enabled.boot) return;

    if (!document.body) {
      document.addEventListener("DOMContentLoaded", initBoot, { once: true });
      return;
    }

    const lines = [
      '> INITIALIZING JARVIS CORE v2.0',
      '> LOADING NEURAL LINK....... <span class="boot-ok">OK</span>',
      '> SCANNING WORKSPACE........ <span class="boot-ok">OK</span>',
      '> BIOMETRIC AUTHENTICATION.. <span class="boot-ok">VERIFIED</span>',
      '> CONNECTING TO BRAIN....... <span class="boot-ok">ONLINE</span>',
      '> ALL SYSTEMS NOMINAL',
      '> WELCOME BACK, SIR.',
    ];

    // Bygg ALLA rader pre-render med inline animation-delay — CSS gör jobbet
    let linesHtml = "";
    for (let i = 0; i < lines.length; i++) {
      linesHtml += '<div class="boot-line visible" style="animation-delay:' + (i * 220) + 'ms;">' + lines[i] + '</div>';
    }

    const overlay = document.createElement("div");
    overlay.id = "jarvisBootOverlay";
    overlay.innerHTML =
      '<div class="boot-logo">J A R V I S</div>' +
      '<div class="boot-lines" id="bootLines">' + linesHtml + '</div>' +
      '<div class="boot-progress"></div>' +
      '<div style="margin-top:18px; font-size:10px; letter-spacing:1.5px; opacity:0.45;">CLICK ANYWHERE TO SKIP</div>';
    document.body.appendChild(overlay);

    let removed = false;
    function removeOverlay() {
      if (removed) return;
      removed = true;
      try { overlay.classList.add("fade-out"); } catch (e) {}
      // 2-stegs cleanup: fade ut + hard remove
      try { setTimeout(function () { try { overlay.remove(); } catch (e) {} }, 700); } catch (e) {
        try { overlay.remove(); } catch (e2) {}
      }
    }

    // SAFETY NET 1: setTimeout (om det fungerar)
    try { setTimeout(removeOverlay, 3000); } catch (e) {}

    // SAFETY NET 2: requestAnimationFrame-loop (backup om setTimeout är trasig)
    try {
      const start = (typeof performance !== "undefined" && performance.now) ? performance.now() : Date.now();
      const tickFn = function () {
        if (removed) return;
        const now = (typeof performance !== "undefined" && performance.now) ? performance.now() : Date.now();
        if (now - start > 3200) { removeOverlay(); return; }
        try { requestAnimationFrame(tickFn); } catch (e) {}
      };
      requestAnimationFrame(tickFn);
    } catch (e) {}

    // SAFETY NET 3: setInterval-fallback
    try {
      const start2 = Date.now();
      const ivl = setInterval(function () {
        if (removed) { clearInterval(ivl); return; }
        if (Date.now() - start2 > 3500) { removeOverlay(); clearInterval(ivl); }
      }, 400);
    } catch (e) {}

    // SKIP-handlers
    try {
      overlay.addEventListener("click", removeOverlay);
      const keyH = function (e) {
        if (e.key === "Escape" || e.key === "Enter" || e.key === " ") removeOverlay();
      };
      document.addEventListener("keydown", keyH);
    } catch (e) {}
  }

  // ---------- 2. STATUS TICKER ----------
  function initTicker() {
    if (!FX.enabled.ticker) return;
    document.body.classList.add("fx-ticker-on");
    const bar = document.createElement("div");
    bar.id = "jarvisTicker";
    bar.innerHTML = `<div class="ticker-track" id="tickerTrack"></div>`;
    document.body.appendChild(bar);
    const track = bar.querySelector("#tickerTrack");

    function buildContent() {
      const now = new Date().toISOString().replace("T", " ").slice(0, 19);
      const items = [
        `<span class="tag">[${now}]</span>`,
        `BRAIN <span class="val-ok">ONLINE</span>`,
        `GPU <span class="val-ok">${(Math.random() * 30 + 12).toFixed(0)}%</span>`,
        `MEM <span class="val-ok">${(Math.random() * 2 + 3).toFixed(1)}GB</span>`,
        `OLLAMA <span class="val-ok">phi-3</span>`,
        `NEURAL LINK <span class="val-ok">STABLE</span>`,
        `WORKSPACE <span class="val-ok">/Jarvis-clean</span>`,
        `WHISPER <span class="val-ok">READY</span>`,
        `PIPER TTS <span class="val-ok">LOADED</span>`,
        `MAP TILES <span class="val-ok">OK</span>`,
        `TOKENS <span class="val-ok">2.4M IN · 890K OUT</span>`,
        `SESSION <span class="val-ok">ACTIVE</span>`,
      ];
      // upprepa så scroll-loop känns oändlig
      const joined = items.join(`<span class="sep">·</span>`);
      track.innerHTML = joined + `<span class="sep">·</span>` + joined;
    }
    buildContent();
    setInterval(buildContent, 12000);
  }

  // ---------- 3. COORDINATE READOUTS PÅ KARTA ----------
  function initMapCoords() {
    if (!FX.enabled.coords) return;
    const mapPanel = document.getElementById("mapPanel");
    if (!mapPanel) return;

    let hud = document.getElementById("mapCoordHud");
    if (!hud) {
      hud = document.createElement("div");
      hud.id = "mapCoordHud";
      hud.innerHTML = `
        <div><span class="coord-label">LAT</span><span class="coord-val" id="coordLat">--.----° N</span></div>
        <div><span class="coord-label">LON</span><span class="coord-val" id="coordLon">--.----° E</span></div>
        <div><span class="coord-label">ZOOM</span><span class="coord-val" id="coordZoom">--.--</span></div>
        <div><span class="coord-label">BEAR</span><span class="coord-val" id="coordBearing">000°</span></div>
      `;
      mapPanel.appendChild(hud);
    }

    // Poll efter window.map (MapLibre) — finns inte alltid direkt
    let attached = false;
    const tryAttach = () => {
      const m = window.map || window.mapLibreInstance || window.kartaMap;
      if (!m || attached) return;
      attached = true;
      const update = () => {
        try {
          const c = m.getCenter();
          const z = m.getZoom();
          const b = m.getBearing ? m.getBearing() : 0;
          const latEl = document.getElementById("coordLat");
          const lonEl = document.getElementById("coordLon");
          const zEl = document.getElementById("coordZoom");
          const bEl = document.getElementById("coordBearing");
          if (latEl) latEl.textContent = `${c.lat.toFixed(4)}° ${c.lat >= 0 ? "N" : "S"}`;
          if (lonEl) lonEl.textContent = `${c.lng.toFixed(4)}° ${c.lng >= 0 ? "E" : "W"}`;
          if (zEl)   zEl.textContent   = z.toFixed(2);
          if (bEl)   bEl.textContent   = `${((b + 360) % 360).toFixed(0).padStart(3, "0")}°`;
        } catch (e) {}
      };
      try { m.on("move", update); m.on("zoom", update); m.on("rotate", update); } catch (e) {}
      update();
    };
    const poll = setInterval(() => { tryAttach(); if (attached) clearInterval(poll); }, 800);
    setTimeout(() => clearInterval(poll), 60000); // ge upp efter 60s
  }

  // ---------- 4. RADAR SWEEP ----------
  function initRadar() {
    if (!FX.enabled.radar) return;
    const mapPanel = document.getElementById("mapPanel");
    if (!mapPanel) return;
    if (document.getElementById("mapRadarSweep")) return;
    const radar = document.createElement("div");
    radar.id = "mapRadarSweep";
    radar.innerHTML = `<div class="radar-rings"></div><div class="radar-cone"></div>`;
    mapPanel.appendChild(radar);
  }

  // ---------- 5. AUDIO-REACTIVE ORB ----------
  function initOrbStates() {
    if (!FX.enabled.orbAudio) return;
    const orb = document.getElementById("sceneJarvisOrb");
    if (!orb) return;

    // Observera #messages för Jarvis-svar — sätt speaking-state under ~3s
    // SAFE: childList endast (ingen subtree) för att inte trigga onödiga events
    const messages = document.getElementById("messages");
    if (messages) {
      const obs = new MutationObserver((muts) => {
        for (const m of muts) {
          if (m.target !== messages) continue;
          for (const n of m.addedNodes) {
            if (n && n.nodeType === 1) {
              const text = (n.textContent || "").trim();
              if (text.length > 0) {
                orb.dataset.state = "speaking";
                clearTimeout(orb._fxSpeakTimer);
                orb._fxSpeakTimer = setTimeout(() => {
                  delete orb.dataset.state;
                }, Math.min(8000, 800 + text.length * 25));
              }
            }
          }
        }
      });
      obs.observe(messages, { childList: true, subtree: false });
    }

    // Expose API för manuell trigger
    window.JarvisFX.orbState = (state) => {
      if (state) orb.dataset.state = state;
      else delete orb.dataset.state;
    };
  }

  // ---------- 6. HEXAGONAL GRID ----------
  function applyHexGrid() {
    if (FX.enabled.hexGrid) document.body.classList.add("fx-hex-grid");
    else document.body.classList.remove("fx-hex-grid");
  }

  // ---------- 7. TELEMETRY MINI-GRAFER ----------
  function initTelemetry() {
    if (!FX.enabled.telemetry) return;
    const corners = [
      { id: "fxTmTL", cls: "tl", label: "CPU", color: "#6ad9ff" },
      { id: "fxTmTR", cls: "tr", label: "MEM", color: "#aee6ff" },
      { id: "fxTmBL", cls: "bl", label: "NET", color: "#7dffbd" },
      { id: "fxTmBR", cls: "br", label: "GPU", color: "#ffd166" },
    ];
    const charts = [];
    for (const c of corners) {
      if (document.getElementById(c.id)) continue;
      const wrap = document.createElement("div");
      wrap.id = c.id;
      wrap.className = `fx-telemetry-corner ${c.cls}`;
      wrap.innerHTML = `
        <div class="tm-label"><span>${c.label}</span><span class="tm-val" data-tm-val>--</span></div>
        <canvas width="84" height="22"></canvas>
      `;
      document.body.appendChild(wrap);
      const canvas = wrap.querySelector("canvas");
      const ctx = canvas.getContext("2d");
      const data = new Array(40).fill(0).map(() => Math.random() * 0.5 + 0.25);
      charts.push({ ctx, data, valEl: wrap.querySelector("[data-tm-val]"), label: c.label, color: c.color });
    }
    function draw() {
      for (const ch of charts) {
        let v;
        if (ch.label === "CPU") v = 0.15 + Math.random() * 0.30;
        else if (ch.label === "MEM") v = 0.35 + Math.random() * 0.10;
        else if (ch.label === "GPU") v = 0.10 + Math.random() * 0.25;
        else v = 0.05 + Math.random() * 0.40;
        ch.data.shift();
        ch.data.push(v);
        ch.valEl.textContent = `${Math.round(v * 100)}%`;

        const { ctx } = ch;
        const w = ctx.canvas.width, h = ctx.canvas.height;
        ctx.clearRect(0, 0, w, h);
        ctx.beginPath();
        ctx.strokeStyle = ch.color;
        ctx.lineWidth = 1.2;
        ctx.shadowColor = ch.color;
        ctx.shadowBlur = 4;
        const n = ch.data.length;
        for (let i = 0; i < n; i++) {
          const x = (i / (n - 1)) * w;
          const y = h - ch.data[i] * h * 0.9 - 1;
          if (i === 0) ctx.moveTo(x, y); else ctx.lineTo(x, y);
        }
        ctx.stroke();
        // Fill under line
        ctx.lineTo(w, h);
        ctx.lineTo(0, h);
        ctx.closePath();
        ctx.globalAlpha = 0.18;
        ctx.fillStyle = ch.color;
        ctx.fill();
        ctx.globalAlpha = 1;
      }
    }
    setInterval(draw, 800);
    draw();
  }

  // ---------- 8. SOUND DESIGN (Web Audio UI ticks) ----------
  let audioCtx = null;
  function ensureAudioCtx() {
    if (audioCtx) return audioCtx;
    try {
      const AC = window.AudioContext || window.webkitAudioContext;
      if (!AC) return null;
      audioCtx = new AC();
    } catch (e) { return null; }
    return audioCtx;
  }
  function uiBlip(opts) {
    if (!FX.enabled.sound) return;
    const ac = ensureAudioCtx();
    if (!ac) return;
    const o = ac.createOscillator();
    const g = ac.createGain();
    o.type = opts && opts.type ? opts.type : "sine";
    o.frequency.value = opts && opts.freq ? opts.freq : 880;
    g.gain.value = 0;
    o.connect(g).connect(ac.destination);
    const now = ac.currentTime;
    g.gain.setValueAtTime(0, now);
    g.gain.linearRampToValueAtTime(0.06, now + 0.005);
    g.gain.exponentialRampToValueAtTime(0.0001, now + 0.14);
    o.start(now);
    o.stop(now + 0.16);
  }
  function initSound() {
    document.addEventListener("click", (e) => {
      const btn = e.target.closest("button.panel-button, button#send, button.monitor-action");
      if (!btn) return;
      uiBlip({ freq: 920, type: "triangle" });
    }, { capture: true });
  }
  window.JarvisFX.blip = uiBlip;

  // ---------- 9. GLITCH TRANSITION VID MODE-BYTE ----------
  function initGlitch() {
    if (!FX.enabled.glitch) return;
    // mapPanel + brainPanel exkluderade: filter+clip-path på deras stora
    // WebGL/Maps-canvas ger FPS-drop varje klick. Glitch funkar bra på
    // resterande paneler som är vanlig HTML.
    const targets = ["scenePanel", "settingsPanel", "visualPanel"];
    const buttons = document.querySelectorAll(".panel-button");
    buttons.forEach((btn) => {
      btn.addEventListener("click", () => {
        // Glitcha alla synliga targets ut
        for (const id of targets) {
          const el = document.getElementById(id);
          if (!el) continue;
          if (el.style.display !== "none" && getComputedStyle(el).display !== "none") {
            el.classList.add("fx-glitch-out");
            setTimeout(() => el.classList.remove("fx-glitch-out"), 240);
          }
        }
        // Efter att panel bytt — kör glitch-in på nytt visible target
        setTimeout(() => {
          for (const id of targets) {
            const el = document.getElementById(id);
            if (!el) continue;
            if (el.style.display !== "none" && getComputedStyle(el).display !== "none") {
              el.classList.add("fx-glitch-in");
              setTimeout(() => el.classList.remove("fx-glitch-in"), 340);
            }
          }
        }, 260);
      }, { passive: true });
    });
  }

  // ---------- 10. PARTICLE FIELD RUNT ORBEN ----------
  function initParticles() {
    if (!FX.enabled.particles) return;
    const panel = document.querySelector(".scene-orb-panel");
    if (!panel) return;
    if (panel.querySelector("#orbParticleCanvas")) return;

    const canvas = document.createElement("canvas");
    canvas.id = "orbParticleCanvas";
    panel.appendChild(canvas);
    const ctx = canvas.getContext("2d");

    function resize() {
      const r = panel.getBoundingClientRect();
      canvas.width = Math.max(200, r.width);
      canvas.height = Math.max(200, r.height);
    }
    resize();
    new ResizeObserver(resize).observe(panel);

    const N = 28;
    const particles = [];
    for (let i = 0; i < N; i++) {
      particles.push({
        angle: Math.random() * Math.PI * 2,
        radius: 90 + Math.random() * 110,
        speed: 0.0006 + Math.random() * 0.0022,
        size: 0.6 + Math.random() * 1.6,
        opacity: 0.25 + Math.random() * 0.5,
        wobble: Math.random() * 10,
      });
    }
    function frame() {
      const w = canvas.width, h = canvas.height;
      ctx.clearRect(0, 0, w, h);
      const cx = w / 2, cy = h / 2;
      for (const p of particles) {
        p.angle += p.speed;
        const r = p.radius + Math.sin(performance.now() * 0.001 + p.wobble) * 6;
        const x = cx + Math.cos(p.angle) * r;
        const y = cy + Math.sin(p.angle) * r * 0.92;
        ctx.beginPath();
        ctx.arc(x, y, p.size, 0, Math.PI * 2);
        ctx.fillStyle = `rgba(106, 217, 255, ${p.opacity})`;
        ctx.shadowColor = "rgba(106, 217, 255, 0.85)";
        ctx.shadowBlur = 6;
        ctx.fill();
      }
      requestAnimationFrame(frame);
    }
    requestAnimationFrame(frame);
  }

  // ---------- 13. TYPE-ON REVEAL ----------
  // SAFE-MODE: childList ONLY (no subtree), skip already-processed nodes,
  // skip nodes we created ourselves. Tidigare version loopade oändligt.
  function initTypeOn() {
    if (!FX.enabled.typeon) return;
    const messages = document.getElementById("messages");
    if (!messages) return;

    function reveal(node) {
      if (!node || node.dataset && node.dataset.fxTypeonDone === "1") return;
      // Skippa noder vi skapat själva
      if (node.classList && (node.classList.contains("fx-char") || node.classList.contains("fx-typeon"))) return;
      const text = (node.textContent || "").trim();
      if (!text || text.length > 600) return;
      // Endast text-bärande leaf-noder (inga komplexa barn)
      if (node.children && node.children.length > 0) return;
      // Markera FÖRE mutation så observer-event på våra spans skippas
      if (node.dataset) node.dataset.fxTypeonDone = "1";
      node.classList.add("fx-typeon");
      const original = text;
      node.textContent = "";
      const frag = document.createDocumentFragment();
      const max = Math.min(original.length, 400);
      for (let i = 0; i < max; i++) {
        const s = document.createElement("span");
        s.className = "fx-char";
        s.textContent = original[i];
        s.style.animationDelay = (i * 12) + "ms";
        frag.appendChild(s);
      }
      node.appendChild(frag);
    }

    const obs = new MutationObserver((muts) => {
      for (const m of muts) {
        // ENDAST direkta barn till #messages — INGEN subtree-rekursion
        if (m.target !== messages) continue;
        for (const n of m.addedNodes) {
          if (!n || n.nodeType !== 1) continue;
          // Skippa user-meddelanden
          const cls = n.className || "";
          if (/user|right|sent|outgoing/i.test(cls)) continue;
          // Hitta lämplig textbärare i toppen av subtree (ej rekursivt)
          reveal(n);
        }
      }
    });
    // VIKTIGT: childList endast, INGEN subtree
    obs.observe(messages, { childList: true, subtree: false });
  }

  // ---------- 15. PARALLAX ORB ----------
  function initParallax() {
    if (!FX.enabled.parallax) return;
    const panel = document.querySelector(".scene-orb-panel");
    if (!panel) return;
    let active = false;
    panel.addEventListener("mouseenter", () => {
      active = true;
      panel.classList.add("fx-parallax-active");
    });
    panel.addEventListener("mouseleave", () => {
      active = false;
      panel.classList.remove("fx-parallax-active");
      panel.style.setProperty("--px", "0");
      panel.style.setProperty("--py", "0");
    });
    panel.addEventListener("mousemove", (e) => {
      if (!active) return;
      const r = panel.getBoundingClientRect();
      const x = ((e.clientX - r.left) / r.width - 0.5) * 18;
      const y = ((e.clientY - r.top) / r.height - 0.5) * 18;
      panel.style.setProperty("--px", x.toFixed(1));
      panel.style.setProperty("--py", y.toFixed(1));
    });
  }

  // ---------- CURVED HUD-FRAMES (apply class) ----------
  function applyCurvedFrames() {
    const targets = ["scenePanel", "mapPanel", "brainPanel"];
    for (const id of targets) {
      const el = document.getElementById(id);
      if (el && el.classList.contains("jarvis-hud-frame")) {
        el.classList.add("curved");
      }
    }
  }

  // ---------- WAVE DIVIDERS i chat ----------
  function applyWaveDividers() {
    // Lägg en wave-divider under workspaceTitle som dekorativ separator
    const top = document.querySelector(".editor .top-row");
    if (top && !document.getElementById("fxWaveDividerMain")) {
      const div = document.createElement("div");
      div.id = "fxWaveDividerMain";
      div.className = "fx-wave-divider";
      top.parentNode.insertBefore(div, top.nextSibling);
    }
  }

  // ---------- SETTINGS-UI ----------
  function injectSettings() {
    const settingsPanel = document.getElementById("settingsPanel");
    if (!settingsPanel) return;
    const container = settingsPanel.querySelector("div[style*='max-width:780px']") || settingsPanel.firstElementChild;
    if (!container) return;
    if (container.querySelector("#fxSettingsCard")) return;

    const card = document.createElement("section");
    card.id = "fxSettingsCard";
    card.className = "settings-card fx-settings-card";
    card.style.cssText = "background:#0a1622; border:1px solid #1f4e68; border-radius:8px; padding:14px 16px;";
    card.innerHTML = `
      <h4 style="margin:0 0 6px 0; color:#aee6ff;">UI-effekter (FX Pack)</h4>
      <div style="opacity:0.7; font-size:12px; margin-bottom:10px;">Visuella effekter. Sparas lokalt. Ladda om för att tillämpa alla.</div>
      <div style="display:grid; grid-template-columns:1fr 1fr; gap:4px 16px;">
        ${fxToggle("fx_boot",      "Boot-sekvens")}
        ${fxToggle("fx_ticker",    "Status ticker (botten)")}
        ${fxToggle("fx_coords",    "Kartkoordinater")}
        ${fxToggle("fx_radar",     "Radar-sweep")}
        ${fxToggle("fx_orb_audio", "Audio-reactive orb")}
        ${fxToggle("fx_hex_grid",  "Hexagonal grid")}
        ${fxToggle("fx_telemetry", "Telemetri-hörn")}
        ${fxToggle("fx_sound",     "UI-ljud (blip)")}
        ${fxToggle("fx_glitch",    "Glitch-transition")}
        ${fxToggle("fx_particles", "Orb-partiklar")}
        ${fxToggle("fx_typeon",    "Type-on chat")}
        ${fxToggle("fx_parallax",  "Parallax orb")}
      </div>
    `;
    container.appendChild(card);

    card.querySelectorAll("input[type='checkbox'][data-fx-key]").forEach((cb) => {
      cb.addEventListener("change", () => {
        writeBool(cb.dataset.fxKey, cb.checked);
        // Hot-apply där möjligt
        if (cb.dataset.fxKey === "fx_hex_grid") {
          FX.enabled.hexGrid = cb.checked;
          applyHexGrid();
        }
        if (cb.dataset.fxKey === "fx_sound") FX.enabled.sound = cb.checked;
      });
    });
  }
  function fxToggle(key, label) {
    const offByDefault = (
      key === "fx_sound" ||
      key === "fx_hex_grid" ||
      key === "fx_boot" ||
      key === "fx_typeon" ||
      key === "fx_orb_audio"
    );
    const checked = readBool(key, !offByDefault);
    return `<label><input type="checkbox" data-fx-key="${key}" ${checked ? "checked" : ""}><span>${label}</span></label>`;
  }

  // ---------- 16. HUD CORNER BRACKETS ----------
  // L-formade accenter i alla 4 hörn av viewport. Pulserar svagt.
  function initCornerBrackets() {
    if (document.getElementById("jarvisCornerBrackets")) return;
    const wrap = document.createElement("div");
    wrap.id = "jarvisCornerBrackets";
    wrap.setAttribute("aria-hidden", "true");
    wrap.innerHTML =
      '<div class="jc-bracket jc-tl"></div>' +
      '<div class="jc-bracket jc-tr"></div>' +
      '<div class="jc-bracket jc-bl"></div>' +
      '<div class="jc-bracket jc-br"></div>';
    document.body.appendChild(wrap);
  }

  // ---------- 17. CLICK RIPPLE RING ----------
  // Radial cyan ring som expanderar från klickpunkten på primära kontroller.
  function initClickRipple() {
    if (document._jarvisRippleBound) return;
    document._jarvisRippleBound = true;
    const SELECTOR = ".monitor-action, .karta-btn, .panel-button, .panel-toggle-btn, .brain-mini-btn, .scene-tool-icon, #showTerminalBtn, #copyEditorBtn";
    document.addEventListener("click", function (e) {
      const tgt = e.target.closest(SELECTOR);
      if (!tgt) return;
      const rect = tgt.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const y = e.clientY - rect.top;
      const ring = document.createElement("span");
      ring.className = "jarvis-click-ring";
      ring.style.left = x + "px";
      ring.style.top  = y + "px";
      // Säkerställ att target kan hosta ringen
      const prevPos = getComputedStyle(tgt).position;
      if (prevPos === "static") tgt.style.position = "relative";
      tgt.appendChild(ring);
      setTimeout(function () { try { ring.remove(); } catch (_) {} }, 700);
    }, true);
  }

  // ---------- 18. MISSION TIME CLOCK ----------
  // Top-right live klocka: UTC + lokal tid + Mission Day-counter sedan första start.
  function initMissionClock() {
    if (document.getElementById("jarvisMissionClock")) return;
    const el = document.createElement("div");
    el.id = "jarvisMissionClock";
    el.setAttribute("aria-hidden", "true");
    el.innerHTML =
      '<div class="jmc-row jmc-utc"><span class="jmc-label">UTC</span><span class="jmc-val" id="jmcUtc">--:--:--</span></div>' +
      '<div class="jmc-row jmc-local"><span class="jmc-label">LOC</span><span class="jmc-val" id="jmcLocal">--:--:--</span></div>' +
      '<div class="jmc-row jmc-mday"><span class="jmc-label">DAY</span><span class="jmc-val" id="jmcDay">T+000</span></div>';
    document.body.appendChild(el);

    // Mission start = första gång appen körs (persisteras i localStorage)
    let started;
    try {
      started = parseInt(localStorage.getItem("jarvis_mission_start") || "0", 10);
      if (!started) {
        started = Date.now();
        localStorage.setItem("jarvis_mission_start", String(started));
      }
    } catch (_) { started = Date.now(); }

    function pad(n) { return n < 10 ? "0" + n : "" + n; }
    function tick() {
      const now = new Date();
      const utc = pad(now.getUTCHours()) + ":" + pad(now.getUTCMinutes()) + ":" + pad(now.getUTCSeconds());
      const loc = pad(now.getHours()) + ":" + pad(now.getMinutes()) + ":" + pad(now.getSeconds());
      const days = Math.floor((Date.now() - started) / 86400000);
      const utcEl = document.getElementById("jmcUtc");
      const locEl = document.getElementById("jmcLocal");
      const dayEl = document.getElementById("jmcDay");
      if (utcEl) utcEl.textContent = utc;
      if (locEl) locEl.textContent = loc;
      if (dayEl) dayEl.textContent = "T+" + pad(days);
    }
    tick();
    setInterval(tick, 1000);
  }

  // ---------- BOOTSTRAP ----------
  function boot() {
    try { initBoot(); }       catch (e) { console.warn("FX boot:", e); }
    try { initTicker(); }     catch (e) { console.warn("FX ticker:", e); }
    try { initMapCoords(); }  catch (e) { console.warn("FX coords:", e); }
    try { initRadar(); }      catch (e) { console.warn("FX radar:", e); }
    try { initOrbStates(); }  catch (e) { console.warn("FX orbStates:", e); }
    try { applyHexGrid(); }   catch (e) { console.warn("FX hex:", e); }
    try { initTelemetry(); }  catch (e) { console.warn("FX telemetry:", e); }
    try { initSound(); }      catch (e) { console.warn("FX sound:", e); }
    try { initGlitch(); }     catch (e) { console.warn("FX glitch:", e); }
    try { initParticles(); }  catch (e) { console.warn("FX particles:", e); }
    try { initTypeOn(); }     catch (e) { console.warn("FX typeon:", e); }
    try { initParallax(); }   catch (e) { console.warn("FX parallax:", e); }
    try { applyCurvedFrames(); } catch (e) { console.warn("FX curved:", e); }
    try { applyWaveDividers(); } catch (e) { console.warn("FX wave:", e); }
    try { injectSettings(); } catch (e) { console.warn("FX settings:", e); }
    try { initCornerBrackets(); } catch (e) { console.warn("FX corners:", e); }
    try { initClickRipple(); }    catch (e) { console.warn("FX ripple:", e); }
    try { initMissionClock(); }   catch (e) { console.warn("FX clock:", e); }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }
})();
