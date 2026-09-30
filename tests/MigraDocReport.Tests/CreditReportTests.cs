using MigraDocReport.Services;
using PdfSharp.Pdf.IO;

namespace MigraDocReport.Tests;

public sealed class CreditReportTests
{
    [Fact]
    public void Load_DeserializesCustomerSummaryBureausAndReferences()
    {
        var path = Path.Combine(Path.GetTempPath(), $"credit-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """
            {
              "customer": { "name": "Sample", "addressLines": ["1 Example Road"] },
              "summary": [{ "label": "Score", "value": "700" }],
              "bureaus": [{ "bureau": "Example Bureau", "score": "700" }],
              "references": {
                "columns": [{ "key": "name", "header": "Name" }],
                "rows": [{ "name": "Reference" }]
              }
            }
            """);

        try
        {
            var report = CreditReportJson.Load(path);

            Assert.Equal("Sample", report.Customer.Name);
            Assert.Single(report.Summary);
            Assert.Single(report.Bureaus);
            Assert.Single(report.References.Columns);
            Assert.Single(report.References.Rows);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Generate_CreatesNonEmptyCreditReportPdf()
    {
        var inputPath = CreateSampleResponseFile();
        var report = CreditReportJson.Load(inputPath);
        var outputPath = Path.Combine(Path.GetTempPath(), $"credit-{Guid.NewGuid():N}.pdf");

        try
        {
            new CreditReportPdfGenerator().Generate(report, outputPath, new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero));

            var info = new FileInfo(outputPath);
            Assert.True(info.Exists);
            Assert.True(info.Length > 1_000);
            using var pdf = PdfReader.Open(outputPath, PdfDocumentOpenMode.Import);
            Assert.True(pdf.PageCount >= 1);
        }
        finally
        {
            File.Delete(inputPath);
            File.Delete(outputPath);
        }
    }

    private static string CreateSampleResponseFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"credit-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """
            {
              "customer": { "name": "Sample", "addressLines": ["1 Example Road", "Chicago, IL"] },
              "summary": [
                { "label": "One", "value": "1" }, { "label": "Two", "value": "2" },
                { "label": "Three", "value": "3" }, { "label": "Four", "value": "4" },
                { "label": "Five", "value": "5" }, { "label": "Six", "value": "6" },
                { "label": "Seven", "value": "7" }, { "label": "Eight", "value": "8" },
                { "label": "Nine", "value": "9" }, { "label": "Ten", "value": "10" },
                { "label": "Eleven", "value": "11" }, { "label": "Twelve", "value": "12" },
                { "label": "Thirteen", "value": "13" }, { "label": "Fourteen", "value": "14" },
                { "label": "Fifteen", "value": "15" }, { "label": "Sixteen", "value": "16" },
                { "label": "Seventeen", "value": "17" }, { "label": "Eighteen", "value": "18" },
                { "label": "Nineteen", "value": "19" }, { "label": "Twenty", "value": "20" }
              ],
              "bureaus": [{ "bureau": "Example", "reportDate": "Today", "score": "700", "status": "Clear", "details": "Example line" }],
              "references": {
                "columns": [{ "key": "name", "header": "Name" }],
                "rows": [{ "name": "Reference" }]
              }
            }
            """);
        return path;
    }
}
