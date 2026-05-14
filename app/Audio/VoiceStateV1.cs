namespace JarvisClean;

internal enum VoiceModeV1
{
    Off,
    Idle,
    Listening,
    Transcribing,
    Sending,
    Speaking
}

internal sealed record VoiceStateSnapshotV1(
    VoiceModeV1 Mode,
    bool Muted,
    string SttModelLabel,
    string TtsVoiceLabel,
    long LastSttMs,
    DateTime UpdatedAt);

internal static class VoiceStateV1
{
    private static readonly object Lock = new();
    private static VoiceStateSnapshotV1 Current = new(
        Mode: VoiceModeV1.Off,
        Muted: false,
        SttModelLabel: "small",
        TtsVoiceLabel: "sv_SE-nst-medium",
        LastSttMs: 0,
        UpdatedAt: DateTime.Now);

    public static VoiceStateSnapshotV1 Snapshot
    {
        get
        {
            lock (Lock)
                return Current;
        }
    }

    public static bool TransitionTo(VoiceModeV1 mode)
    {
        lock (Lock)
        {
            if (Current.Mode == mode)
                return false;
            Current = Current with { Mode = mode, UpdatedAt = DateTime.Now };
            return true;
        }
    }

    public static void SetMuted(bool muted)
    {
        lock (Lock)
            Current = Current with { Muted = muted, UpdatedAt = DateTime.Now };
    }

    public static void SetModelLabels(string sttModel, string ttsVoice)
    {
        lock (Lock)
        {
            Current = Current with
            {
                SttModelLabel = string.IsNullOrWhiteSpace(sttModel) ? Current.SttModelLabel : sttModel,
                TtsVoiceLabel = string.IsNullOrWhiteSpace(ttsVoice) ? Current.TtsVoiceLabel : ttsVoice,
                UpdatedAt = DateTime.Now
            };
        }
    }

    public static void RecordSttElapsed(long milliseconds)
    {
        if (milliseconds < 0)
            milliseconds = 0;
        lock (Lock)
            Current = Current with { LastSttMs = milliseconds, UpdatedAt = DateTime.Now };
    }

    public static bool IsEnabled
    {
        get
        {
            lock (Lock)
                return Current.Mode != VoiceModeV1.Off;
        }
    }

    public static bool ShouldSpeak
    {
        get
        {
            lock (Lock)
                return Current.Mode != VoiceModeV1.Off && !Current.Muted;
        }
    }

    public static string Status()
    {
        var s = Snapshot;
        return "Röst V1\n" +
               "Läge: " + Label(s.Mode) + "\n" +
               "Mute: " + (s.Muted ? "ja" : "nej") + "\n" +
               "STT-modell: " + s.SttModelLabel + "\n" +
               "TTS-röst: " + s.TtsVoiceLabel + "\n" +
               "Senaste STT: " + s.LastSttMs + " ms\n" +
               "Uppdaterad: " + s.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public static string OverviewLine()
    {
        var s = Snapshot;
        var muteSuffix = s.Muted ? " (mute)" : "";
        var sttSuffix = s.LastSttMs > 0 ? $" | senast {s.LastSttMs}ms" : "";
        return "Röst: " + Label(s.Mode) + muteSuffix + "\n" +
               "Modell: " + s.SttModelLabel + sttSuffix;
    }

    public static string Label(VoiceModeV1 mode)
    {
        return mode switch
        {
            VoiceModeV1.Off => "Av",
            VoiceModeV1.Idle => "På (lyssnar inte)",
            VoiceModeV1.Listening => "Lyssnar",
            VoiceModeV1.Transcribing => "Transkriberar",
            VoiceModeV1.Sending => "Skickar",
            VoiceModeV1.Speaking => "Talar",
            _ => "Av"
        };
    }
}
