# Widget V2 — UI-shell Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bygga ut Sprint 3 `JarvisWidgetsV1` med snap-grid (12×8), widget-bibliotek-sidebar, multi-layout-system (sparas i `data/widgets/layouts.json`) och keyboard shortcuts. Detta är W-A sub-projekt — load-bearing för W-B (lokala widgets) och W-C (Spotify).

**Architecture:** Existing `JarvisWidgetsV1` IIFE i `dashboard/widgets-v1.js` får ny `gridX/Y/W/H`-position-model + snap-funktion. Ny `WidgetLayoutStoreV1.cs` lagrar named layouts. Sidebar + dropdowns i `dashboard/index.html`. Router/handler-dispatch i Program.cs följer befintligt voice/scene-mönster.

**Tech Stack:** Vanilla JS (ingen build-step), C# .NET 8 WinForms + WebView2, JSON-store + localStorage backup, postMessage-bridge mellan dashboard och C#.

---

## File Structure

| Path | Status | Responsibility |
|---|---|---|
| `dashboard/widgets-v1.js` | Modify | Lägg till grid-model, snap, sidebar-mountpoint, layout-API |
| `dashboard/widgets-v1.css` | Modify | Grid-overlay + sidebar styles |
| `dashboard/index.html` | Modify | Sidebar HTML, top-row dropdown + layout-switcher, keyboard handler |
| `app/Widgets/WidgetLayoutStoreV1.cs` | Create | Load/save `data/widgets/layouts.json` |
| `app/Widgets/WidgetLayoutCommandHandlerV1.cs` | Create | Pure handler för `/widget`-intents |
| `app/CommandRouterV1.cs` | Modify | 4 nya `CommandIntent.WidgetLayout*` + slash + NL |
| `app/CommandValidatorV1.cs` | Modify | Acceptera nya intents (no-op = SafeUi default) |
| `app/Program.cs` | Modify | 4 message-handlers + intent-dispatch |
| `tests/widget-snap-grid.test.js` | Create | Snap-grid math + DOM-ids |
| `tests/widget-layouts.test.js` | Create | Save/load layout-flöde |
| `tests/widget-sidebar-ui.test.js` | Create | Sidebar HTML/JS-ids |

---

## Task 1: Snap-grid math i widgets-v1.js

**Files:**
- Modify: `dashboard/widgets-v1.js` (lägg till nya helpers + uppdatera drag/resize)
- Test: `tests/widget-snap-grid.test.js`

- [ ] **Step 1: Skriv failing-test för snap-math**

Skapa `f:/Jarvis-clean/tests/widget-snap-grid.test.js`:

```javascript
const fs = require("fs");
const path = require("path");

const widgetsJs = fs.readFileSync(
  path.join(__dirname, "..", "dashboard", "widgets-v1.js"), "utf8"
);

let failures = 0;
function check(name, cond) {
  if (!cond) { failures++; console.log("FAIL " + name); }
  else { console.log("PASS " + name); }
}

// Snap-grid model
check("widgets-v1.js har GRID_COLS = 12", /GRID_COLS\s*=\s*12/.test(widgetsJs));
check("widgets-v1.js har GRID_ROWS = 8", /GRID_ROWS\s*=\s*8/.test(widgetsJs));
check("widgets-v1.js har pixelsToGridCell helper",
  widgetsJs.includes("pixelsToGridCell"));
check("widgets-v1.js har gridCellToPixels helper",
  widgetsJs.includes("gridCellToPixels"));
check("widgets-v1.js har snapToGrid funktion",
  widgetsJs.includes("snapToGrid"));
check("widgets-v1.js drag använder snapToGrid vid mouseup",
  /mouseup[\s\S]{0,400}snapToGrid/.test(widgetsJs));
check("widgets-v1.js resize använder snapToGrid vid mouseup",
  /resizing[\s\S]{0,800}snapToGrid/.test(widgetsJs));

if (failures > 0) {
  console.log("\n" + failures + " snap-grid failure(s)");
  process.exit(1);
}
console.log("\nWidget snap-grid checks passed.");
```

- [ ] **Step 2: Kör testet → ska FAILA**

```powershell
cd F:\Jarvis-clean
node tests/widget-snap-grid.test.js
```

Förväntat: FAIL på alla checks (inga av funktionerna finns än).

- [ ] **Step 3: Lägg till grid-konstanter + helpers i widgets-v1.js**

I `f:/Jarvis-clean/dashboard/widgets-v1.js`, lägg till efter `var LS_PREFIX = "jarvis_widget_v1_";` (rad ~17):

```javascript
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
```

- [ ] **Step 4: Uppdatera drag mouseup till att snappa**

I `f:/Jarvis-clean/dashboard/widgets-v1.js` `makeDraggable`-funktionen, hitta mouseup-handlern (rad ~90) och ersätt den med:

```javascript
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
```

- [ ] **Step 5: Uppdatera resize mouseup till att snappa**

I samma fil, `makeResizable`-funktionen, ersätt mouseup-handlern (rad ~123):

```javascript
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
```

- [ ] **Step 6: Lägg till stub-funktioner för grid-overlay (implementeras i Task 2)**

I `f:/Jarvis-clean/dashboard/widgets-v1.js`, lägg till efter `snapToGrid`-funktionen:

```javascript
  function showGridOverlay() {
    // Implementeras i Task 2
  }
  function hideGridOverlay() {
    // Implementeras i Task 2
  }
```

- [ ] **Step 7: Lägg till showGridOverlay() i drag/resize mousedown**

I `makeDraggable` mousedown-handler (rad ~72), efter `widget.el.classList.add("is-dragging");`:

```javascript
      showGridOverlay();
```

