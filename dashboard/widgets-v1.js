/* widgets-v1.js — Sprint 3 (2026-05-17): JarvisWidgetsV1 namespace.
 *
 * Skapar floating, draggable, resizable widgets över hela Jarvis-UI.
 * Position + storlek persisteras per widget-typ i localStorage.
 *
 * API:
 *   window.JarvisWidgetsV1.create(type, options) -> widgetId
 *   window.JarvisWidgetsV1.update(id, options)
 *   window.JarvisWidgetsV1.close(id)
 *   window.JarvisWidgetsV1.list() -> [{id, type, title}]
 *
 * Stödda typer: image, iframe, webcam, video, text, chat-mini, html
 */
(function () {
  "use strict";

  var LS_PREFIX = "jarvis_widget_v1_";
  var nextId = 1;
  var widgets = new Map(); // id -> { el, type, options }

  // Widget V2 — Snap-grid model (12 cols x 8 rows)
  var GRID_COLS = 12;
  var GRID_ROWS = 8;
  var GRID_PAD = 10;
  var GRID_INSET = 16;

  function getContainerRect(scoped) {
    if (scoped) {
      var scene = document.getElementById("scenePanel");
      if (scene) return scene.getBoundingClientRect();
    }
    return { left: 0, top: 0, width: window.innerWidth, height: window.innerHeight };
  }

  function gridDims(scoped) {
    var rect = getContainerRect(scoped);
    var usableW = Math.max(rect.width - GRID_INSET * 2, 200);
    var usableH = Math.max(rect.height - GRID_INSET * 2, 200);
    var cellW = (usableW - GRID_PAD * (GRID_COLS - 1)) / GRID_COLS;
    var cellH = (usableH - GRID_PAD * (GRID_ROWS - 1)) / GRID_ROWS;
    return { rect: rect, cellW: cellW, cellH: cellH };
  }

  function pixelsToGridCell(left, top, scoped) {
    var d = gridDims(scoped);
    var relLeft = left - d.rect.left - GRID_INSET;
    var relTop = top - d.rect.top - GRID_INSET;
    var col = Math.round(relLeft / (d.cellW + GRID_PAD));
    var row = Math.round(relTop / (d.cellH + GRID_PAD));
    col = Math.max(0, Math.min(GRID_COLS - 1, col));
    row = Math.max(0, Math.min(GRID_ROWS - 1, row));
    return { col: col, row: row };
  }

  function gridCellToPixels(col, row, scoped) {
    var d = gridDims(scoped);
    return {
      left: d.rect.left + GRID_INSET + col * (d.cellW + GRID_PAD),
      top: d.rect.top + GRID_INSET + row * (d.cellH + GRID_PAD)
    };
  }

  function snapToGrid(left, top, width, height, scoped) {
    var d = gridDims(scoped);
    var cell = pixelsToGridCell(left, top, scoped);
    var px = gridCellToPixels(cell.col, cell.row, scoped);
    var spanW = Math.max(1, Math.min(GRID_COLS - cell.col,
      Math.round(width / (d.cellW + GRID_PAD))));
    var spanH = Math.max(1, Math.min(GRID_ROWS - cell.row,
      Math.round(height / (d.cellH + GRID_PAD))));
    var snapW = spanW * d.cellW + (spanW - 1) * GRID_PAD;
    var snapH = spanH * d.cellH + (spanH - 1) * GRID_PAD;
    return {
      left: px.left, top: px.top, width: snapW, height: snapH,
      gridX: cell.col, gridY: cell.row, gridW: spanW, gridH: spanH
    };
  }

  // Grid-overlay (visas under drag/resize)
  function ensureGridOverlay() {
    var el = document.getElementById("widgetGridOverlay");
    if (el) return el;
    el = document.createElement("div");
    el.id = "widgetGridOverlay";
    el.className = "widget-grid-overlay";
    document.body.appendChild(el);
    return el;
  }
  function showGridOverlay() {
    var scoped = false;
    widgets.forEach(function (w) {
      if (w.el.dataset.scope === "scene" &&
          (w.el.classList.contains("is-dragging") || w.el.classList.contains("is-resizing"))) {
        scoped = true;
      }
    });
    var d = gridDims(scoped);
    var overlay = ensureGridOverlay();
    overlay.style.left = (d.rect.left + GRID_INSET) + "px";
    overlay.style.top = (d.rect.top + GRID_INSET) + "px";
    overlay.style.width = (d.rect.width - GRID_INSET * 2) + "px";
    overlay.style.height = (d.rect.height - GRID_INSET * 2) + "px";
    overlay.style.setProperty("--grid-cell-w", (d.cellW + GRID_PAD) + "px");
    overlay.style.setProperty("--grid-cell-h", (d.cellH + GRID_PAD) + "px");
    overlay.classList.add("visible");
  }
  function hideGridOverlay() {
    var overlay = document.getElementById("widgetGridOverlay");
    if (overlay) overlay.classList.remove("visible");
  }

  function loadGeom(type) {
    try {
      var raw = localStorage.getItem(LS_PREFIX + type);
      if (!raw) return null;
      return JSON.parse(raw);
    } catch (e) { return null; }
  }
  function saveGeom(type, geom) {
    try { localStorage.setItem(LS_PREFIX + type, JSON.stringify(geom)); }
    catch (e) {}
  }

  function el(tag, cls, attrs) {
    var e = document.createElement(tag);
    if (cls) e.className = cls;
    if (attrs) for (var k in attrs) {
      if (k === "html") e.innerHTML = attrs[k];
      else if (k === "text") e.textContent = attrs[k];
      else e.setAttribute(k, attrs[k]);
    }
    return e;
  }

  function defaultGeom(idx) {
    return {
      left: 24 + idx * 28,
      top: 60 + idx * 28,
      width: 340,
      height: 240
    };
  }

  /* Returnerar dar widgets ska appendas: scen-panelen om aktiv (cinematic workspace),
     annars body som fallback. Position blir absolute mot scen, fixed mot body. */
  function pickContainer() {
    var scene = document.getElementById("scenePanel");
    if (scene && scene.offsetParent !== null && getComputedStyle(scene).display !== "none") {
      return { el: scene, scoped: true };
    }
    return { el: document.body, scoped: false };
  }

  function focusWidget(id) {
    widgets.forEach(function (w) { w.el.classList.remove("is-focused"); });
    var w = widgets.get(id);
    if (w) w.el.classList.add("is-focused");
  }

  function makeDraggable(widget, handle) {
    var dragging = false;
    var startX = 0, startY = 0, startLeft = 0, startTop = 0;
    handle.addEventListener("mousedown", function (e) {
      if (e.target.tagName === "BUTTON") return;
      dragging = true;
      startX = e.clientX; startY = e.clientY;
      var rect = widget.el.getBoundingClientRect();
      startLeft = rect.left; startTop = rect.top;
      widget.el.classList.add("is-dragging");
      showGridOverlay();
      focusWidget(widget.id);
      e.preventDefault();
    });
    window.addEventListener("mousemove", function (e) {
      if (!dragging) return;
      var dx = e.clientX - startX, dy = e.clientY - startY;
      var newLeft = Math.max(0, Math.min(window.innerWidth - 60, startLeft + dx));
      var newTop = Math.max(0, Math.min(window.innerHeight - 40, startTop + dy));
      widget.el.style.left = newLeft + "px";
      widget.el.style.top = newTop + "px";
    });
    window.addEventListener("mouseup", function () {
      if (!dragging) return;
      dragging = false;
      widget.el.classList.remove("is-dragging");
      hideGridOverlay();
      var scoped = widget.el.dataset.scope === "scene";
      var rect = widget.el.getBoundingClientRect();
      var snap = snapToGrid(rect.left, rect.top, rect.width, rect.height, scoped);
      var localLeft = scoped ? (snap.left - getContainerRect(true).left) : snap.left;
      var localTop = scoped ? (snap.top - getContainerRect(true).top) : snap.top;
      widget.el.style.left = localLeft + "px";
      widget.el.style.top = localTop + "px";
      widget.el.style.width = snap.width + "px";
      widget.el.style.height = snap.height + "px";
      widget.gridX = snap.gridX; widget.gridY = snap.gridY;
      widget.gridW = snap.gridW; widget.gridH = snap.gridH;
      saveGeom(widget.type + "_" + (widget.el.dataset.scope || "global"), {
        left: localLeft, top: localTop, width: snap.width, height: snap.height,
        gridX: snap.gridX, gridY: snap.gridY, gridW: snap.gridW, gridH: snap.gridH
      });
    });
  }

  function makeResizable(widget, handle) {
    var resizing = false;
    var startX = 0, startY = 0, startW = 0, startH = 0;
    handle.addEventListener("mousedown", function (e) {
      resizing = true;
      startX = e.clientX; startY = e.clientY;
      var rect = widget.el.getBoundingClientRect();
      startW = rect.width; startH = rect.height;
      widget.el.classList.add("is-resizing");
      showGridOverlay();
      focusWidget(widget.id);
      e.preventDefault();
      e.stopPropagation();
    });
    window.addEventListener("mousemove", function (e) {
      if (!resizing) return;
      var newW = Math.max(220, startW + (e.clientX - startX));
      var newH = Math.max(120, startH + (e.clientY - startY));
      widget.el.style.width = newW + "px";
      widget.el.style.height = newH + "px";
    });
    window.addEventListener("mouseup", function () {
      if (!resizing) return;
      resizing = false;
      widget.el.classList.remove("is-resizing");
      hideGridOverlay();
      var scoped = widget.el.dataset.scope === "scene";
      var rect = widget.el.getBoundingClientRect();
      var snap = snapToGrid(rect.left, rect.top, rect.width, rect.height, scoped);
      var localLeft = scoped ? (snap.left - getContainerRect(true).left) : snap.left;
      var localTop = scoped ? (snap.top - getContainerRect(true).top) : snap.top;
      widget.el.style.left = localLeft + "px";
      widget.el.style.top = localTop + "px";
      widget.el.style.width = snap.width + "px";
      widget.el.style.height = snap.height + "px";
      widget.gridX = snap.gridX; widget.gridY = snap.gridY;
      widget.gridW = snap.gridW; widget.gridH = snap.gridH;
      saveGeom(widget.type + "_" + (widget.el.dataset.scope || "global"), {
        left: localLeft, top: localTop, width: snap.width, height: snap.height,
        gridX: snap.gridX, gridY: snap.gridY, gridW: snap.gridW, gridH: snap.gridH
      });
    });
  }

  function renderBody(widget, options) {
    var body = widget.el.querySelector(".jarvis-widget-body");
    body.innerHTML = "";
    body.className = "jarvis-widget-body";
    body.removeAttribute("data-content-mode");

    var type = widget.type;
    if (type === "image" && options.url) {
      body.setAttribute("data-content-mode", "image");
      var img = el("img", null, { src: options.url, alt: options.caption || "" });
      img.onerror = function () { body.textContent = "Kunde inte ladda bild."; body.classList.add("text-mode"); };
      body.appendChild(img);
    } else if (type === "iframe" && options.url) {
      body.setAttribute("data-content-mode", "iframe");
      var iframe = el("iframe", null, {
        src: options.url,
        allow: "autoplay; encrypted-media; clipboard-write",
        loading: "lazy"
      });
      body.appendChild(iframe);
    } else if (type === "webcam") {
      body.setAttribute("data-content-mode", "webcam");
      var video = el("video", null, { autoplay: "true", muted: "true", playsinline: "true" });
      body.appendChild(video);
      if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
        navigator.mediaDevices.getUserMedia({ video: true, audio: false })
          .then(function (stream) {
            video.srcObject = stream;
            widget._stream = stream;
          })
          .catch(function (err) {
            body.removeAttribute("data-content-mode");
            body.classList.add("text-mode");
            body.textContent = "Kunde inte komma åt kamera: " + err.message;
          });
      } else {
        body.classList.add("text-mode");
        body.textContent = "getUserMedia stöds inte i denna miljö.";
      }
    } else if (type === "video" && options.url) {
      body.setAttribute("data-content-mode", "video");
      var v = el("video", null, { src: options.url, controls: "true", autoplay: "true" });
      body.appendChild(v);
    } else if (type === "chat-mini") {
      body.classList.add("text-mode");
      renderChatMini(body);
      // Re-render var 3:e sek så widgeten alltid speglar senaste konversation.
      widget._refreshTimer = setInterval(function () { renderChatMini(body); }, 3000);
    } else if (type === "text" || type === "html") {
      body.classList.add("text-mode");
      if (type === "html") body.innerHTML = String(options.content || "");
      else body.textContent = String(options.content || "");
    } else {
      body.classList.add("text-mode");
      body.textContent = "Tom widget (" + type + ")";
    }
  }

  function renderChatMini(body) {
    var messages = document.getElementById("messages");
    if (!messages) { body.textContent = "Hittade inte chat-message-list."; return; }
    var lines = messages.querySelectorAll(".message, .msg, [data-role]");
    if (lines.length === 0) {
      var fallback = messages.children;
      lines = fallback.length ? fallback : [];
    }
    body.innerHTML = "";
    var arr = Array.prototype.slice.call(lines);
    var last = arr.slice(-5);
    for (var i = 0; i < last.length; i++) {
      var src = last[i];
      var role = src.getAttribute("data-role") ||
                 (src.className && src.className.indexOf("user") >= 0 ? "user" : "assistant");
      var txt = (src.textContent || "").trim();
      if (!txt) continue;
      var line = el("div", "jarvis-widget-chat-line", null);
      line.setAttribute("data-role", role);
      var tag = el("span", "role-tag", { text: role === "user" ? "Du" : "Jarvis" });
      line.appendChild(tag);
      line.appendChild(document.createTextNode(txt.length > 240 ? txt.substring(0, 237) + "…" : txt));
      body.appendChild(line);
    }
    if (body.children.length === 0) {
      body.textContent = "Inga meddelanden än.";
    } else {
      body.scrollTop = body.scrollHeight;
    }
  }

  function titleFor(type, options) {
    if (options && options.title) return options.title;
    var map = {
      "image": "BILD",
      "iframe": "WEBB",
      "webcam": "KAMERA",
      "video": "VIDEO",
      "chat-mini": "JARVIS CHAT",
      "text": "INFO",
      "html": "HTML"
    };
    return map[type] || type.toUpperCase();
  }

  function createWidget(type, options) {
    options = options || {};
    var id = "w" + (nextId++);
    var wrap = el("div", "jarvis-widget", { "data-type": type });
    wrap.id = "jarvis-widget-" + id;

    var header = el("div", "jarvis-widget-header");
    var title = el("div", "jarvis-widget-title", { text: titleFor(type, options) });
    var controls = el("div", "jarvis-widget-controls");

    var btnMin = el("button", "widget-btn-min", { text: "_", title: "Minimera" });
    btnMin.addEventListener("click", function (e) {
      e.stopPropagation();
      wrap.classList.toggle("is-minimized");
    });

    var btnClose = el("button", "widget-btn-close", { text: "×", title: "Stäng" });
    btnClose.addEventListener("click", function (e) {
      e.stopPropagation();
      closeWidget(id);
    });

    controls.appendChild(btnMin);
    controls.appendChild(btnClose);
    header.appendChild(title);
    header.appendChild(controls);

    var body = el("div", "jarvis-widget-body");
    var resize = el("div", "jarvis-widget-resize");

    wrap.appendChild(header);
    wrap.appendChild(body);
    wrap.appendChild(resize);

    var ctx = pickContainer();
    wrap.style.position = ctx.scoped ? "absolute" : "fixed";
    wrap.dataset.scope = ctx.scoped ? "scene" : "global";

    // Initial-geom har högre prio än sparad — används av composeScene för auto-layout.
    var initialGeom = options && options._initialGeom;
    var saved = initialGeom ? null : loadGeom(type + (ctx.scoped ? "_scene" : "_global"));
    var geom = initialGeom || saved || defaultGeom(widgets.size);
    wrap.style.left = geom.left + "px";
    wrap.style.top = geom.top + "px";
    wrap.style.width = geom.width + "px";
    wrap.style.height = geom.height + "px";

    ctx.el.appendChild(wrap);
    var w = { id: id, type: type, options: options, el: wrap };
    widgets.set(id, w);

    makeDraggable(w, header);
    makeResizable(w, resize);
    wrap.addEventListener("mousedown", function () { focusWidget(id); });

    renderBody(w, options);
    focusWidget(id);
    return id;
  }

  function updateWidget(id, options) {
    var w = widgets.get(id);
    if (!w) return false;
    if (w._refreshTimer) { clearInterval(w._refreshTimer); w._refreshTimer = null; }
    w.options = Object.assign({}, w.options, options || {});
    if (options && options.title) {
      var t = w.el.querySelector(".jarvis-widget-title");
      if (t) t.textContent = options.title;
    }
    renderBody(w, w.options);
    return true;
  }

  function closeWidget(id) {
    var w = widgets.get(id);
    if (!w) return false;
    if (w._refreshTimer) { clearInterval(w._refreshTimer); w._refreshTimer = null; }
    if (w._stream) {
      try { w._stream.getTracks().forEach(function (t) { t.stop(); }); } catch (e) {}
    }
    try { w.el.remove(); } catch (e) {}
    widgets.delete(id);
    return true;
  }

  function listWidgets() {
    var out = [];
    widgets.forEach(function (w) {
      out.push({ id: w.id, type: w.type, title: titleFor(w.type, w.options) });
    });
    return out;
  }

  /**
   * composeScene(specs) — komponerar en MULTI-WIDGET-scen i #scenePanel.
   * Stänger befintliga scen-scoped widgets, beräknar grid-layout, skapar nya.
   *
   * specs: [{type, options}, ...]
   *   Storlek anpassas automatiskt så alla widgets får plats utan att täcka varandra.
   *
   * Returnerar lista av widget-IDs som skapades.
   */
  /**
   * composeScene(specs) — multi-widget scen med storleks-anpassning per relevans.
   * Spec-format: { type, size: "hero" | "medium" | "small", options }
   *
   * Layout: 6×4 sub-grid. hero=3×3 (stor), medium=2×2, small=2×1.
   * Algoritmen försöker packa widgets tätt utan överlapp.
   */
  function composeScene(specs) {
    if (!Array.isArray(specs) || specs.length === 0) return [];

    var toClose = [];
    widgets.forEach(function (w) {
      if (w.el && w.el.dataset && w.el.dataset.scope === "scene") toClose.push(w.id);
    });
    toClose.forEach(function (id) { closeWidget(id); });

    var scene = document.getElementById("scenePanel");
    if (!scene) return [];
    var rect = scene.getBoundingClientRect();
    if (rect.width < 100 || rect.height < 100) {
      return new Promise(function (resolve) {
        setTimeout(function () { resolve(composeScene(specs)); }, 200);
      });
    }

    // Anvand 90% av scen-ytan med inset sa det finns andning runt widgets — inte allt taeckt.
    var inset = 24;
    var usableW = rect.width - inset * 2;
    var usableH = rect.height - inset * 2;

    var GRID_COLS = 6, GRID_ROWS = 4;
    var pad = 10;
    var cellW = (usableW - pad * (GRID_COLS - 1)) / GRID_COLS;
    var cellH = (usableH - pad * (GRID_ROWS - 1)) / GRID_ROWS;

    // Skapa bitmap över upptagna celler.
    var taken = [];
    for (var r = 0; r < GRID_ROWS; r++) {
      var row = [];
      for (var c = 0; c < GRID_COLS; c++) row.push(false);
      taken.push(row);
    }

    function findFreeBlock(spanW, spanH) {
      for (var r = 0; r <= GRID_ROWS - spanH; r++) {
        for (var c = 0; c <= GRID_COLS - spanW; c++) {
          var fits = true;
          for (var dr = 0; dr < spanH && fits; dr++) {
            for (var dc = 0; dc < spanW && fits; dc++) {
              if (taken[r + dr][c + dc]) fits = false;
            }
          }
          if (fits) return { r: r, c: c };
        }
      }
      return null;
    }
    function markTaken(r, c, spanW, spanH) {
      for (var dr = 0; dr < spanH; dr++)
        for (var dc = 0; dc < spanW; dc++)
          if (r + dr < GRID_ROWS && c + dc < GRID_COLS) taken[r + dr][c + dc] = true;
    }
    function sizeToSpan(size) {
      if (size === "hero") return { w: 3, h: 3 };
      if (size === "small") return { w: 2, h: 1 };
      return { w: 2, h: 2 }; // medium / default
    }

    // Behall specs-ordning (anroparen styr visuell prioritet uppifran ned).
    // Bilder placeras forst -> hamnar overst i griden. Texter sist -> hamnar underst.
    var ordered = specs.map(function (s, i) { return { spec: s, idx: i }; });

    var ids = [];
    for (var i = 0; i < ordered.length; i++) {
      var s = ordered[i].spec || {};
      var span = sizeToSpan(s.size);
      var slot = findFreeBlock(span.w, span.h);
      // Fallback: mindre span om inte plats.
      if (!slot && span.w > 2) { span = { w: 2, h: 2 }; slot = findFreeBlock(2, 2); }
      if (!slot) { span = { w: 2, h: 1 }; slot = findFreeBlock(2, 1); }
      if (!slot) { span = { w: 1, h: 1 }; slot = findFreeBlock(1, 1); }
      if (!slot) continue;

      markTaken(slot.r, slot.c, span.w, span.h);

      var left = inset + slot.c * (cellW + pad);
      var top = inset + slot.r * (cellH + pad);
      var w = span.w * cellW + (span.w - 1) * pad;
      var h = span.h * cellH + (span.h - 1) * pad;

      var opts = Object.assign({}, s.options || {});
      opts._initialGeom = { left: left, top: top, width: w, height: h };
      try {
        var id = createWidget(s.type, opts);
        ids.push(id);
      } catch (e) {
        console.warn("composeScene: kunde inte skapa widget", s.type, e);
      }
    }
    return ids;
  }

  function clearSceneWidgets() {
    var toClose = [];
    widgets.forEach(function (w) {
      if (w.el && w.el.dataset && w.el.dataset.scope === "scene") toClose.push(w.id);
    });
    toClose.forEach(function (id) { closeWidget(id); });
    return toClose.length;
  }

  window.JarvisWidgetsV1 = {
    create: createWidget,
    update: updateWidget,
    close: closeWidget,
    list: listWidgets,
    composeScene: composeScene,
    clearScene: clearSceneWidgets,
    types: ["image", "iframe", "webcam", "video", "text", "chat-mini", "html"]
  };

  // Widget V2 — layout-API (anropas av C# via ExecuteScriptAsync + interna handlers)

  function postToHost(payload) {
    try {
      if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) {
        window.chrome.webview.postMessage(payload);
      }
    } catch (e) {}
  }

  window.jarvisWidgetCollectCurrentLayoutV1 = function (name) {
    var snapshot = [];
    widgets.forEach(function (w) {
      if (w.el.dataset.scope !== "scene") return; // Bara scen-scopade widgets sparas
      snapshot.push({
        type: w.type,
        gridX: w.gridX || 0,
        gridY: w.gridY || 0,
        gridW: w.gridW || 2,
        gridH: w.gridH || 2,
        options: w.options || {}
      });
    });
    postToHost({
      type: "widget_layout_save_finalize",
      name: String(name || "unnamed"),
      widgets: snapshot
    });
  };

  window.jarvisWidgetApplyLayoutV1 = function (payload) {
    var list = Array.isArray(payload) ? payload : [];
    if (list.length === 0) return;
    var layout = list[0];
    // Stäng existerande scen-scopade widgets
    var toClose = [];
    widgets.forEach(function (w) {
      if (w.el.dataset.scope === "scene") toClose.push(w.id);
    });
    toClose.forEach(function (id) { closeWidget(id); });
    // Skapa nya från layout
    (layout.widgets || []).forEach(function (spec) {
      var opts = Object.assign({}, spec.options || {});
      var px = gridCellToPixels(spec.gridX, spec.gridY, true);
      var d = gridDims(true);
      var width = spec.gridW * d.cellW + (spec.gridW - 1) * GRID_PAD;
      var height = spec.gridH * d.cellH + (spec.gridH - 1) * GRID_PAD;
      var localLeft = px.left - d.rect.left;
      var localTop = px.top - d.rect.top;
      opts._initialGeom = { left: localLeft, top: localTop, width: width, height: height };
      try {
        var id = createWidget(spec.type, opts);
        var w = widgets.get(id);
        if (w) {
          w.gridX = spec.gridX; w.gridY = spec.gridY;
          w.gridW = spec.gridW; w.gridH = spec.gridH;
        }
      } catch (e) {
        console.warn("apply layout: kunde inte skapa", spec.type, e);
      }
    });
  };

  var _knownLayouts = [];
  window.jarvisWidgetSetLayoutsV1 = function (payload) {
    _knownLayouts = Array.isArray(payload) ? payload : [];
    var menu = document.getElementById("layoutSwitcherMenu");
    if (!menu) return;
    menu.innerHTML = "";
    _knownLayouts.forEach(function (l) {
      var btn = document.createElement("button");
      btn.className = "layout-menu-item";
      btn.textContent = l.name + " (" + (l.widgets || []).length + ")";
      btn.addEventListener("click", function () {
        postToHost({ type: "widget_layout_load", id: l.id });
        menu.style.display = "none";
      });
      menu.appendChild(btn);
    });
  };
  window.jarvisWidgetGetKnownLayoutsV1 = function () { return _knownLayouts; };

  // Widget V2 — keyboard shortcuts (Ctrl+W close, Ctrl+M minimize, Ctrl+Shift+L menu, Esc unfocus, Tab cycle)
  function getFocusedWidget() {
    var found = null;
    widgets.forEach(function (w) {
      if (w.el.classList.contains("is-focused")) found = w;
    });
    return found;
  }

  function cycleWidgetFocus() {
    var arr = Array.from(widgets.values());
    if (arr.length === 0) return;
    var current = getFocusedWidget();
    var idx = current ? arr.indexOf(current) : -1;
    var next = arr[(idx + 1) % arr.length];
    if (next) focusWidget(next.id);
  }

  document.addEventListener("keydown", function (e) {
    var inInput = (e.target && (e.target.tagName === "INPUT" || e.target.tagName === "TEXTAREA"));
    if (inInput) return;

    if (e.ctrlKey && e.shiftKey && (e.key === "L" || e.key === "l")) {
      e.preventDefault();
      var btn = document.getElementById("layoutSwitcherBtn");
      if (btn) btn.click();
      return;
    }
    if (e.ctrlKey && !e.shiftKey && (e.key === "w" || e.key === "W")) {
      e.preventDefault();
      var w1 = getFocusedWidget();
      if (w1) closeWidget(w1.id);
      return;
    }
    if (e.ctrlKey && !e.shiftKey && (e.key === "m" || e.key === "M")) {
      e.preventDefault();
      var w2 = getFocusedWidget();
      if (w2) w2.el.classList.toggle("is-minimized");
      return;
    }
    if (e.key === "Escape") {
      widgets.forEach(function (w) { w.el.classList.remove("is-focused"); });
      return;
    }
    if (e.key === "Tab" && !e.ctrlKey && !e.altKey) {
      if (widgets.size > 0) {
        e.preventDefault();
        cycleWidgetFocus();
      }
    }
  });
})();
