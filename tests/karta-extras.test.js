const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const dashboard = fs.readFileSync(path.join(root, "dashboard", "index.html"), "utf8");

let failures = 0;
function check(name, condition) {
  if (!condition) { failures += 1; console.log("FAIL " + name); }
  else { console.log("PASS " + name); }
}

// --- POI-klick (fix 2026-05-14) ---
check("Karta has POI popup renderer", dashboard.includes("_kartaRenderPoiHtml"));
check("Karta queries Overpass API", dashboard.includes("overpass-api.de/api/interpreter"));
check("Karta fetches Wikidata image", dashboard.includes("_kartaFetchWikidataImageV1"));
check("Karta detects POI from tile features", dashboard.includes("namedFeature") && dashboard.includes("_kartaOpenPoiPanelV2"));
check("POI popup CSS exists", dashboard.includes(".karta-poi-popup"));
check("POI links to Google Maps as fallback for reviews",
  dashboard.includes("google.com/maps/search"));
check("Dblclick triggers POI/note flow",
  /_kartaMap\.on\("dblclick"/.test(dashboard));
check("Single-click on existing marker removes it",
  /el\.addEventListener\("click",\s*function/.test(dashboard) &&
  /marker\.remove\(\)/.test(dashboard));

// --- Väder-overlay ---
check("Karta has weather toggle button", dashboard.includes('id="kartaToggleWeatherBtn"'));
check("Karta uses open-meteo for weather", dashboard.includes("api.open-meteo.com/v1/forecast"));
check("Karta has rain animation CSS", dashboard.includes("weatherDropFall"));
check("Karta has snow animation CSS", dashboard.includes("weatherFlakeFall"));
check("Karta has lightning animation CSS", dashboard.includes("weatherLightning"));
check("Weather overlay reads WMO codes", dashboard.includes("_kartaWmoToEffect"));

// --- Flyg + båtar visible-error reporting ---
check("Flights show error in chat (not silent warn only)",
  /Flygplan-fel:/.test(dashboard));
check("Flights use airplanes.live as primary (no auth)",
  dashboard.includes("airplanes.live"));
check("Flights default interval is 10s (no rate-limit on airplanes.live)",
  /_kartaFetchFlights,\s*10000/.test(dashboard));
check("Ships announce first-seen message",
  dashboard.includes("_kartaShipFirstSeen") &&
  dashboard.includes("första båten mottagen"));
check("Ships resend bbox on map move",
  dashboard.includes("_kartaResendShipBounds") &&
  dashboard.includes("_kartaMaybeResendShipBoundsV1"));

// --- Aktiva lager-indikatorer ---
check("All layer-toggles sync .is-active class",
  dashboard.includes("kartaToggleWeatherBtn: !!_kartaWeatherOn") &&
  dashboard.includes("kartaToggleShipsBtn: !!_kartaShipsOn"));

// --- POI-panel V2 (slide-in sidopanel istället för popup) ---
check("POI sidopanel finns i DOM", dashboard.includes('id="kartaPoiPanel"'));
check("POI-panel öppnas via _kartaOpenPoiPanelV2", dashboard.includes("function _kartaOpenPoiPanelV2"));
check("POI fetchar Commons-bilder för galleri", dashboard.includes("_kartaFetchCommonsImagesV1"));
check("POI fetchar Wikipedia-extract", dashboard.includes("_kartaFetchWikipediaSummaryV1"));
check("POI visar nearby POIs (150m radius)", dashboard.includes("_kartaQueryNearbyOsmV1"));
check("POI visar väder vid platsen", dashboard.includes("_kartaFetchPoiWeatherV1"));
check("POI close-knapp wired", dashboard.includes("kartaPoiCloseBtn"));

// --- Live weather HUD (ingen chat-spam) ---
check("Weather HUD finns + uppdaterar live", dashboard.includes("_kartaShowWeatherHud"));
check("Weather: ingen chat-spam vid varje fetch",
  !/_kartaFetchWeatherForCenter[\s\S]{0,500}?jarvisAddMessage\([\s\S]{0,80}?Väder vid kart-mitten/.test(dashboard));
check("Weather uppdateras vid map moveend (debounced)",
  /_kartaMap\.on\("moveend"[\s\S]{0,400}?_kartaFetchWeatherForCenter/.test(dashboard));

// --- Sidopanel-toggles + reopen-tabs ---
check("Reopen-tabs i DOM", dashboard.includes('id="reopenLeftTab"') && dashboard.includes('id="reopenRightTab"'));
check("Grid använder enkel 1fr (ingen minmax) när kollapsad",
  /body\.hide-left\.hide-right \.layout\s*\{\s*grid-template-columns:\s*0 1fr 0/.test(dashboard));
check(".editor har position:relative som anchor för mapPanel",
  /\.editor\s*\{[^}]*position:\s*relative/.test(dashboard));
check("Grid items har min-width:0 (förhindrar overflow-bug)",
  /\.explorer,\s*\.editor,\s*\.chat\s*\{[^}]*min-width:\s*0/.test(dashboard));

if (failures > 0) {
  console.log("\nKarta extras — " + failures + " check(s) failed.");
  process.exit(1);
}
console.log("\nKarta extras — all checks passed.");