I `makeResizable` mousedown-handler (rad ~106), efter `widget.el.classList.add("is-resizing");`:

```javascript
      showGridOverlay();
```

- [ ] **Step 8: Kör testet → ska PASSA**

```powershell
cd F:\Jarvis-clean
node tests/widget-snap-grid.test.js
```

Förväntat: PASS på alla 7 checks.

- [ ] **Step 9: Commit**

```bash
git add dashboard/widgets-v1.js tests/widget-snap-grid.test.js
git commit -m "widget-v2: snap-to-grid math (12x8 grid) + drag/resize snap"
```

---

## Task 2: Grid-overlay CSS + JS-toggle

**Files:**
- Modify: `dashboard/widgets-v1.css` (lägg till `.widget-grid-overlay`)
- Modify: `dashboard/widgets-v1.js` (implementera show/hideGridOverlay)
- Test: `tests/widget-snap-grid.test.js` (extra checks)

- [ ] **Step 1: Lägg till grid-overlay CSS**

I `f:/Jarvis-clean/dashboard/widgets-v1.css`, append i slutet:

```css
/* Widget V2 — grid-overlay syns under drag/resize */
.widget-grid-overlay {
  position: fixed;
  pointer-events: none;
  z-index: 999;
  opacity: 0;
  transition: opacity 0.15s ease-out;
  background-image:
    repeating-linear-gradient(to right,
      rgba(106, 217, 255, 0.15) 0,
      rgba(106, 217, 255, 0.15) 1px,
      transparent 1px,
      transparent var(--grid-cell-w, 80px)),
    repeating-linear-gradient(to bottom,
      rgba(106, 217, 255, 0.15) 0,
      rgba(106, 217, 255, 0.15) 1px,
      transparent 1px,
      transparent var(--grid-cell-h, 60px));
}
.widget-grid-overlay.visible {
  opacity: 1;
}
```

- [ ] **Step 2: Implementera showGridOverlay() i widgets-v1.js**

Ersätt stub-funktionerna från Task 1 Step 6:

```javascript
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
    var anyDragging = false;
    widgets.forEach(function (w) {
      if (w.el.classList.contains("is-dragging") || w.el.classList.contains("is-resizing")) {
        anyDragging = true;
      }
    });
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
```

- [ ] **Step 3: Lägg till test för overlay-CSS**

Lägg till i `f:/Jarvis-clean/tests/widget-snap-grid.test.js` före `if (failures > 0)`:

```javascript
const widgetsCss = fs.readFileSync(
  path.join(__dirname, "..", "dashboard", "widgets-v1.css"), "utf8"
);
check("widgets-v1.css har .widget-grid-overlay",
  widgetsCss.includes(".widget-grid-overlay"));
check("widgets-v1.css har .widget-grid-overlay.visible",
  widgetsCss.includes(".widget-grid-overlay.visible"));
check("widgets-v1.js har ensureGridOverlay",
  widgetsJs.includes("ensureGridOverlay"));
```

- [ ] **Step 4: Kör test → PASS**

```powershell
cd F:\Jarvis-clean
node tests/widget-snap-grid.test.js
```

- [ ] **Step 5: Commit**

```bash
git add dashboard/widgets-v1.css dashboard/widgets-v1.js tests/widget-snap-grid.test.js
git commit -m "widget-v2: grid-overlay (12x8) visas under drag/resize"
```

---

## Task 3: Widget-sidebar HTML + JS

**Files:**
- Modify: `dashboard/index.html` (sidebar HTML + JS handlers)
- Modify: `dashboard/widgets-v1.css` (sidebar styles)
- Test: `tests/widget-sidebar-ui.test.js`

- [ ] **Step 1: Skriv failing test för sidebar-ids**

Skapa `f:/Jarvis-clean/tests/widget-sidebar-ui.test.js`:

```javascript
const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const dashboard = fs.readFileSync(path.join(root, "dashboard", "index.html"), "utf8");

let failures = 0;
function check(name, cond) {
  if (!cond) { failures++; console.log("FAIL " + name); }
  else { console.log("PASS " + name); }
}

check("Dashboard har #widgetSidebar", dashboard.includes('id="widgetSidebar"'));
check("Dashboard har #widgetSidebarToggle", dashboard.includes('id="widgetSidebarToggle"'));
check("Dashboard har widget-sidebar ikoner",
  dashboard.includes('data-widget-type="text"') &&
  dashboard.includes('data-widget-type="image"') &&
  dashboard.includes('data-widget-type="iframe"') &&
  dashboard.includes('data-widget-type="webcam"') &&
  dashboard.includes('data-widget-type="chat-mini"'));
check("Dashboard har sidebar-collapse-toggle handler",
  /widgetSidebarToggle[\s\S]{0,200}classList\.toggle/.test(dashboard));
check("Dashboard har widget-create-on-click handler",
  /data-widget-type[\s\S]{0,400}JarvisWidgetsV1\.create/.test(dashboard));

if (failures > 0) {
  console.log("\n" + failures + " widget-sidebar-ui failure(s)");
  process.exit(1);
}
console.log("\nWidget sidebar UI checks passed.");
```

- [ ] **Step 2: Kör test → FAIL (alla 5 checks)**

```powershell
cd F:\Jarvis-clean
node tests/widget-sidebar-ui.test.js
```

- [ ] **Step 3: Lägg till sidebar HTML i index.html**

I `f:/Jarvis-clean/dashboard/index.html`, hitta `<body>`-taggen och lägg till DIREKT efter `<body>`:

