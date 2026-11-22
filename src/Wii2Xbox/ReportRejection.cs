namespace Wii2Xbox;

internal enum ReportRejection { None, Truncated, UnknownReport }

internal static class WiiReportValidation
{
    public static ReportRejection ClassifyIgnored(ReadOnlySpan<byte> report)
    {
        if (report.IsEmpty) return ReportRejection.Truncated;
        int minimumLength = report[0] switch
        {
            0x20 => 7,
            0x21 when report.Length >= 6 && (report[3] & 0x0f) == 0 => 6 + (report[3] >> 4) + 1,
            0x21 => 6,
            0x22 => 5,
            0x30 => 3,
            0x32 => 11,
            0x36 => 22,
            _ => 0
        };
        if (minimumLength == 0) return ReportRejection.UnknownReport;
        return report.Length < minimumLength ? ReportRejection.Truncated : ReportRejection.None;
    }
}
