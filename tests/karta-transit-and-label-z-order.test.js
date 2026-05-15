const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const dashboard = fs.readFileSync(path.join(root, "dashboard", "index.html"), "utf8");
const programCs = fs.readFileSync(path.join(root, "app", "Program.cs"), "utf8");
const envVaultCs = fs.readFileSync(path.join(root, "app", "Bridges", "EnvVaultV1.cs"), "utf8");

let failures = 0;
function check(name, condition) {
  if (!condition) {
    failures += 1;
    console.log("FAIL " + name);
  } else {
    console.log("PASS " + name);
  }
}

// --- Trafiklab / Skånetrafiken transit-proxy (C#) ---
check("EnvVaultV1 whitelistar TRAFIKLAB_API_KEY", envVaultCs.includes("TRAFIKLAB_API_KEY"));
check("Program.cs routar karta_transit_departures", programCs.includes("\"karta_transit_departures\""));
check("Program.cs har HandleKartaTransitDeparturesAsync", programCs.includes("HandleKartaTransitDeparturesAsync"));
check("Transit-handler anropar ResRobot location.nearbystops",
  programCs.includes("api.resrobot.se/v2.1/location.nearbystops"));
check("Transit-handler anropar ResRobot departureBoard",
  programCs.includes("api.resrobot.se/v2.1/departureBoard"));
check("Transit-handler skickar resultat via window.jarvisKartaTransitResultV1",
  programCs.includes("jarvisKartaTransitResultV1"));
check("Transit-handler ger tydligt fel om nyckel saknas",
  programCs.includes("TRAFIKLAB_API_KEY saknas"));
check("Transit-handler söker stopId inom 250m",
  programCs.includes("r=250"));

// --- Dashboard: callback-tabell och bus-stop-detektion ---
check("Dashboard har _kartaTransitCallbacks", dashboard.includes("_kartaTransitCallbacks"));
check("Dashboard registrerar window.jarvisKartaTransitResultV1",
  dashboard.includes("window.jarvisKartaTransitResultV1 = function"));
check("Dashboard postar karta_transit_departures",
  dashboard.includes("type: \"karta_transit_departures\""));
check("_kartaIsBusOrTransitStop finns och täcker public_transport",
  dashboard.includes("_kartaIsBusOrTransitStop") && dashboard.includes("public_transport"));
check("_kartaIsBusOrTransitStop täcker highway=bus_stop",
  dashboard.includes("highway === \"bus_stop\""));
check("_kartaIsBusOrTransitStop täcker railway tram/halt/station",
  dashboard.includes("tram_stop") && dashboard.includes("\"halt\""));

// Render: transit-sektion i POI-panel
check("POI-panel renderar transitSection",
  dashboard.includes("transitSection"));
check("POI-panel-render lägger transitSection i body.innerHTML",
  dashboard.includes("infoSection + transitSection"));
check("Transit-loading-state rendereras",
  dashboard.includes("Hämtar tidtabell från Skånetrafiken"));
check("Transit-row-CSS finns",
  dashboard.includes(".poi-transit-row") && dashboard.includes(".poi-transit-line"));

// --- Label-z-order ovanför 3D-byggnader ---
check("_kartaRaiseLabelsAboveBuildings finns",
  dashboard.includes("_kartaRaiseLabelsAboveBuildings"));
check("Label-höjning kallas efter Add3dBuildings",
  /addLayer\([^]*3d-buildings[^]*?_kartaRaiseLabelsAboveBuildings\(\)/.test(dashboard));
check("Label-höjning körs på styledata-event",
  dashboard.includes("_kartaMap.on(\"styledata\"")
  && /styledata[^]{0,200}_kartaRaiseLabelsAboveBuildings/.test(dashboard));
check("Label-höjning flyttar symbol-lager",
  dashboard.includes("layer.type === \"symbol\"")
  && dashboard.includes("moveLayer(layer.id)"));
check("Label-höjning flyttar 3d-buildings UNDER första symbol-lagret",
  dashboard.includes("moveLayer(\"3d-buildings\", firstSymbolAfter.id)"));

// --- TomTom Traffic Flow overlay ---
check("EnvVaultV1 whitelistar TOMTOM_API_KEY", envVaultCs.includes("TOMTOM_API_KEY"));
check("Program.cs routar karta_get_tomtom_key", programCs.includes("\"karta_get_tomtom_key\""));
check("Program.cs har SendKartaTomTomKeyAsync", programCs.includes("SendKartaTomTomKeyAsync"));
check("Traffic-key skickas via jarvisKartaApplyTomTomKeyV1",
  programCs.includes("jarvisKartaApplyTomTomKeyV1"));
check("Dashboard har trafik-knapp", dashboard.includes("kartaToggleTrafficBtn"));
check("Dashboard cachar TomTom-nyckeln via apply-callback",
  dashboard.includes("window.jarvisKartaApplyTomTomKeyV1 = function"));
check("Dashboard har _kartaToggleTraffic", dashboard.includes("_kartaToggleTraffic"));
check("Dashboard postar karta_get_tomtom_key vid init",
  dashboard.includes("type: \"karta_get_tomtom_key\""));
check("Trafik-toggle ger tydligt fel om nyckel saknas",
  dashboard.includes("Live-trafik kräver TOMTOM_API_KEY"));
check("Trafik-tile är TomTom flow relative0",
  dashboard.includes("api.tomtom.com/traffic/map/4/tile/flow/relative0"));
check("Trafiklagret läggs FÖRE första symbol-lagret",
  /id:\s*_KARTA_TRAFFIC_LAYER_ID[\s\S]*?\}\s*,\s*beforeId\)/.test(dashboard));
check("Trafik-overlay återskapas vid style.load",
  /style\.load[\s\S]{0,600}_kartaTrafficOn[\s\S]{0,80}_kartaAddTrafficLayer\(\)/.test(dashboard));
check("Trafik-button syncas i _kartaSyncLayerButtonsV1",
  dashboard.includes("kartaToggleTrafficBtn: !!_kartaTrafficOn"));
check("Trafik-tiles refreshas 60s via setTiles",
  dashboard.includes("setTiles([_kartaTrafficTileUrl()])"));

if (failures > 0) {
  console.log("\nKarta transit + label z-order checks failed: " + failures);
  process.exit(1);
}
console.log("\nKarta transit + label z-order checks passed.");
