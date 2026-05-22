# Widget V2 — UI-shell (W-A) Design

**Datum:** 2026-05-22
**Sub-projekt:** W-A av Widget V2 (W-A = shell, W-B = lokala widgets, W-C = Spotify)
**Status:** Design godkänd 2026-05-22, redo för writing-plans

## Context

Sprint 3 (2026-05-17) levererade `JarvisWidgetsV1` — floating, draggable, resizable
widgets med 7 typer (image/iframe/webcam/video/text/chat-mini/html) och `composeScene()`
med 6×4 sub-grid för auto-layout. Manuell drag är fri (ingen snap) och alla widgets är
generiska — inga concrete data-widgets (Spotify/Calc/Väder) finns.

W-A bygger ut shell:et med snap-grid, widget-bibliotek-sidebar, multi-layout-slots
och keyboard shortcuts. Detta är load-bearing: W-B och W-C bygger på W-A:s grid-API.

## Hårda krav

- Inga API-nycklar i repo. Layouts är harmless JSON, sparas i `data/widgets/layouts.json`
  (gitignored via `data/`-pattern).
- All file-write är user-driven (klick på "Save layout"), ingen LLM-bypass.
- Backward-compat: existerande widgets med pixel-positioner i localStorage konverteras
  till grid-celler vid första load.
- `dotnet build app/JarvisClean.csproj` måste passera med 0 errors.
- Alla MD-filer < 14 000 tecken.

## Arkitektur

```
┌──────────────────────────────────────────────────────────┐
│ Top-row: ... [Karta] [Inställn] [+ Widget ▾] [Layout ▾] │
├────┬─────────────────────────────────────────────┬───────┤
│ S  │  ┌─────┐  ┌─────┐                            │       │
│ i  │  │Calc │  │Wedr │   ┌── grid-overlay ──┐    │ Chat  │
│ d  │  └─────┘  └─────┘   │  syns under drag │    │       │
│ e  │  ┌──────────┐                                │       │
│ b  │  │  Notes   │   12 cols × 8 rows fluid       │       │
│ a  │  └──────────┘   snap till cell-gränser       │       │
│ r  │                                                │       │
└────┴─────────────────────────────────────────────┴───────┘
```

### Snap-grid model

- **12 kolumner × 8 rader** fluid (cell-bredd = (panelW − gaps) / 12)
- Varje widget: `gridX, gridY, gridW, gridH` (cell-koordinater, int)
- **Drag:** pixel-position visas live, vid `mouseup` snappa till närmaste cell
- **Resize:** snappa till cell-step (drag-handle bottom-right)
- **Grid-overlay:** rader + kolumner i opacity 0.15 cyan, visas **endast** under drag/resize
- **Backward-compat:** befintliga `LS_PREFIX + type`-pixel-positioner konverteras vid load:
  `gridX = floor(left / cellW)`, etc.

### Widget-bibliotek

**Sidebar** (vänster, 56px bred, collapsable via knapp i top-row):
- Ikoner per generisk typ (image/iframe/webcam/video/text/chat-mini/html)
- Ikoner per concrete widget från W-B/W-C när installerade
- Klick på ikon → skapa widget i nästa lediga grid-cell
- Drag från ikon → fly-out preview, släpp på grid-cell

**Dropdown** (alternativ): `[+ Widget ▾]` i top-row med samma meny.

### Multi-layout-system

`data/widgets/layouts.json`:

```json
[
  {
    "id": "default",
    "name": "Default",
    "widgets": []
  },
  {
    "id": "work",
    "name": "Work",
    "widgets": [
      {"type": "calc", "gridX": 0, "gridY": 0, "gridW": 3, "gridH": 2},
      {"type": "notes", "gridX": 3, "gridY": 0, "gridW": 4, "gridH": 4}
    ]
  }
]
```

Switcher: `[Layout: Work ▾]` i top-row. Meny: lista + "Save current" + "+ New layout".

Predefined layouts vid första boot: **Default**, **Work** (calc + notes), **Play**
(stub för W-C Spotify), **Brief** (stub för W-B väder + chat-mini).

### Keyboard shortcuts

| Key | Action |
|---|---|
| `Ctrl+W` | Close focused widget |
| `Ctrl+M` | Minimera/återställ focused |
| `Ctrl+Shift+L` | Öppna layout-meny |
| `Esc` | Unfocus alla widgets |
| `Tab` | Cycla focus mellan widgets |

## Filer som skapas

| Fil | Syfte |
|---|---|
| `app/Widgets/WidgetLayoutStoreV1.cs` | Load/save `data/widgets/layouts.json` med id/name/widgets-array |
| `app/Widgets/WidgetCommandHandlerV1.cs` | Pure handlers för `/widget save/load/list/delete <name>` |
| `tests/widget-snap-grid.test.js` | Snap-funktion träffar cell-gränser inom 1px |
| `tests/widget-layouts.test.js` | Save/load layout-flöde med fixture-JSON |
| `tests/widget-sidebar-ui.test.js` | HTML-ids: `#widgetSidebar`, `#widgetSidebarToggle`, `#widgetDropdownBtn`, `#layoutSwitcherBtn` |
| `data/widgets/layouts.json` | Default-layout vid första boot (skapas runtime) |

## Filer som ändras

