namespace JarvisClean;

internal static class CommandValidatorV1
{
    public static List<string> Validate(CommandResult command)
    {
        var errors = new List<string>();
        errors.AddRange(command.ValidationErrors);

        switch (command.Intent)
        {
            case CommandIntent.MemorySave:
                RequireArgument(command, "text", "Minnestext saknas.", errors);
                break;

            case CommandIntent.MemorySearch:
            case CommandIntent.MemoryArchiveSearch:
            case CommandIntent.MemoryForgetPrepare:
                RequireArgument(command, "query", "Söktext saknas.", errors);
                break;

            case CommandIntent.FileOpen:
            case CommandIntent.FileRead:
            case CommandIntent.FolderOpen:
                RequireArgument(command, "path", "Sökväg saknas.", errors);
                break;

            case CommandIntent.FileCreateRequest:
            case CommandIntent.FileWriteRequest:
            case CommandIntent.FileAppendRequest:
                RequireArgument(command, "path", "Sökväg saknas.", errors);
                RequireArgument(command, "text", "Text/innehåll saknas.", errors);
                RequireApproval(command, "Filskrivning måste kräva pending/godkännande.", errors);
                break;

            case CommandIntent.TerminalPreview:
                RequireArgument(command, "command", "Terminalkommando saknas.", errors);
                break;

            case CommandIntent.TerminalConfirm:
                RequireApproval(command, "Terminalkörning måste kräva preview/godkännande.", errors);
                break;

            case CommandIntent.ModelChange:
                RequireArgument(command, "model", "Modellnamn saknas.", errors);
                break;
        }

        return errors;
    }

    private static void RequireArgument(CommandResult command, string key, string message, List<string> errors)
    {
        if (!command.Arguments.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            errors.Add(message);
    }

    private static void RequireApproval(CommandResult command, string message, List<string> errors)
    {
        if (!command.RequiresApproval)
            errors.Add(message);
    }
}
