namespace AliCdnSSLWorker.Configs;

public record TimerMonitorConfig : MonitorConfigBase
{
    public uint RefreshIntervalMinute { get; set; }

    public TimeSpan RefreshInterval
    {
        get => TimeSpan.FromMinutes(RefreshIntervalMinute);
        set => RefreshIntervalMinute = (uint)value.TotalMinutes;
    }
}
