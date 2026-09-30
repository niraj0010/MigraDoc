using System.Text.Json;
using MigraDocReport.Models;
using MigraDocReport.Services;
using PdfSharp.Pdf.IO;

namespace MigraDocReport.Tests;

public sealed class ReportPdfGeneratorTests
{
    [Fact]
    public void Generate_CreatesNonEmptyMultiPagePdfFromDynamicRows()
    {
        var report = new ReportResponse
        {
            Title = "Generation test",
            Kpis = [new KpiCard { Label = "Items", Value = "90", Detail = "test data" }],
            Columns =
            [
                new TableColumnDefinition { Key = "id", Header = "ID", WidthWeight = 1 },
                new TableColumnDefinition { Key = "description", Header = "Description", WidthWeight = 4 },
            ],
            Rows = Enumerable.Range(1, 90)
                .Select(index => new Dictionary<string, JsonElement>
                {
                    ["id"] = ToElement(index),
                    ["description"] = ToElement($"Row {index}: a deliberately detailed description that verifies automatic wrapping and continued table layout across pages."),
                })
                .ToList(),
        };
        var outputPath = Path.Combine(Path.GetTempPath(), $"report-{Guid.NewGuid():N}.pdf");

        try
        {
            new ReportPdfGenerator().Generate(report, outputPath, new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero));

            var info = new FileInfo(outputPath);
            Assert.True(info.Exists);
            Assert.True(info.Length > 1_000);

            using var pdf = PdfReader.Open(outputPath, PdfDocumentOpenMode.Import);
            Assert.True(pdf.PageCount > 1);
        }
        finally
        {
            File.Delete(outputPath);
        }
    }

    private static JsonElement ToElement<T>(T value) => JsonSerializer.SerializeToElement(value);
}