```html
  <aside id="widgetSidebar" class="widget-sidebar" data-collapsed="false">
    <button id="widgetSidebarToggle" class="widget-sidebar-toggle" title="Visa/dölj widget-bibliotek">◧</button>
    <div class="widget-sidebar-list">
      <button class="widget-icon" data-widget-type="text" title="Text-widget">T</button>
      <button class="widget-icon" data-widget-type="image" title="Bild-widget">🖼</button>
      <button class="widget-icon" data-widget-type="iframe" title="Webb-widget">🌐</button>
      <button class="widget-icon" data-widget-type="webcam" title="Kamera-widget">📷</button>
      <button class="widget-icon" data-widget-type="video" title="Video-widget">▶</button>
      <button class="widget-icon" data-widget-type="chat-mini" title="Chat-mini">💬</button>
      <button class="widget-icon" data-widget-type="html" title="HTML-widget">{}</button>
    </div>
  </aside>
```

- [ ] **Step 4: Lägg till sidebar CSS**

I `f:/Jarvis-clean/dashboard/widgets-v1.css`, append:

```css
/* Widget V2 — sidebar */
.widget-sidebar {
  position: fixed;
  top: 80px;
  left: 0;
  width: 56px;
  background: rgba(6, 21, 32, 0.92);
  border-right: 1px solid rgba(106, 217, 255, 0.25);
  border-top-right-radius: 8px;
  border-bottom-right-radius: 8px;
  z-index: 50;
  display: flex;
  flex-direction: column;
  padding: 8px 0;
  transition: transform 0.2s ease-out;
  box-shadow: 0 0 24px rgba(106, 217, 255, 0.1);
}
.widget-sidebar[data-collapsed="true"] {
  transform: translateX(-50px);
}
.widget-sidebar-toggle {
  background: rgba(106, 217, 255, 0.15);
  border: 1px solid rgba(106, 217, 255, 0.3);
  color: #aee6ff;
  cursor: pointer;
  width: 32px;
  height: 32px;
  border-radius: 4px;
  margin: 0 auto 10px;
  font-size: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
}
.widget-sidebar-toggle:hover {
  background: rgba(106, 217, 255, 0.3);
}
.widget-sidebar-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 0 8px;
}
.widget-icon {
  width: 40px;
  height: 40px;
  background: rgba(13, 31, 46, 0.85);
  border: 1px solid rgba(106, 217, 255, 0.25);
  color: #aee6ff;
  font-size: 16px;
  cursor: pointer;
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
}
.widget-icon:hover {
  background: rgba(106, 217, 255, 0.2);
  border-color: #6ad9ff;
  box-shadow: 0 0 12px rgba(106, 217, 255, 0.4);
}
```

- [ ] **Step 5: Lägg till sidebar JS-handlers i index.html**

I `f:/Jarvis-clean/dashboard/index.html`, hitta `<script>`-blocket nära slutet och lägg till efter `JarvisWidgetsV1`-referenser (sök efter `JarvisWidgetsV1` så hittar du rätt plats):

```javascript
    // Widget V2 sidebar — collapse + create-on-click
    (function () {
      var toggle = document.getElementById("widgetSidebarToggle");
      var sidebar = document.getElementById("widgetSidebar");
      if (toggle && sidebar) {
        toggle.addEventListener("click", function () {
          var collapsed = sidebar.getAttribute("data-collapsed") === "true";
          sidebar.setAttribute("data-collapsed", collapsed ? "false" : "true");
        });
      }
      var icons = document.querySelectorAll(".widget-icon[data-widget-type]");
      icons.forEach(function (btn) {
        btn.addEventListener("click", function () {
          var type = btn.getAttribute("data-widget-type");
          if (!type || !window.JarvisWidgetsV1) return;
          var defaults = {
            text: { content: "Ny anteckning..." },
            html: { content: "<div>HTML här</div>" },
            image: { url: "" },
            iframe: { url: "about:blank" }
          };
          window.JarvisWidgetsV1.create(type, defaults[type] || {});
        });
      });
    })();
```

- [ ] **Step 6: Kör test → PASS**

```powershell
cd F:\Jarvis-clean
node tests/widget-sidebar-ui.test.js
```

- [ ] **Step 7: Commit**

```bash
git add dashboard/index.html dashboard/widgets-v1.css tests/widget-sidebar-ui.test.js
git commit -m "widget-v2: sidebar med 7 widget-typer + collapse-toggle"
```

---

## Task 4: WidgetLayoutStoreV1.cs (C# store)

**Files:**
- Create: `app/Widgets/WidgetLayoutStoreV1.cs`

- [ ] **Step 1: Skapa filen**

Skapa `f:/Jarvis-clean/app/Widgets/WidgetLayoutStoreV1.cs`:

