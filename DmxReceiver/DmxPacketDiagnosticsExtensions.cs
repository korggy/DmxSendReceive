namespace DmxReceiver;

public static class DmxPacketDiagnosticsExtensions
{
    public static string FormatBreakStatus(this DmxPacketDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        return diagnostics.BreakStatus switch
        {
            DmxBreakStatus.NoBreakInformation => "none",
            DmxBreakStatus.UartFramingErrorNoDuration => "uart-framing-no-duration",
            DmxBreakStatus.UartExplicitBreak => "uart-break",
            DmxBreakStatus.GpioMeasuredValidBreak => "gpio-valid",
            DmxBreakStatus.GpioMeasuredShortBreak => "gpio-short",
            DmxBreakStatus.InferredFromUartFramingError => "inferred",
            _ => diagnostics.BreakStatus.ToString()
        };
    }
}
