const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const program = fs.readFileSync(path.join(root, "app", "Program.cs"), "utf8");
const dashboard = fs.readFileSync(path.join(root, "dashboard", "index.html"), "utf8");
const sceneSvc = fs.readFileSync(path.join(root, "app", "Scene", "SceneServiceV1.cs"), "utf8");
const sceneResearchPath = path.join(root, "app", "Scene", "SceneResearchV1.cs");
const sceneResearch = fs.readFileSync(sceneResearchPath, "utf8");

let failures = 0;
function check(name, condition) {
  if (!condition) { failures += 1; console.log("FAIL " + name); }
  else { console.log("PASS " + name); }
}

// --- 1. SceneResearchV1 (Wikipedia + DDG Instant Answer JSON) ---
check("SceneResearchV1.cs exists", fs.existsSync(sceneResearchPath));
check("SceneResearchV1 has SearchAsync method", /public static async Task<List<SceneResearchHitV1>>\s+SearchAsync/.test(sceneResearch));
check("SceneResearchV1 calls Wikipedia opensearch API", sceneResearch.includes("/w/api.php?action=opensearch"));
check("SceneResearchV1 fetches Wikipedia summary REST", sceneResearch.includes("/api/rest_v1/page/summary/"));
check("SceneResearchV1 tries Swedish Wikipedia first", /TrySearchWikipediaAsync\(query,\s*"sv"/.test(sceneResearch));
check("SceneResearchV1 falls back to English Wikipedia", /TrySearchWikipediaAsync\(query,\s*"en"/.test(sceneResearch));
check("SceneResearchV1 uses DDG Instant Answer JSON", sceneResearch.includes("api.duckduckgo.com"));
check("SceneResearchV1 reads DDG AbstractURL", sceneResearch.includes("AbstractURL"));
check("SceneResearchV1 reads DDG RelatedTopics", sceneResearch.includes("RelatedTopics"));
check("SceneResearchV1 escapes query as URL", sceneResearch.includes("Uri.EscapeDataString(query)"));
check("SceneResearchV1 sets reasonable timeout", /TimeSpan\.FromSeconds\(\s*(?:[5-9]|1[0-5])\s*\)/.test(sceneResearch));
check("SceneResearchV1 fails silently (best-effort)", /catch\s*[\s\S]{0,80}?(?:best-effort|\{)/i.test(sceneResearch));
check("SceneResearchV1 dedupes URLs across sources", sceneResearch.includes("seen.Add") && sceneResearch.includes("HashSet"));
check("SceneResearchHitV1 record exposes Title/Snippet/Url",
  /record SceneResearchHitV1\(string Title, string Snippet, string Url/.test(sceneResearch));

// --- 2. HandleSceneShowAsync uses streaming flow ---
check("Program.cs calls SceneResearchV1.SearchAsync", program.includes("SceneResearchV1.SearchAsync"));
check("Program.cs renders skeleton scene FIRST",
  /jarvisShowScenePanelV1[\s\S]{0,400}?jarvisApplyScenePayloadV1[\s\S]{0,400}?SceneResearchV1\.SearchAsync/.test(program));
check("Program.cs awaits summary in parallel with research",
  program.includes("var searchTask = SceneResearchV1.SearchAsync") &&
  program.includes("var summaryTask = BuildSceneSummaryAsync"));
check("Program.cs streams source cards via jarvisAddSceneCardV1",
  program.includes("jarvisAddSceneCardV1"));
check("Program.cs updates summary via jarvisUpdateSceneSummaryV1",
  program.includes("jarvisUpdateSceneSummaryV1"));
check("Program.cs source card carries hit.Url as imageUrl",
  /type\s*=\s*"source"[\s\S]{0,200}?imageUrl\s*=\s*hit\.Url/.test(program));
// V3: hero-bild + TTS
check("Program.cs uses BuildSkeleton (V3 hero+summary skeleton)",
  program.includes("SceneServiceV1.BuildSkeleton(query)"));
check("Program.cs picks Wikipedia thumbnail as hero",
  /FirstOrDefault\(h => !string\.IsNullOrWhiteSpace\(h\.ThumbnailUrl\)\)/.test(program));
check("Program.cs calls jarvisUpdateSceneHeroV1", program.includes("jarvisUpdateSceneHeroV1"));
check("Program.cs reads scene summary aloud via TTS",
  program.includes("VoiceTtsServiceV1.SpeakIfEnabled(summaryText"));

// --- 3. SceneServiceV1 V3 layout ---
check("SceneServiceV1 has BuildSkeleton with hero + summary",
  /BuildSkeleton\([\s\S]{0,400}?new\("hero"[\s\S]{0,400}?new\("summary"/.test(sceneSvc));
check("SceneServiceV1 has Pollinations fallback URL builder",
  sceneSvc.includes("PollinationsFallbackUrl"));
check("SceneResearchHitV1 includes ThumbnailUrl",
  sceneResearch.includes("ThumbnailUrl"));

// --- 4. Dashboard streaming API ---
check("Dashboard exposes jarvisAddSceneCardV1", dashboard.includes("window.jarvisAddSceneCardV1"));
check("Dashboard exposes jarvisUpdateSceneSummaryV1", dashboard.includes("window.jarvisUpdateSceneSummaryV1"));
check("Dashboard exposes jarvisUpdateSceneHeroV1", dashboard.includes("window.jarvisUpdateSceneHeroV1"));
check("Dashboard renders source-type cards", dashboard.includes("_sceneRenderSource"));
check("Dashboard renders hero-type cards", dashboard.includes("_sceneRenderHero"));
check("Dashboard source card has open link button", /case "source"[\s\S]{0,80}?_sceneRenderSource/.test(dashboard));
check("Dashboard tags summary card with data-scene-role",
  dashboard.includes('dataset.sceneRole = "summary"') &&
  dashboard.includes('[data-scene-role="summary"]'));
check("Dashboard tags hero card with data-scene-role",
  dashboard.includes('card.dataset.sceneRole = "hero"'));
check("Dashboard applies skeleton shimmer when summary empty",
  dashboard.includes("_sceneApplySummaryShimmer") &&
  dashboard.includes("Bygger sammanfattning"));
check("Dashboard uses slot-based scene layout",
  dashboard.includes("sceneHeroSlot") &&
  dashboard.includes("sceneSummarySlot") &&
  dashboard.includes("sceneVideoSlot") &&
  dashboard.includes("sceneSourcesSlot"));

// --- 5. CSS for source + shimmer + hero ---
check("CSS has source-card styling", dashboard.includes(".scene-card.source"));
check("CSS has hero-card styling", dashboard.includes(".scene-card.hero"));
check("CSS has sceneShimmer keyframes", dashboard.includes("@keyframes sceneShimmer"));
check("CSS has scene-card.skeleton class", dashboard.includes(".scene-card.skeleton"));
check("scenePanel is vertically scrollable", /\#scenePanel\s*\{[^}]*overflow-y:\s*auto/.test(dashboard));

// --- 6. Sidopanel-toggles (fix 5b V2) ---
check("Dashboard has toggleLeftPanelBtn", dashboard.includes('id="toggleLeftPanelBtn"'));
check("Dashboard has toggleRightPanelBtn", dashboard.includes('id="toggleRightPanelBtn"'));
check("Dashboard has hide-left body class CSS",
  /body\.hide-left\s+\.explorer\s*\{\s*display:\s*none/.test(dashboard));
check("Dashboard has hide-right body class CSS",
  /body\.hide-right\s+\.chat\s*\{\s*display:\s*none/.test(dashboard));
check("Dashboard wires panel toggle helper", dashboard.includes("_setSidePanelHiddenV1"));
check("Dashboard has reopen tabs", dashboard.includes('id="reopenLeftTab"') && dashboard.includes('id="reopenRightTab"'));
check("Esc-key reopens hidden side panels", /Escape[\s\S]{0,200}?hide-left/.test(dashboard));

// --- 7. Sanity: no regression of existing scene API ---
check("Dashboard still exposes jarvisApplyScenePayloadV1", dashboard.includes("window.jarvisApplyScenePayloadV1"));
check("Dashboard still exposes jarvisShowScenePanelV1", dashboard.includes("window.jarvisShowScenePanelV1"));

// --- 7. Safety: SceneResearchV1 does not touch approval store ---
check("SceneResearchV1 does NOT import PendingApprovalStoreV1",
  !sceneResearch.includes("PendingApprovalStoreV1"));
check("SceneResearchV1 does NOT touch Bridges/Desktop namespaces",
  !sceneResearch.includes("Bridges.") && !sceneResearch.includes("Desktop."));

if (failures > 0) {
  console.log("\nScene Cinematic V2 — " + failures + " check(s) failed.");
  process.exit(1);
}
console.log("\nScene Cinematic V2 checks passed.");
