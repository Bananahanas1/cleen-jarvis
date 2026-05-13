using System.Text;

namespace JarvisClean;

internal sealed record ContextPackV1(
    string UserRequest,
    string MemoryContext,
    string ProjectContext,
    string TerminalContext,
    string TaskContext,
    string ActiveFileContext,
    string SafetyContext)
{
    internal const int MaxSectionChars = 5000;

    public string ToAdvisorSystemPrompt(string backendName)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Du ar Jarvis sprakmodell-radgivare via " + backendName + ".");
        builder.AppendLine("Svara pa svenska, kort och praktiskt.");
        builder.AppendLine("Modellen får inte köra tools, skriva filer, klicka, starta program eller kora terminal.");
        builder.AppendLine("Du får inte utföra lokala actions. Foresla bara svar, intent eller plan.");
        builder.AppendLine("Jarvis C# runtime ager sjalv genom CommandRouter, CommandValidator och PendingApproval.");
        builder.AppendLine();
        AppendSection(builder, "Sakerhetskontrakt", SafetyContext);
        AppendSection(builder, "Aktiv task/status", TaskContext);
        AppendSection(builder, "Aktiv fil", ActiveFileContext);
        AppendSection(builder, "Senaste terminal", TerminalContext);
        AppendSection(builder, "Projektkontext", ProjectContext);
        AppendSection(builder, "Lokalt minne", MemoryContext);
        return builder.ToString().Trim();
    }

    public static ContextPackV1 Build(
        string userRequest,
        string memoryContext,
        string projectContext,
        string terminalContext,
        string taskContext,
        string activeFileContext)
    {
        return new ContextPackV1(
            UserRequest: LimitSection(userRequest, 2000),
            MemoryContext: LimitSection(memoryContext, MaxSectionChars),
            ProjectContext: LimitSection(projectContext, MaxSectionChars),
            TerminalContext: LimitSection(terminalContext, 3000),
            TaskContext: LimitSection(taskContext, 2500),
            ActiveFileContext: LimitSection(activeFileContext, 2500),
            SafetyContext: "Lokala kommandon fore LLM. Riskabla actions kraver pending preview och anvandarens godkannande. F:\\New project ar read-only. Inga secrets ska skickas eller loggas.");
    }

    private static void AppendSection(StringBuilder builder, string title, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        builder.AppendLine("## " + title);
        builder.AppendLine(value.Trim());
        builder.AppendLine();
    }

    internal static string LimitSection(string? value, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = value.Trim();
        if (trimmed.Length <= maxChars)
            return trimmed;

        return trimmed[..Math.Max(0, maxChars - 80)] + "\n\n[ContextPackV1: avkortat for att halla kontexten liten]";
    }
}
