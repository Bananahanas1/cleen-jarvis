namespace JarvisClean;

internal enum PendingApprovalTypeV1
{
    None,
    FileCreate,
    FileWrite,
    FileAppend,
    FileDelete,
    FileUndo,
    TerminalRun,
    MemorySave
}

internal sealed class PendingApprovalV1
{
    public PendingApprovalTypeV1 Type { get; init; } = PendingApprovalTypeV1.None;
    public string Target { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    public bool RequiresUserApproval { get; init; } = true;
}

internal static class PendingApprovalStoreV1
{
    public static PendingApprovalV1? Current { get; private set; }

    public static void Set(PendingApprovalV1 approval)
    {
        Current = approval;
    }

    public static PendingApprovalV1? Get()
    {
        return Current;
    }

    public static void Clear()
    {
        Current = null;
    }

    public static bool HasPending => Current is not null;
}
