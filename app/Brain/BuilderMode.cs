using System.Text;
using System.Text.RegularExpressions;

namespace JarvisClean;

internal sealed class BuilderSessionV1
{
    public required string Slug { get; init; }
    public required string Description { get; init; }
    public required string Questions { get; set; }
    public required string CreatedAt { get; init; }
    public List<string> Answers { get; } = new();
}

internal static class BuilderMode
{
    private static readonly object Lock = new();
    private static BuilderSessionV1? CurrentSession;

    public static bool HasActiveSession
    {
        get { lock (Lock) return CurrentSession is not null; }
    }

    public static BuilderSessionV1? Current
    {
        get
        {
            lock (Lock)
            {
                if (CurrentSession is null)
                    return null;

                var copy = new BuilderSessionV1
                {
                    Slug = CurrentSession.Slug,
                    Description = CurrentSession.Description,
                    Questions = CurrentSession.Questions,
                    CreatedAt = CurrentSession.CreatedAt
                };
                copy.Answers.AddRange(CurrentSession.Answers);
                return copy;
            }
        }
    }

    public static BuilderSessionV1 Start(string description, string questions)
    {
        var session = new BuilderSessionV1
        {
            Slug = DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + SanitizeSlug(description),
            Description = description.Trim(),
            Questions = string.IsNullOrWhiteSpace(questions) ? FallbackQuestions(description) : questions.Trim(),
            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        lock (Lock)
            CurrentSession = session;

        return session;
    }

    public static bool AddAnswer(string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return false;

        lock (Lock)
        {
            if (CurrentSession is null)
                return false;

            CurrentSession.Answers.Add(answer.Trim());
            return true;
        }
    }

    public static void Clear()
    {
        lock (Lock)
            CurrentSession = null;
    }

    public static string PlanRelativePath(BuilderSessionV1 session) =>
        "vault/builds/" + session.Slug + "/PLAN.md";

    public static string BuildQuestionPrompt(string description)
    {
        return
            "Användaren vill bygga detta:\n" +
            description.Trim() + "\n\n" +
            "Ställ 3-5 specifika klargörande frågor på svenska.\n" +
            "Frågorna ska hjälpa Jarvis bygga rätt sak senare.\n" +
            "Returnera bara frågorna som en kort numrerad lista.";
    }

    public static string BuildPlanPrompt(BuilderSessionV1 session)
    {
        var answers = session.Answers.Count == 0
            ? "(Inga svar ännu. Gör en försiktig MVP-plan baserat på idén.)"
            : string.Join("\n", session.Answers.Select((answer, index) => (index + 1) + ". " + answer));

        return
            "Skapa en konkret svensk implementationplan för Jarvis BuilderMode.\n\n" +
            "Originalidé:\n" + session.Description + "\n\n" +
            "Frågor Jarvis ställde:\n" + session.Questions + "\n\n" +
            "Användarens svar:\n" + answers + "\n\n" +
            "Planen ska innehålla:\n" +
            "- Kort målbild\n" +
            "- Föreslagen fil-lista\n" +
            "- Arkitektur/komponenter\n" +
            "- Säkerhetsnoteringar\n" +
            "- Stegvis byggordning\n\n" +
            "Viktigt: skriv inga filer, kör inget, och anta att varje framtida filskapande måste gå via PendingApproval.";
    }

    public static string BuildPlanMarkdown(BuilderSessionV1 session, string generatedPlan)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine("type: build-plan");
        sb.AppendLine("created: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("source: BuilderMode");
        sb.AppendLine("tags: [builder, plan, pending]");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# Builder plan");
        sb.AppendLine();
        sb.AppendLine("## Originalidé");
        sb.AppendLine();
        sb.AppendLine(session.Description);
        sb.AppendLine();
        sb.AppendLine("## Frågor");
        sb.AppendLine();
        sb.AppendLine(session.Questions);
        sb.AppendLine();
        sb.AppendLine("## Användarens svar");
        sb.AppendLine();
        if (session.Answers.Count == 0)
        {
            sb.AppendLine("(Inga svar sparade ännu.)");
        }
        else
        {
            foreach (var (answer, index) in session.Answers.Select((answer, index) => (answer, index)))
                sb.AppendLine((index + 1) + ". " + answer);
        }
        sb.AppendLine();
        sb.AppendLine("## Föreslagen plan");
        sb.AppendLine();
        sb.AppendLine(CleanMarkdown(generatedPlan));
        sb.AppendLine();
        sb.AppendLine("## Nästa steg");
        sb.AppendLine();
        sb.AppendLine("- Granska planen.");
        sb.AppendLine("- Godkänn pending file-create om planen ska sparas i vault.");
        sb.AppendLine("- Nästa BuilderMode-fas får skapa filer stegvis via PendingApproval, aldrig i ett stort svep.");
        return sb.ToString().TrimEnd();
    }

    public static string Status()
    {
        var session = Current;
        if (session is null)
            return "BuilderMode: ingen aktiv builder-session. Starta med: /bygg <beskrivning>";

        return
            "BuilderMode aktiv:\n" +
            "- Idé: " + session.Description + "\n" +
            "- Slug: " + session.Slug + "\n" +
            "- Svar sparade: " + session.Answers.Count + "\n" +
            "- Planfil: " + PlanRelativePath(session) + "\n\n" +
            "Kommandon:\n" +
            "- /bygg svar <dina svar>\n" +
            "- /bygg plan\n" +
            "- /bygg avbryt";
    }

    public static string FallbackQuestions(string description)
    {
        return
            "1. Vilket är minsta fungerande resultat du vill testa först?\n" +
            "2. Ska det vara en sida, ett verktyg, en app eller en Jarvis-funktion?\n" +
            "3. Vilka filer eller delar av projektet ska den helst påverka?\n" +
            "4. Finns det något Jarvis absolut inte får ändra?\n" +
            "5. Hur vill du verifiera att det fungerar?";
    }

    public static string SanitizeSlug(string raw)
    {
        var normalized = Regex.Replace((raw ?? "").ToLowerInvariant(), @"[^a-z0-9åäö]+", "-").Trim('-');
        normalized = normalized
            .Replace("å", "a")
            .Replace("ä", "a")
            .Replace("ö", "o");

        if (string.IsNullOrWhiteSpace(normalized))
            normalized = "build";

        if (normalized.Length > 48)
            normalized = normalized[..48].Trim('-');

        return normalized;
    }

    private static string CleanMarkdown(string raw)
    {
        var text = (raw ?? "").Trim();
        var fenced = Regex.Match(text, @"^```(?:md|markdown)?\s*\n(?<body>[\s\S]*?)\n```\s*$", RegexOptions.IgnoreCase);
        return fenced.Success ? fenced.Groups["body"].Value.Trim() : text;
    }
}
