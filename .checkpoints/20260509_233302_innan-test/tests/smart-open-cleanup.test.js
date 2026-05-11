const fs = require("fs");
const path = require("path");

const programPath = path.join(__dirname, "..", "app", "Program.cs");
const source = fs.readFileSync(programPath, "utf8");

const forbidden = [
  "OpenProjectFileByNameEarlyV3Async",
  "OpenProjectFileSmartV4Async",
  "OpenProjectFileSmartV5Async",
  "OpenProjectFileSmartV6Async",
  "OpenProjectFileSmartV7Async",
  "ResolveProjectFileRequestV3",
  "ResolveProjectFileRequestV4",
  "ResolveProjectFileRequestV5",
  "ResolveProjectFileRequestV6",
  "ResolveProjectFileRequestV7",
  "TryExtractFileOpenEarlyV3",
  "TryExtractFileOpenCommandV6",
  "GetReadableProjectFilesForSmartOpenV3",
  "GetReadableProjectFilesForSmartOpenV4",
  "GetReadableProjectFilesForSmartOpenV5",
  "GetReadableProjectFilesForSmartOpenV6"
];

const required = [
  "OpenProjectFileSmartAsync",
  "ResolveProjectFileRequest",
  "TryExtractSmartOpenFileRequest"
];

let failures = 0;

for (const value of forbidden) {
  if (source.includes(value)) {
    failures += 1;
    console.log(`FAIL old smart-open duplicate remains: ${value}`);
  } else {
    console.log(`PASS old smart-open duplicate absent: ${value}`);
  }
}

for (const value of required) {
  if (!source.includes(value)) {
    failures += 1;
    console.log(`FAIL canonical smart-open marker missing: ${value}`);
  } else {
    console.log(`PASS canonical smart-open marker present: ${value}`);
  }
}

if (failures > 0) {
  process.exit(1);
}