```csharp
using System.Text.Json;

namespace JarvisClean;

internal sealed record WidgetPlacementV1(
    string Type,
    int GridX,
    int GridY,
    int GridW,
    int GridH,
    Dictionary<string, string>? Options);

internal sealed record WidgetLayoutV1(
    string Id,
    string Name,
    List<WidgetPlacementV1> Widgets);

/// <summary>
/// Widget V2 - lagrar named layouts (Work, Play, Brief, user-saved).
/// User-driven save (klick), ingen PendingApproval (samma som TaskStoreV1).
/// </summary>
internal static class WidgetLayoutStoreV1
{
    private static readonly object Lock = new();

    public static string LayoutsFilePath(string projectRoot)
    {
        return Path.Combine(projectRoot, "data", "widgets", "layouts.json");
    }

    public static List<WidgetLayoutV1> LoadAll(string projectRoot)
    {
        var path = LayoutsFilePath(projectRoot);
        if (!File.Exists(path)) return SeedDefault();
        try
        {
            var json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<WidgetLayoutV1>>(json);
            return list ?? SeedDefault();
        }
        catch
        {
            return SeedDefault();
        }
    }

    public static WidgetLayoutV1? Get(string projectRoot, string id)
    {
        var all = LoadAll(projectRoot);
        return all.FirstOrDefault(l => string.Equals(l.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public static void Save(string projectRoot, WidgetLayoutV1 layout)
    {
        lock (Lock)
        {
            var all = LoadAll(projectRoot);
            var idx = all.FindIndex(l => string.Equals(l.Id, layout.Id, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) all[idx] = layout;
            else all.Add(layout);
            WriteAll(projectRoot, all);
        }
    }

    public static bool Delete(string projectRoot, string id)
    {
        lock (Lock)
        {
            var all = LoadAll(projectRoot);
            var removed = all.RemoveAll(l => string.Equals(l.Id, id, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
            {
                WriteAll(projectRoot, all);
                return true;
            }
            return false;
        }
    }

    public static string ToClientJson(List<WidgetLayoutV1> layouts)
    {
        return JsonSerializer.Serialize(layouts.Select(l => new
        {
            id = l.Id,
            name = l.Name,
            widgets = l.Widgets.Select(w => new
            {
                type = w.Type,
                gridX = w.GridX, gridY = w.GridY,
                gridW = w.GridW, gridH = w.GridH,
                options = w.Options
            }).ToList()
        }).ToList());
    }

    private static void WriteAll(string projectRoot, List<WidgetLayoutV1> all)
    {
        var path = LayoutsFilePath(projectRoot);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(all, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    private static List<WidgetLayoutV1> SeedDefault()
    {
        return new List<WidgetLayoutV1>
        {
            new("default", "Default", new List<WidgetPlacementV1>()),
            new("work", "Work", new List<WidgetPlacementV1>
            {
                new("text", 0, 0, 3, 2, new() { ["content"] = "Work tasks" }),
                new("chat-mini", 9, 0, 3, 4, null)
            }),
            new("play", "Play", new List<WidgetPlacementV1>()),
            new("brief", "Brief", new List<WidgetPlacementV1>())
        };
    }
}
```

- [ ] **Step 2: Bygg → 0 errors**

```powershell
cd F:\Jarvis-clean
dotnet build app\JarvisClean.csproj -nologo 2>&1 | Select-String "Error\(s\)" | Select-Object -Last 1
```

Förväntat: `0 Error(s)`.

- [ ] **Step 3: Commit**

```bash
git add app/Widgets/WidgetLayoutStoreV1.cs
git commit -m "widget-v2: WidgetLayoutStoreV1 (CRUD för named layouts)"
```

---

## Task 5: WidgetLayoutCommandHandlerV1.cs + router

**Files:**
- Create: `app/Widgets/WidgetLayoutCommandHandlerV1.cs`
- Modify: `app/CommandRouterV1.cs` (4 nya intents + parsning)

- [ ] **Step 1: Skapa handler-fil**

Skapa `f:/Jarvis-clean/app/Widgets/WidgetLayoutCommandHandlerV1.cs`:

```csharp
namespace JarvisClean;

internal static class WidgetLayoutCommandHandlerV1
{
    public static string Apply(CommandResult command, string projectRoot)
    {
        return command.Intent switch
        {
            CommandIntent.WidgetLayoutSave => SaveCurrent(command, projectRoot),
            CommandIntent.WidgetLayoutLoad => Load(command, projectRoot),
            CommandIntent.WidgetLayoutList => List(projectRoot),
            CommandIntent.WidgetLayoutDelete => Delete(command, projectRoot),
            _ => "Okänt widget-layout-kommando."
        };
    }

    private static string SaveCurrent(CommandResult command, string projectRoot)
    {
        var name = command.Arguments.TryGetValue("name", out var n) ? (n ?? "").Trim() : "";
        if (string.IsNullOrWhiteSpace(name))
            return "Ange ett namn för layouten. Exempel: /widget save Mitt-namn";
        // Faktisk save sker via JS som plockar nuvarande widgets och postar tillbaka
        // till "widget_layout_save_finalize" — den här handlern triggar JS-side att börja.
        return "Sparar nuvarande widget-layout som '" + name + "'... (klart om en stund)";
    }

    private static string Load(CommandResult command, string projectRoot)
    {
        var name = command.Arguments.TryGetValue("name", out var n) ? (n ?? "").Trim() : "";
        if (string.IsNullOrWhiteSpace(name))
            return "Ange vilken layout att ladda. Lista: /widget list";
        var layout = WidgetLayoutStoreV1.Get(projectRoot, name);
        if (layout is null)
            return "Layout '" + name + "' hittades inte. Lista: /widget list";
        return "Laddar layout '" + layout.Name + "' (" + layout.Widgets.Count + " widgets).";
    }

    private static string List(string projectRoot)
    {
        var all = WidgetLayoutStoreV1.LoadAll(projectRoot);
        if (all.Count == 0) return "Inga sparade layouts.";
        var lines = all.Select(l => "• " + l.Name + " (" + l.Widgets.Count + " widgets) — id: " + l.Id);
        return "Widget-layouts:\n" + string.Join("\n", lines);
    }

    private static string Delete(CommandResult command, string projectRoot)
    {
        var name = command.Arguments.TryGetValue("name", out var n) ? (n ?? "").Trim() : "";
        if (string.IsNullOrWhiteSpace(name))
            return "Ange vilken layout att radera.";
        var ok = WidgetLayoutStoreV1.Delete(projectRoot, name);
        return ok ? "Layout '" + name + "' raderad." : "Layout '" + name + "' hittades inte.";
    }
}
```

- [ ] **Step 2: Lägg till intents i CommandRouterV1.cs**

I `f:/Jarvis-clean/app/CommandRouterV1.cs`, hitta `enum CommandIntent` (rad ~3) och lägg till nya värden i slutet (före `}`):

