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
check("Karta detects POI from tile features", dashboard.includes("namedFeature") && dashboard.includes("_kartaOpenPoiPopupV1"));
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
  /Flygplan-fel från OpenSky/.test(dashboard));
check("Flights auto-backoff to 60s on rate-limit",
  dashboard.includes("_kartaFlightConsecutiveErrors === 3") &&
  dashboard.includes("60000"));
check("Flights default interval is 15s (rate-limit safe)",
  /_kartaFetchFlights,\s*15000/.test(dashboard));
check("Ships announce first-seen message",
  dashboard.includes("_kartaShipFirstSeen") &&
  dashboard.includes("första båten mottagen"));
check("Ships resend bbox on map move",
  dashboard.includes("_kartaResendShipBounds") &&
  dashboard.includes("_kartaShipBoundsTimer"));

// --- Aktiva lager-indikatorer ---
check("All layer-toggles sync .is-active class",
  dashboard.includes("kartaToggleWeatherBtn: !!_kartaWeatherOn") &&
  dashboard.includes("kartaToggleShipsBtn: !!_kartaShipsOn"));

// --- Sidopanel-toggles + reopen-tabs ---
check("Reopen-tabs in DOM", dashboard.includes('id="reopenLeftTab"') && dashboard.includes('id="reopenRightTab"'));

if (failures > 0) {
  console.log("\nKarta extras — " + failures + " check(s) failed.");
  process.exit(1);
}
console.log("\nKarta extras — all checks passed.");
