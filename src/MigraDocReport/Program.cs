using MigraDocReport.Services;

var isCreditReport = args.Length > 0 && string.Equals(args[0], "credit", StringComparison.OrdinalIgnoreCase);
var firstInputArgument = isCreditReport ? 1 : 0;
var maximumArguments = isCreditReport ? 3 : 2;
if (args.Length is < 1 || args.Length > maximumArguments || args.Length <= firstInputArgument)
{
    Console.Error.WriteLine("Usage: dotnet run --project src/MigraDocReport -- <input-json> [output-pdf]");
    Console.Error.WriteLine("   or: dotnet run --project src/MigraDocReport -- credit <input-json> [output-pdf]");
    return 2;
}

try
{
    var inputPath = Path.GetFullPath(args[firstInputArgument]);
    var hasOutputPath = args.Length > firstInputArgument + 1;
    var outputPath = hasOutputPath
        ? Path.GetFullPath(args[firstInputArgument + 1])
        : Path.Combine(Directory.GetCurrentDirectory(), "output", "pdf", isCreditReport ? "credit-report.pdf" : "report.pdf");

    if (isCreditReport)
    {
        new CreditReportPdfGenerator().Generate(CreditReportJson.Load(inputPath), outputPath);
    }
    else
    {
        new ReportPdfGenerator().Generate(ReportJson.Load(inputPath), outputPath);
    }
    Console.WriteLine($"PDF created: {outputPath}");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Report generation failed: {exception.Message}");
    return 1;
}