```csharp
    WidgetLayoutSave,
    WidgetLayoutLoad,
    WidgetLayoutList,
    WidgetLayoutDelete,
```

- [ ] **Step 3: Lägg till slash + NL i router**

I samma fil, hitta `ParseSlashCommand` och `IsVoiceNaturalLanguage`-pattern. Lägg till nya parsning EFTER scene-blocket (sök efter `command.StartsWith("scen ")` så hittar du var):

```csharp
        // Widget-layout
        if (command.StartsWith("widget save ") || command.StartsWith("spara layout som "))
        {
            var prefix = command.StartsWith("spara layout som ") ? "spara layout som " : "widget save ";
            return WidgetLayoutResult(CommandIntent.WidgetLayoutSave, command.Substring(prefix.Length).Trim());
        }
        if (command.StartsWith("widget load ") || command.StartsWith("ladda layout ")
            || command.StartsWith("byt till layout "))
        {
            string p;
            if (command.StartsWith("byt till layout ")) p = "byt till layout ";
            else if (command.StartsWith("ladda layout ")) p = "ladda layout ";
            else p = "widget load ";
            return WidgetLayoutResult(CommandIntent.WidgetLayoutLoad, command.Substring(p.Length).Trim());
        }
        if (command is "widget list" or "lista layouts" or "visa layouts")
            return WidgetLayoutResult(CommandIntent.WidgetLayoutList, "");
        if (command.StartsWith("widget delete ") || command.StartsWith("ta bort layout "))
        {
            var p = command.StartsWith("ta bort layout ") ? "ta bort layout " : "widget delete ";
            return WidgetLayoutResult(CommandIntent.WidgetLayoutDelete, command.Substring(p.Length).Trim());
        }
```

Lägg också till helper-funktionen `WidgetLayoutResult` (kan vara nära andra `*Result`-helpers, t.ex. nära `SceneResult`):

```csharp
    private static CommandResult WidgetLayoutResult(CommandIntent intent, string name)
    {
        var args = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(name)) args["name"] = name;
        return new CommandResult
        {
            Intent = intent,
            Risk = CommandRisk.SafeUi,
            ToolName = "widget.layout." + intent.ToString().Replace("WidgetLayout", "").ToLowerInvariant(),
            Arguments = args,
            ShouldSendToOllama = false
        };
    }
```

- [ ] **Step 4: Bygg → 0 errors**

```powershell
cd F:\Jarvis-clean
dotnet build app\JarvisClean.csproj -nologo 2>&1 | Select-String "Error\(s\)" | Select-Object -Last 1
```

- [ ] **Step 5: Commit**

```bash
git add app/Widgets/WidgetLayoutCommandHandlerV1.cs app/CommandRouterV1.cs
git commit -m "widget-v2: router intents + slash/NL parsning + handler"
```

---

## Task 6: Program.cs dispatch + message-handlers

**Files:**
- Modify: `app/Program.cs` (dispatch nya intents + 4 message-handlers)

- [ ] **Step 1: Lägg till intent-dispatch i TryHandleCommandRouterV1UiAsync**

I `f:/Jarvis-clean/app/Program.cs`, hitta `routedV1.Intent == CommandIntent.SceneShow`-blocket och lägg till EFTER det:

```csharp
        if (routedV1.Intent is CommandIntent.WidgetLayoutSave
            or CommandIntent.WidgetLayoutLoad
            or CommandIntent.WidgetLayoutList
            or CommandIntent.WidgetLayoutDelete)
        {
            await AddAssistantMessage(WidgetLayoutCommandHandlerV1.Apply(routedV1, ProjectRoot));
            if (routedV1.Intent == CommandIntent.WidgetLayoutSave && _webView.CoreWebView2 is not null)
            {
                var nameArg = routedV1.Arguments.TryGetValue("name", out var n) ? n : "";
                var nameJson = JsonSerializer.Serialize(nameArg);
                await _webView.CoreWebView2.ExecuteScriptAsync(
                    $"window.jarvisWidgetCollectCurrentLayoutV1 && window.jarvisWidgetCollectCurrentLayoutV1({nameJson});");
            }
            if (routedV1.Intent == CommandIntent.WidgetLayoutLoad && _webView.CoreWebView2 is not null)
            {
                var nameArg = routedV1.Arguments.TryGetValue("name", out var n) ? n : "";
                var layout = WidgetLayoutStoreV1.Get(ProjectRoot, nameArg);
                if (layout is not null)
                {
                    var json = WidgetLayoutStoreV1.ToClientJson(new List<WidgetLayoutV1> { layout });
                    await _webView.CoreWebView2.ExecuteScriptAsync(
                        $"window.jarvisWidgetApplyLayoutV1 && window.jarvisWidgetApplyLayoutV1({json});");
                }
            }
            return true;
        }
```

- [ ] **Step 2: Lägg till message-handlers i OnWebMessageReceived**

I `OnWebMessageReceived` (rad ~562), hitta sista voice/karta-handler och lägg till efter:

```csharp
            // Widget V2 message-handlers
            if (type == "widget_layout_list")
            {
                await SendWidgetLayoutsAsync();
                return;
            }
            if (type == "widget_layout_save_finalize")
            {
                await HandleWidgetLayoutSaveFinalizeAsync(root);
                return;
            }
            if (type == "widget_layout_load")
            {
                await HandleWidgetLayoutLoadAsync(root);
                return;
            }
            if (type == "widget_layout_delete")
            {
                await HandleWidgetLayoutDeleteAsync(root);
                return;
            }
```

- [ ] **Step 3: Lägg till handler-implementationer**

