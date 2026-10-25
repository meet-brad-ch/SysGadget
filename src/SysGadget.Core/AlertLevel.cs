namespace SysGadget.Core
{
    /// <summary>How alarming a value is; the view colours it accordingly.</summary>
    public enum AlertLevel
    {
        /// <summary>Normal.</summary>
        Normal,

        /// <summary>High: shown in amber.</summary>
        Warning,

        /// <summary>Critical: shown in red.</summary>
        Critical,
    }
}
