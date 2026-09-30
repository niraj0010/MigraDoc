using MigraDocReport.Services;

if (args.Length is < 1 or > 2)
{
    Console.Error.WriteLine("Usage: dotnet run --project src/MigraDocReport -- <input-json> [output-pdf]");
    return 2;
}

try
{
    var inputPath = Path.GetFullPath(args[0]);
    var outputPath = args.Length == 2
        ? Path.GetFullPath(args[1])
        : Path.Combine(Directory.GetCurrentDirectory(), "output", "pdf", "report.pdf");

    var response = ReportJson.Load(inputPath);
    new ReportPdfGenerator().Generate(response, outputPath);
    Console.WriteLine($"PDF created: {outputPath}");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Report generation failed: {exception.Message}");
    return 1;
}