Hitta en lämplig plats nära andra `HandleKarta*Async`-metoder och lägg till:

```csharp
    private async Task SendWidgetLayoutsAsync()
    {
        if (_webView.CoreWebView2 is null) return;
        var all = WidgetLayoutStoreV1.LoadAll(ProjectRoot);
        var json = WidgetLayoutStoreV1.ToClientJson(all);
        await _webView.CoreWebView2.ExecuteScriptAsync(
            $"window.jarvisWidgetSetLayoutsV1 && window.jarvisWidgetSetLayoutsV1({json});");
    }

    private async Task HandleWidgetLayoutSaveFinalizeAsync(JsonElement root)
    {
        try
        {
            var name = root.TryGetProperty("name", out var nEl) ? (nEl.GetString() ?? "") : "";
            if (string.IsNullOrWhiteSpace(name))
            {
                await AddAssistantMessage("Widget-layout save: namn saknas.");
                return;
            }
            var widgets = new List<WidgetPlacementV1>();
            if (root.TryGetProperty("widgets", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var w in arr.EnumerateArray())
                {
                    var t = w.TryGetProperty("type", out var tEl) ? (tEl.GetString() ?? "") : "";
                    var gx = w.TryGetProperty("gridX", out var gxEl) ? gxEl.GetInt32() : 0;
                    var gy = w.TryGetProperty("gridY", out var gyEl) ? gyEl.GetInt32() : 0;
                    var gw = w.TryGetProperty("gridW", out var gwEl) ? gwEl.GetInt32() : 2;
                    var gh = w.TryGetProperty("gridH", out var ghEl) ? ghEl.GetInt32() : 2;
                    Dictionary<string, string>? opts = null;
                    if (w.TryGetProperty("options", out var oEl) && oEl.ValueKind == JsonValueKind.Object)
                    {
                        opts = new Dictionary<string, string>();
                        foreach (var p in oEl.EnumerateObject())
                            opts[p.Name] = p.Value.ToString();
                    }
                    if (!string.IsNullOrWhiteSpace(t))
                        widgets.Add(new WidgetPlacementV1(t, gx, gy, gw, gh, opts));
                }
            }
            var id = name.ToLowerInvariant().Replace(" ", "-");
            WidgetLayoutStoreV1.Save(ProjectRoot, new WidgetLayoutV1(id, name, widgets));
            await AddAssistantMessage("Layout '" + name + "' sparad (" + widgets.Count + " widgets).");
            await SendWidgetLayoutsAsync();
        }
        catch (Exception ex)
        {
            await AddAssistantMessage("Widget-layout save fel: " + ex.Message);
        }
    }

    private async Task HandleWidgetLayoutLoadAsync(JsonElement root)
    {
        var id = root.TryGetProperty("id", out var idEl) ? (idEl.GetString() ?? "") : "";
        if (string.IsNullOrWhiteSpace(id)) return;
        var layout = WidgetLayoutStoreV1.Get(ProjectRoot, id);
        if (layout is null)
        {
            await AddAssistantMessage("Layout '" + id + "' hittades inte.");
            return;
        }
        if (_webView.CoreWebView2 is null) return;
        var json = WidgetLayoutStoreV1.ToClientJson(new List<WidgetLayoutV1> { layout });
        await _webView.CoreWebView2.ExecuteScriptAsync(
            $"window.jarvisWidgetApplyLayoutV1 && window.jarvisWidgetApplyLayoutV1({json});");
    }

    private async Task HandleWidgetLayoutDeleteAsync(JsonElement root)
    {
        var id = root.TryGetProperty("id", out var idEl) ? (idEl.GetString() ?? "") : "";
        if (string.IsNullOrWhiteSpace(id)) return;
        var ok = WidgetLayoutStoreV1.Delete(ProjectRoot, id);
        if (ok)
        {
            await AddAssistantMessage("Layout borttagen.");
            await SendWidgetLayoutsAsync();
        }
    }
```

- [ ] **Step 4: Bygg → 0 errors**

```powershell
cd F:\Jarvis-clean
dotnet build app\JarvisClean.csproj -nologo 2>&1 | Select-String "Error\(s\)" | Select-Object -Last 1
```

- [ ] **Step 5: Commit**

```bash
git add app/Program.cs
git commit -m "widget-v2: Program.cs dispatch + 4 layout-message-handlers"
```

---

## Task 7: JS layout-API (collect/apply/setLayouts)

**Files:**
- Modify: `dashboard/widgets-v1.js` (lägg till global window-API)

- [ ] **Step 1: Lägg till globala API-funktioner**

I `f:/Jarvis-clean/dashboard/widgets-v1.js`, hitta `window.JarvisWidgetsV1 = { ... };`-blocket nära slutet. EFTER det blocket (men före `})();`), lägg till:

```javascript
  // Widget V2 — layout-API (anropas av C# via ExecuteScriptAsync)

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
    postMessage({
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
        postMessage({ type: "widget_layout_load", id: l.id });
        menu.style.display = "none";
      });
      menu.appendChild(btn);
    });
  };
  window.jarvisWidgetGetKnownLayoutsV1 = function () { return _knownLayouts; };
```

- [ ] **Step 2: Lägg till test för API-funktionerna**

Lägg till i `f:/Jarvis-clean/tests/widget-layouts.test.js` (skapa nytt fil om saknas):

