// Verifierar att InternetProbe har cache och offline-helpers enligt OFFLINE_PLAN.

const fs = require("fs");
const path = require("path");

const csharp = fs.readFileSync(
  path.join(__dirname, "..", "app", "Program.cs"),
  "utf8"
);

let failures = 0;

const markers = [
  "IsInternetOnlineCachedAsync",
  "OfflineSkipMessageOrEmptyAsync",
  "InternetProbeCacheTtlSeconds",
  "_lastInternetProbeAt",
  "_lastInternetProbeResult",
  "_inflightInternetProbe",
  "ProbeTcpAsync(\"1.1.1.1\", 443, 800)"
];

for (const m of markers) {
  if (!csharp.includes(m)) {
    failures += 1;
    console.log(`FAIL Program.cs missing: ${m}`);
  } else {
    console.log(`PASS Program.cs has: ${m}`);
  }
}

// Verify the cache TTL is exactly 30 seconds (per OFFLINE_PLAN spec).
if (!csharp.includes("InternetProbeCacheTtlSeconds = 30")) {
  failures += 1;
  console.log("FAIL InternetProbeCacheTtlSeconds must be 30");
} else {
  console.log("PASS InternetProbeCacheTtlSeconds is 30");
}

// Verify timeout is 800ms (per OFFLINE_PLAN spec).
if (!csharp.includes("ProbeTcpAsync(\"1.1.1.1\", 443, 800)")) {
  failures += 1;
  console.log("FAIL Probe target must be 1.1.1.1:443 with 800ms timeout");
} else {
  console.log("PASS Probe target is 1.1.1.1:443 / 800ms");
}

// Verify the offline-skip message is in Swedish (consistent with project).
if (!csharp.includes("\"Internet saknas just nu, hoppar över.\"")) {
  failures += 1;
  console.log("FAIL OfflineSkipMessage Swedish text missing");
} else {
  console.log("PASS OfflineSkipMessage uses Swedish text");
}

// Verify thread safety: lock object exists.
if (!csharp.includes("_internetProbeLock = new()")) {
  failures += 1;
  console.log("FAIL Internet probe must use lock for thread safety");
} else {
  console.log("PASS Internet probe uses lock");
}

// Verify in-flight coalescing (multiple concurrent callers share one probe task).
if (!csharp.includes("_inflightInternetProbe")) {
  failures += 1;
  console.log("FAIL In-flight coalescing missing");
} else {
  console.log("PASS In-flight coalescing present");
}

// Force-refresh path: forceRefresh: true must bypass cache (used by InternetStatusToolAsync).
if (!csharp.includes("IsInternetOnlineCachedAsync(forceRefresh: true)")) {
  failures += 1;
  console.log("FAIL InternetStatusToolAsync should call with forceRefresh: true");
} else {
  console.log("PASS InternetStatusToolAsync forces refresh");
}

if (failures > 0) {
  console.log(`\n${failures} failure(s)`);
  process.exit(1);
} else {
  console.log("\nAll internet-probe checks passed.");
}
