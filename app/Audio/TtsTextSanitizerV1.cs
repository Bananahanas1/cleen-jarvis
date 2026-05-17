using System.Text;
using System.Text.RegularExpressions;

namespace JarvisClean;

/// <summary>
/// Sprint 3+ (2026-05-17) — saneringspipeline för text som ska läsas upp via TTS.
/// Behåller naturlig svenska men tar bort skräp som TTS-motorer läser bokstavligt:
///   - URLs (https://example.com)
///   - Markdown-symboler (#, *, _, ``)
///   - Emoji + symboler (🎙, →, ⇒, etc.)
///   - JSON-syntax ({...}, [...], "...")
///   - Tekniska brackets (`[...] `[ctx≈...]`)
///   - Multipla mellanslag / radbrytningar
/// </summary>
internal static class TtsTextSanitizerV1
{
    // Tidigare badge: "[lokal] [ctx≈1,4k tok, svar≈40 tok] "
    private static readonly Regex BadgePrefix =
        new(@"^\s*\[[^\]]+\]\s*(\[[^\]]+\]\s*)*", RegexOptions.Compiled);

    // Markdown-rubriker, bold, italic, code-block, inline code
    private static readonly Regex MarkdownHeading = new(@"^#{1,6}\s+", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex MarkdownBold = new(@"\*\*([^*]+)\*\*", RegexOptions.Compiled);
    private static readonly Regex MarkdownItalic = new(@"(?<!\w)[*_]([^*_\s][^*_]*[^*_\s]|\w)[*_](?!\w)", RegexOptions.Compiled);
    private static readonly Regex MarkdownCodeBlock = new(@"```[\s\S]*?```", RegexOptions.Compiled);
    private static readonly Regex MarkdownInlineCode = new(@"`[^`]+`", RegexOptions.Compiled);
    private static readonly Regex MarkdownLink = new(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled);
    private static readonly Regex MarkdownListBullet = new(@"^\s*[-*+]\s+", RegexOptions.Compiled | RegexOptions.Multiline);

    // URLs
    private static readonly Regex Url = new(@"https?://\S+|www\.\S+|\b\S+\.(?:com|org|net|se|io|dev|app)/\S*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // JSON-objekt och arrayer
    private static readonly Regex JsonObject = new(@"\{[^{}]*?(?:\{[^{}]*\}[^{}]*?)*\}", RegexOptions.Compiled);
    private static readonly Regex JsonArray = new(@"\[\s*(?:\""[^\""]*\"")(?:\s*,\s*\""[^\""]*\"")*\s*\]", RegexOptions.Compiled);

    // Decorativa pilar och dingbats som inte TTS:as
    private static readonly Regex DingbatsAndArrows = new(@"[←-⇿✀-➿☀-⛿]", RegexOptions.Compiled);

    // Emoji-range (vanligaste) — täcker de flesta utan att slå sönder bokstäver med diakritik
    private static readonly Regex EmojiRange = new(
        @"[\uD83C-\uDBFF][\uDC00-\uDFFF]|[⌀-⏿⬀-⯿ἀ0-῿F]",
        RegexOptions.Compiled);

    // Multipla mellanslag / line-breaks
    private static readonly Regex WhitespaceCollapse = new(@"[ \t]+", RegexOptions.Compiled);
    private static readonly Regex BlankLinesCollapse = new(@"(\r?\n\s*){2,}", RegexOptions.Compiled);

    // Specifika "tekniska" prefixer / förkortningar
    private static readonly Regex CtxBadge = new(@"\[\s*ctx[^\]]*\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string Sanitize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";

        var s = text;

        // 1. Ta bort badge-prefix (lokal/edge/ctx-tags)
        s = BadgePrefix.Replace(s, "");
        s = CtxBadge.Replace(s, "");

        // 2. Markdown -> ren prosa
        s = MarkdownCodeBlock.Replace(s, " ");
        s = MarkdownInlineCode.Replace(s, m => m.Value.Trim('`'));
        s = MarkdownLink.Replace(s, "$1");
        s = MarkdownHeading.Replace(s, "");
        s = MarkdownBold.Replace(s, "$1");
        s = MarkdownItalic.Replace(s, "$1");
        s = MarkdownListBullet.Replace(s, "");

        // 3. URLs bort (TTS lasanv har URL bokstavligt)
        s = Url.Replace(s, "");

        // 4. JSON-fragment bort (om LLM rapar JSON i sitt svar)
        for (int i = 0; i < 3; i++) s = JsonObject.Replace(s, " ");
        s = JsonArray.Replace(s, " ");

        // 5. Pilar och dingbats
        s = DingbatsAndArrows.Replace(s, "");
        s = EmojiRange.Replace(s, "");

        // 6. Tekniska tecken som TTS lasar bokstavligt — ersatt med space/borttagna.
        var cleaned = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (char.IsLetter(ch) || char.IsDigit(ch) || char.IsWhiteSpace(ch))
            {
                cleaned.Append(ch);
                continue;
            }
            switch (ch)
            {
                case '.': case ',': case '?': case '!':
                case ';': case ':': case '\'':
                case '"': case '–': case '—': case '-':
                case '(': case ')': case '%':
                    cleaned.Append(ch);
                    break;
                default:
                    // alla overiga tecken (#, *, _, ~, |, ^, &, =, /, \, +, [, ], {, }, <, >, @, etc.)
                    cleaned.Append(' ');
                    break;
            }
        }
        s = cleaned.ToString();

        // 7. Stadkomprimera whitespace
        s = WhitespaceCollapse.Replace(s, " ");
        s = BlankLinesCollapse.Replace(s, "\n");
        s = s.Trim();

        return s;
    }
}