```javascript
const fs = require("fs");
const path = require("path");

const widgetsJs = fs.readFileSync(
  path.join(__dirname, "..", "dashboard", "widgets-v1.js"), "utf8"
);
const program = fs.readFileSync(
  path.join(__dirname, "..", "app", "Program.cs"), "utf8"
);
const router = fs.readFileSync(
  path.join(__dirname, "..", "app", "CommandRouterV1.cs"), "utf8"
);

let failures = 0;
function check(name, cond) {
  if (!cond) { failures++; console.log("FAIL " + name); }
  else { console.log("PASS " + name); }
}

// Router intents
["WidgetLayoutSave", "WidgetLayoutLoad", "WidgetLayoutList", "WidgetLayoutDelete"]
  .forEach(function (x) { check("Router har " + x, router.includes(x)); });

// Router parsning
check("Router parsar 'spara layout som'", router.includes("spara layout som"));
check("Router parsar 'ladda layout'", router.includes("ladda layout"));
check("Router parsar 'ta bort layout'", router.includes("ta bort layout"));

// JS API
check("widgets-v1.js har jarvisWidgetCollectCurrentLayoutV1",
  widgetsJs.includes("jarvisWidgetCollectCurrentLayoutV1"));
check("widgets-v1.js har jarvisWidgetApplyLayoutV1",
  widgetsJs.includes("jarvisWidgetApplyLayoutV1"));
check("widgets-v1.js har jarvisWidgetSetLayoutsV1",
  widgetsJs.includes("jarvisWidgetSetLayoutsV1"));

// Program.cs handlers
["widget_layout_list", "widget_layout_save_finalize", "widget_layout_load", "widget_layout_delete"]
  .forEach(function (x) { check("Program.cs har handler " + x, program.includes(x)); });
check("Program.cs har SendWidgetLayoutsAsync", program.includes("SendWidgetLayoutsAsync"));

if (failures > 0) {
  console.log("\n" + failures + " widget-layouts failure(s)");
  process.exit(1);
}
console.log("\nWidget layouts checks passed.");
```

- [ ] **Step 3: Kör test → PASS**

```powershell
cd F:\Jarvis-clean
node tests/widget-layouts.test.js
```

- [ ] **Step 4: Commit**

```bash
git add dashboard/widgets-v1.js tests/widget-layouts.test.js
git commit -m "widget-v2: JS layout-API (collect/apply/setLayouts)"
```

---

## Task 8: Layout-switcher UI (top-row dropdown)

**Files:**
- Modify: `dashboard/index.html` (top-row knapp + meny)
- Modify: `dashboard/widgets-v1.css` (menu styles)

- [ ] **Step 1: Lägg till layout-switcher HTML i index.html**

Hitta top-row med befintliga panel-knappar (sök efter `id="showSettingsBtn"`) och lägg till FÖRE den:

```html
        <button id="layoutSwitcherBtn" class="panel-button" type="button" title="Byt widget-layout">Layout ▾</button>
        <div id="layoutSwitcherMenu" class="layout-menu" style="display:none;"></div>
        <button id="layoutSaveCurrentBtn" class="panel-button" type="button" title="Spara nuvarande layout">💾</button>
```

- [ ] **Step 2: Lägg till menu CSS**

I `f:/Jarvis-clean/dashboard/widgets-v1.css`, append:

```css
/* Widget V2 — layout-switcher menu */
.layout-menu {
  position: fixed;
  top: 80px;
  right: 100px;
  background: rgba(6, 21, 32, 0.95);
  border: 1px solid rgba(106, 217, 255, 0.3);
  border-radius: 6px;
  padding: 6px;
  z-index: 100;
  display: flex;
  flex-direction: column;
  gap: 4px;
  min-width: 200px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.5);
}
.layout-menu-item {
  background: transparent;
  border: none;
  color: #aee6ff;
  text-align: left;
  padding: 8px 12px;
  cursor: pointer;
  border-radius: 4px;
  font-family: inherit;
  font-size: 13px;
}
.layout-menu-item:hover {
  background: rgba(106, 217, 255, 0.2);
}
```

- [ ] **Step 3: Lägg till switcher-handlers i index.html**

Efter sidebar-handlers från Task 3:

```javascript
    // Widget V2 layout-switcher
    (function () {
      var btn = document.getElementById("layoutSwitcherBtn");
      var menu = document.getElementById("layoutSwitcherMenu");
      var saveBtn = document.getElementById("layoutSaveCurrentBtn");

      if (btn && menu) {
        btn.addEventListener("click", function (e) {
          e.stopPropagation();
          if (menu.style.display === "none") {
            postMessage({ type: "widget_layout_list" });
            menu.style.display = "flex";
          } else {
            menu.style.display = "none";
          }
        });
        document.addEventListener("click", function (e) {
          if (e.target !== btn && !menu.contains(e.target)) {
            menu.style.display = "none";
          }
        });
      }

      if (saveBtn) {
        saveBtn.addEventListener("click", function () {
          var name = prompt("Namn på layouten?");
          if (!name || !name.trim()) return;
          if (window.jarvisWidgetCollectCurrentLayoutV1) {
            window.jarvisWidgetCollectCurrentLayoutV1(name.trim());
          }
        });
      }
    })();
```

- [ ] **Step 4: Bygg + manuell test**

```powershell
cd F:\Jarvis-clean
dotnet build app\JarvisClean.csproj -nologo 2>&1 | Select-String "Error\(s\)" | Select-Object -Last 1
```

- [ ] **Step 5: Commit**

```bash
git add dashboard/index.html dashboard/widgets-v1.css
git commit -m "widget-v2: layout-switcher dropdown + 'Spara layout'-knapp i top-row"
```

---

## Task 9: Keyboard shortcuts

**Files:**
- Modify: `dashboard/widgets-v1.js` (lägg till keyboard listener)

- [ ] **Step 1: Lägg till keyboard handler i widgets-v1.js**

I `f:/Jarvis-clean/dashboard/widgets-v1.js`, lägg till efter `_knownLayouts`-deklarationen (Task 7):

