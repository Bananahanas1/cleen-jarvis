const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const dashboard = fs.readFileSync(path.join(root, "dashboard", "index.html"), "utf8");
const themePath = path.join(root, "dashboard", "theme.css");
const sceneCssPath = path.join(root, "dashboard", "scene-pro.css");
const theme = fs.readFileSync(themePath, "utf8");
const sceneCss = fs.readFileSync(sceneCssPath, "utf8");

let failures = 0;
function check(name, condition) {
  if (!condition) {
    failures += 1;
    console.log("FAIL " + name);
  } else {
    console.log("PASS " + name);
  }
}

check("dashboard links theme.css", dashboard.includes('href="theme.css"'));
check("dashboard links scene-pro.css", dashboard.includes('href="scene-pro.css"'));
check("theme.css exists", fs.existsSync(themePath));
check("scene-pro.css exists", fs.existsSync(sceneCssPath));

check("theme exposes Jarvis design tokens", [
  "--jarvis-bg-0",
  "--jarvis-cyan",
  "--jarvis-panel",
  "--jarvis-glow-md",
  "--jarvis-grid-line"
].every((token) => theme.includes(token)));

check("scenePanel still exists", dashboard.includes('id="scenePanel"'));
check("sceneStack still exists", dashboard.includes('id="sceneStack"'));
check("sceneHeroSlot still exists", dashboard.includes('id="sceneHeroSlot"'));
check("sceneSummarySlot still exists", dashboard.includes('id="sceneSummarySlot"'));
check("sceneVideoSlot still exists", dashboard.includes('id="sceneVideoSlot"'));
check("sceneSourcesSlot still exists", dashboard.includes('id="sceneSourcesSlot"'));

check("pro idle screen exists", dashboard.includes('id="sceneIdleScreen"'));
check("central Jarvis orb exists", dashboard.includes('id="sceneJarvisOrb"'));
check("idle screen has command feed", dashboard.includes("scene-command-feed"));
check("idle screen has system widgets", dashboard.includes("scene-system-grid"));
check("idle screen has tool dock", dashboard.includes("scene-tool-dock"));
check("tool dock includes all requested tool labels",
  ["Chat", "Camera", "Map", "Media", "Files", "Terminal"].every((label) => dashboard.includes(`>${label}<`) || dashboard.includes(`title="${label}"`)));

check("scene-pro CSS is scoped to scenePanel or scene classes",
  !/(^|\n)\s*(\.explorer|\.chat|\.editor|button|body\s*\{)/.test(sceneCss));
check("scene-pro CSS defines grid background", sceneCss.includes("linear-gradient(rgba(106, 217, 255"));
check("scene-pro CSS defines orb pulse animation", sceneCss.includes("@keyframes sceneOrbPulse"));
check("scene-pro CSS defines lightweight sweep animation", sceneCss.includes("@keyframes sceneProSweep"));
check("scene-pro CSS keeps responsive layout", sceneCss.includes("@media (max-width: 1050px)"));
check("scene-pro CSS avoids WebView2-risky backdrop filters", !sceneCss.includes("backdrop-filter"));
check("scene-pro CSS avoids fixed pseudo overlay compositor layer", !/scenePanel::before[\s\S]{0,160}position:\s*fixed/.test(sceneCss));

check("existing scene payload renderer remains", dashboard.includes("window.jarvisApplyScenePayloadV1"));
check("existing scene summary updater remains", dashboard.includes("window.jarvisUpdateSceneSummaryV1"));
check("existing scene hero updater remains", dashboard.includes("window.jarvisUpdateSceneHeroV1"));

if (failures > 0) {
  console.log("\nScene Pro Phase 1 checks failed: " + failures);
  process.exit(1);
}

console.log("\nScene Pro Phase 1 checks passed.");
