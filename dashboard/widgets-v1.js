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
      var rect = widget.el.getBoundingClientRect();
      saveGeom(widget.type + "_" + (widget.el.dataset.scope || "global"), {
        left: parseFloat(widget.el.style.left) || rect.left,
        top: parseFloat(widget.el.style.top) || rect.top,
        width: rect.width, height: rect.height
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
      var rect = widget.el.getBoundingClientRect();
      saveGeom(widget.type + "_" + (widget.el.dataset.scope || "global"), {
        left: parseFloat(widget.el.style.left) || rect.left,
        top: parseFloat(widget.el.style.top) || rect.top,
        width: rect.width, height: rect.height
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
})();