```javascript
  // Widget V2 — keyboard shortcuts
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
    if (e.ctrlKey && (e.key === "w" || e.key === "W")) {
      e.preventDefault();
      var w1 = getFocusedWidget();
      if (w1) closeWidget(w1.id);
      return;
    }
    if (e.ctrlKey && (e.key === "m" || e.key === "M")) {
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
```

- [ ] **Step 2: Lägg till test för keyboard handlers**

Lägg till i `f:/Jarvis-clean/tests/widget-layouts.test.js` före `if (failures > 0)`:

```javascript
check("widgets-v1.js har Ctrl+W handler",
  /e\.ctrlKey[\s\S]{0,80}["']w["']/.test(widgetsJs));
check("widgets-v1.js har Ctrl+M handler",
  /e\.ctrlKey[\s\S]{0,80}["']m["']/.test(widgetsJs));
check("widgets-v1.js har Escape handler",
  widgetsJs.includes('e.key === "Escape"'));
check("widgets-v1.js har Tab-cycling",
  widgetsJs.includes("cycleWidgetFocus"));
```

- [ ] **Step 3: Kör test → PASS**

```powershell
cd F:\Jarvis-clean
node tests/widget-layouts.test.js
```

- [ ] **Step 4: Commit**

```bash
git add dashboard/widgets-v1.js tests/widget-layouts.test.js
git commit -m "widget-v2: keyboard shortcuts (Ctrl+W/M, Ctrl+Shift+L, Tab cycle, Esc)"
```

---

## Task 10: Slutlig build + smoke + publish

- [ ] **Step 1: Stoppa Jarvis-instanser**

```powershell
Get-Process -Name "Jarvis","JarvisClean" -ErrorAction SilentlyContinue | ForEach-Object { try { $_.Kill(); $_.WaitForExit(5000) } catch {} }
```

- [ ] **Step 2: Kör alla widget-tester**

```powershell
cd F:\Jarvis-clean
node tests/widget-snap-grid.test.js
node tests/widget-sidebar-ui.test.js
node tests/widget-layouts.test.js
```

Förväntat: alla PASS.

- [ ] **Step 3: Full smoke**

```powershell
cd F:\Jarvis-clean
powershell -NoProfile -ExecutionPolicy Bypass -File tests/run-full-smoke.ps1
```

Förväntat: `ALL CHECKS PASSED`.

- [ ] **Step 4: Publicera**

```powershell
cd F:\Jarvis-clean
dotnet publish app\JarvisClean.csproj -c Debug -o dist --nologo
```

- [ ] **Step 5: Starta Jarvis**

```powershell
Start-Process -FilePath "wscript.exe" -ArgumentList "F:\Jarvis-clean\Starta-Jarvis.vbs"
```

- [ ] **Step 6: Manuell verifiering**

1. Klicka på Scen-fliken
2. Klicka på sidebar-ikon "T" → text-widget skapas
3. Dra widgeten — grid-overlay syns
4. Släpp — widgeten snappar till cell
5. Tryck Ctrl+Shift+L → Layout-meny öppnas
6. Klicka 💾 → ange "Test" → spara
7. Stäng widgeten (Ctrl+W eller × i header)
8. Tryck Ctrl+Shift+L → Klicka "Test" → widgeten återställs

- [ ] **Step 7: Commit slutfas + uppdatera CURRENT_STATE.md**

```bash
git add -A
git commit -m "widget-v2: alla tester + smoke PASS + publicerad"
```

I `f:/Jarvis-clean/CURRENT_STATE.md`, lägg till överst (efter "Senast uppdaterad"-raden):

```markdown
## 2026-05-22 — Widget V2 W-A (UI-shell)

Sub-projekt W-A av Widget V2 är live:

- Snap-grid (12×8) med pixel→cell-konvertering vid drag/resize release
- Grid-overlay (cyan rader+kolumner) visas endast under drag/resize
- Widget-sidebar (56px) med 7 typer + collapse-toggle
- Layout-switcher i top-row: meny + "Spara layout"-knapp
- `data/widgets/layouts.json` lagrar named layouts (Default/Work/Play/Brief seed)
- Slash + NL: /widget save/load/list/delete, "spara layout som X", "ladda layout X"
- Keyboard shortcuts: Ctrl+W (close), Ctrl+M (minimera), Ctrl+Shift+L (meny), Tab (cycle), Esc (unfocus)
- Nya filer: `app/Widgets/WidgetLayoutStoreV1.cs`, `WidgetLayoutCommandHandlerV1.cs`
- Tester: 3 nya test-filer, alla PASS

Nästa: W-B (Calculator + Väder + Notes som concrete widgets).
```

```bash
git add CURRENT_STATE.md
git commit -m "widget-v2: uppdatera CURRENT_STATE för W-A släpp"
```

---

## Verification

| Steg | Verifiering |
|---|---|
| Task 1 | Drag widget → snappar till cell, `tests/widget-snap-grid.test.js` PASS |
| Task 2 | Grid-overlay syns under drag, försvinner vid release |
| Task 3 | Klick på sidebar-ikon → widget skapas, `tests/widget-sidebar-ui.test.js` PASS |
| Task 4 | `dotnet build` 0 errors, `WidgetLayoutStoreV1` kompilerar |
| Task 5 | `tests/widget-layouts.test.js` PASS för router-checks |
| Task 6 | Build OK, `widget_layout_*`-message-types finns i Program.cs |
| Task 7 | JS-API-funktioner finns globalt på window |
| Task 8 | Top-row har "Layout ▾" + 💾, menu togglar |
| Task 9 | Keyboard shortcuts triggar rätt actions |
| Task 10 | Full smoke ALL PASS, manuell UI-verifiering OK |