| Fil | Ändring |
|---|---|
| `dashboard/widgets-v1.js` | Lägg till `gridX/Y/W/H`-model + snap-funktion + sidebar-mountpoint API + layout-API |
| `dashboard/widgets-v1.css` | Grid-overlay (`.widget-grid-overlay`) + sidebar (`#widgetSidebar`) styles |
| `dashboard/index.html` | Sidebar HTML, `+ Widget`-dropdown, `Layout`-switcher i top-row, keyboard event listener |
| `app/CommandRouterV1.cs` | `CommandIntent.WidgetLayoutSave/Load/List/Delete` + slash + NL-parsning |
| `app/CommandValidatorV1.cs` | Acceptera nya intents som `SafeUi` |
| `app/Program.cs` | Dispatch nya intents + `widget_layout_save/load/list/delete` message-handlers |

## Reused patterns

| Pattern | Source | Used by |
|---|---|---|
| Singleton + lock + JSON file-store | `app/Tasks/TaskStoreV1.cs` | `WidgetLayoutStoreV1` |
| Pure command-handler | `app/Audio/VoiceCommandHandlerV1.cs` | `WidgetLayoutCommandHandlerV1` |
| Slash + NL pattern i router | `app/CommandRouterV1.cs` voice/scene-blocken | New layout-block |
| Dashboard ↔ C# bridge via postMessage | `dashboard/index.html` voice/settings-flöden | `widget_layout_*` |

## Slash commands + NL

| Slash | NL | Intent |
|---|---|---|
| `/widget save <name>` | `spara layout som X`, `lagra widget-layout` | `WidgetLayoutSave` |
| `/widget load <name>` | `byt till layout X`, `ladda widget-layout X` | `WidgetLayoutLoad` |
| `/widget list` | `visa layouts`, `lista widget-layouts` | `WidgetLayoutList` |
| `/widget delete <name>` | `ta bort layout X` | `WidgetLayoutDelete` |

Alla `Risk = SafeUi`, `ShouldSendToOllama = false`.

## Step-by-step ordning

Varje steg oberoende committable:

1. Snap-grid model + drag/resize snap i `widgets-v1.js`
2. Grid-overlay CSS + show-on-drag/resize
3. Widget-sidebar HTML + ikoner + drag-from-sidebar
4. `+ Widget`-dropdown i top-row (snabbalternativ)
5. `WidgetLayoutStoreV1.cs` + 4 message-handlers
6. Layout-switcher dropdown + Save/Load UI
7. Slash + NL i router + dispatch i Program.cs
8. Keyboard shortcuts + Tab-cycling
9. Tester + build + publish

## Verification

| Steg | Verifiering |
|---|---|
| 1 | Drag widget → release → position är exakt på cell-gräns |
| 2 | Grid-overlay syns under drag, försvinner vid release |
| 3 | Klick på sidebar-ikon → widget syns på första lediga cell |
| 4 | `+ Widget`-dropdown listar alla typer + skapar widget |
| 5 | `dotnet build` 0 errors, fil `data/widgets/layouts.json` skapas |
| 6 | "Save current layout" + "Load X" funkar end-to-end |
| 7 | `/widget save test` → `/widget load test` → widgets återställs |
| 8 | Ctrl+W stänger focused widget, Tab cyclar |
| 9 | `node tests/widget-*.test.js` PASS + full smoke ALL PASS |

## Risks & open questions

1. **Grid-overlay performance** — om grid renderas via många DOM-element kan det bli
   slow vid drag. Mitigation: använd CSS-gradient eller en enda absolute-positioned canvas.
2. **Backward-compat conversion** — om gamla pixel-positioner är utanför nuvarande
   panelW (smal skärm), kan widget hamna utanför grid. Mitigation: clamp till 0..11 / 0..7.
3. **Sidebar kollision med Project Explorer** — Project Explorer är redan på vänster
   sida. Mitigation: widget-sidebar är ovanpå (overlay) och bara synlig i scen/karta-mode.
4. **Layout JSON-corruption** — om användaren manuellt redigerar layouts.json fel, kan
   load failar. Mitigation: try/catch → ladda Default + visa toast.
5. **Tab-cycling i WebView2** — `Tab` är default browser-tab-fokus. Mitigation:
   `preventDefault` på keydown när widget är focused.

## Deferred (V2.1+)

- Widget-templates marketplace (community-uploaded layouts)
- Auto-layout-detect baserat på tid på dygnet (morning = Brief, evening = Play)
- Drag mellan grid-positioner med live-preview av andra widgets som flyttar undan
- Magnetic snap till andra widget-kanter (inte bara grid-celler)
- Layout-export/import som JSON-fil

## Critical files for implementation

- [dashboard/widgets-v1.js](dashboard/widgets-v1.js) — uppgradera till grid-model
- [dashboard/widgets-v1.css](dashboard/widgets-v1.css) — grid-overlay + sidebar
- [dashboard/index.html](dashboard/index.html) — HTML + JS-handlers
- [app/CommandRouterV1.cs](app/CommandRouterV1.cs) — enum + parse
- [app/Program.cs](app/Program.cs) — message-handlers + intent-dispatch
- [app/Tasks/TaskStoreV1.cs](app/Tasks/TaskStoreV1.cs) — *pattern reference* för store
- [app/Audio/VoiceCommandHandlerV1.cs](app/Audio/VoiceCommandHandlerV1.cs) — *pattern reference* för handler
