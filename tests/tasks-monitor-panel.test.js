const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const taskStorePath = path.join(root, "app", "Tasks", "TaskStoreV1.cs");
const routerPath = path.join(root, "app", "CommandRouterV1.cs");
const validatorPath = path.join(root, "app", "CommandValidatorV1.cs");
const approvalPath = path.join(root, "app", "PendingApprovalV1.cs");
const programPath = path.join(root, "app", "Program.cs");
const dashboardPath = path.join(root, "dashboard", "index.html");

const taskStore = fs.existsSync(taskStorePath) ? fs.readFileSync(taskStorePath, "utf8") : "";
const router = fs.readFileSync(routerPath, "utf8");
const validator = fs.readFileSync(validatorPath, "utf8");
const approval = fs.readFileSync(approvalPath, "utf8");
const program = fs.readFileSync(programPath, "utf8");
const dashboard = fs.readFileSync(dashboardPath, "utf8");

let failures = 0;
function check(label, ok) {
  if (!ok) {
    failures += 1;
    console.log("FAIL " + label);
  } else {
    console.log("PASS " + label);
  }
}

check("TaskStoreV1 exists", fs.existsSync(taskStorePath));
check("TaskStoreV1 stores JSON under data/tasks", taskStore.includes("data\", \"tasks\", \"tasks.json"));
check("TaskStoreV1 supports red orange blue priority", taskStore.includes("\"red\"") && taskStore.includes("\"orange\"") && taskStore.includes("\"blue\""));
check("TaskStoreV1 can add tasks", taskStore.includes("TaskItemV1 Add"));
check("TaskStoreV1 can complete tasks", taskStore.includes("bool Complete"));
check("TaskStoreV1 can search tasks", taskStore.includes("List<TaskItemV1> Search"));

check("router has task intents", router.includes("TaskAddRequest") && router.includes("TaskCompleteRequest") && router.includes("TaskSearch"));
check("router parses task slash commands", router.includes("ParseTaskSlashCommand"));
check("validator requires approval for task writes", validator.includes("Task-skrivning måste kräva pending") && validator.includes("Task-ändring måste kräva pending"));
check("pending approval has TaskChange type", approval.includes("TaskChange"));

check("program exposes task tools", program.includes("TaskAddRequestTool") && program.includes("TaskCompleteRequestTool") && program.includes("TaskListTool"));
check("program approves task changes through pending flow", program.includes("ApprovePendingTaskChangeTool"));
check("overview payload includes tasks", program.includes("[\"tasks\"]") && program.includes("[\"taskChange\"]"));
check("overview payload includes jobs and workNow", program.includes("[\"jobs\"]") && program.includes("[\"workNow\"]"));

check("dashboard has monitor mini agent", dashboard.includes("miniAgentV1") && dashboard.includes("mini-agent"));
check("dashboard shows jobs tasks and live work cells", dashboard.includes("visualJobsState") && dashboard.includes("visualTasksState") && dashboard.includes("visualWorkNowState"));
check("dashboard has quick action buttons", dashboard.includes("data-command=\"/projekt index\"") && dashboard.includes("data-command=\"/task\""));
check("dashboard sends quick commands through C# bridge", dashboard.includes("postMessage({ type: \"jarvis_chat\", text: command })"));
check("dashboard has visual task quick input", dashboard.includes("id=\"taskQuickInput\"") && dashboard.includes("id=\"taskQuickAddBtn\""));
check("dashboard task quick input uses pending task command", dashboard.includes("submitTaskQuickV1") && dashboard.includes("\"/task add \""));
check("dashboard has visual task priority buttons", dashboard.includes("data-priority=\"red\"") && dashboard.includes("data-priority=\"orange\"") && dashboard.includes("data-priority=\"blue\""));

if (failures > 0) {
  console.log(`\n${failures} task/monitor panel failure(s)`);
  process.exit(1);
}

console.log("\nTask and monitor panel checks passed.");
