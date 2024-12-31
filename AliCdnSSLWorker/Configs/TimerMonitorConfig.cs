namespace AliCdnSSLWorker.Configs;

public record TimerMonitorConfig : MonitorConfigBase
{
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.Zero;

    public uint RefreshIntervalHour
    {
        get => (uint)RefreshInterval.TotalHours;
        set => _ = TimeSpan.FromHours(value);
    }

    public uint RefreshIntervalMinute
    {
        get => (uint)RefreshInterval.TotalMinutes;
        set => _ = TimeSpan.FromMinutes(value);
    }
}
