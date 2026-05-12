// Guards the first Project Index + Background Jobs MVP slice.
// The point is architectural: long project analysis must live in app/Jobs,
// not as another large block in Program.cs.
const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const programPath = path.join(root, "app", "Program.cs");
const routerPath = path.join(root, "app", "CommandRouterV1.cs");
const jobsDir = path.join(root, "app", "Jobs");
const queuePath = path.join(jobsDir, "BackgroundJobQueueV1.cs");
const indexPath = path.join(jobsDir, "ProjectIndexServiceV1.cs");
const auditPath = path.join(jobsDir, "ProjectAuditServiceV1.cs");

const program = fs.readFileSync(programPath, "utf8");
const router = fs.readFileSync(routerPath, "utf8");

let failures = 0;
function check(label, ok) {
  if (!ok) {
    failures++;
    console.log("FAIL " + label);
  } else {
    console.log("PASS " + label);
  }
}

check("app/Jobs folder exists", fs.existsSync(jobsDir));
check("BackgroundJobQueueV1 exists", fs.existsSync(queuePath));
check("ProjectIndexServiceV1 exists", fs.existsSync(indexPath));
check("ProjectAuditServiceV1 exists", fs.existsSync(auditPath));

const queue = fs.existsSync(queuePath) ? fs.readFileSync(queuePath, "utf8") : "";
const index = fs.existsSync(indexPath) ? fs.readFileSync(indexPath, "utf8") : "";
const audit = fs.existsSync(auditPath) ? fs.readFileSync(auditPath, "utf8") : "";

check("queue can start project index jobs", queue.includes("StartProjectIndexJob"));
check("queue can start project audit jobs", queue.includes("StartProjectAuditJob"));
check("queue exposes status", queue.includes("FormatStatus"));
check("queue exposes cancel", queue.includes("Cancel"));
check("queue writes readable job log and result", queue.includes("log.md") && queue.includes("result.md"));
check("queue formats result paths for chat", queue.includes("FormatPathForChat"));
check("index service computes file hash", index.includes("SHA256"));
check("index service excludes generated folders", index.includes("\"bin\"") && index.includes("\"obj\"") && index.includes("\".git\"") && index.includes("\".nuget\""));
check("index service writes under data/project-index", index.includes("data") && index.includes("project-index"));
check("index service tracks incremental reuse", index.includes("ReusedFileCount") && index.includes("HashedFileCount"));
check("index service writes file summaries", index.includes("files") && index.includes("folders") && index.includes("search.jsonl"));
check("audit service writes markdown report", audit.includes("Jarvis Project Audit") && audit.includes("WriteAuditAsync"));

check("router has JobStatus intent", router.includes("JobStatus"));
check("router has JobList intent", router.includes("JobList"));
check("router has JobCancel intent", router.includes("JobCancel"));
check("router has ProjectIndexSearch intent", router.includes("ProjectIndexSearch"));
check("router has ProjectAudit intent", router.includes("ProjectAudit"));
check("router parses /jobb", router.includes("ParseJobSlashCommand"));
check("router starts project index", router.includes("project.index.start"));
check("router searches project index", router.includes("project.index.search"));
check("router starts project audit", router.includes("project.audit.start"));

check("Program.cs delegates job start to queue", program.includes("BackgroundJobQueueV1.StartProjectIndexJob"));
check("Program.cs delegates audit start to queue", program.includes("BackgroundJobQueueV1.StartProjectAuditJob"));
check("Program.cs delegates job status to queue", program.includes("BackgroundJobQueueV1.FormatStatus"));
check("Program.cs can search project index", program.includes("ProjectIndexSearchServiceV1.FormatSearchResults"));
check("Program.cs injects project index RAG context", program.includes("ProjectIndexSearchServiceV1.BuildContextPrefix"));
check("Program.cs mentions /jobb in help", program.includes("/jobb status"));

if (failures > 0) {
  console.log("\n" + failures + " failure(s)");
  process.exit(1);
}

console.log("\nBackground jobs architecture checks passed.");
