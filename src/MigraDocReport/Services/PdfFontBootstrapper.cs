using PdfSharp.Fonts;

namespace MigraDocReport.Services;

/// <summary>Configures the PDFsharp Core build before any document is rendered.</summary>
public static class PdfFontBootstrapper
{
    private static int configured;

    public static void Configure()
    {
        if (Interlocked.Exchange(ref configured, 1) != 0)
        {
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            // PDFsharp 6.2 Core deliberately requires this explicit opt-in.
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;
            return;
        }

        throw new PlatformNotSupportedException(
            "This sample uses Windows system fonts. Configure a custom PDFsharp font resolver for non-Windows deployments.");
    }
}
