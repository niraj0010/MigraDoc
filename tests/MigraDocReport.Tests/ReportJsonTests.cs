using System.Text.Json;
using MigraDocReport.Services;

namespace MigraDocReport.Tests;

public sealed class ReportJsonTests
{
    [Fact]
    public void Load_DeserializesColumnDefinitionsAndFlexibleRows()
    {
        var path = Path.Combine(Path.GetTempPath(), $"report-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """
            {
              "title": "Quarterly review",
              "kpis": [{ "label": "Orders", "value": "120" }],
              "columns": [{ "key": "amount", "header": "Amount", "alignment": "right" }],
              "rows": [{ "amount": 42 }, { "amount": null }]
            }
            """);

        try
        {
            var report = ReportJson.Load(path);

            Assert.Equal("Quarterly review", report.Title);
            Assert.Single(report.Kpis);
            Assert.Single(report.Columns);
            Assert.Equal(2, report.Rows.Count);
            Assert.Equal(JsonValueKind.Number, report.Rows[0]["amount"].ValueKind);
            Assert.Equal(JsonValueKind.Null, report.Rows[1]["amount"].ValueKind);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Validate_RejectsDuplicateColumnKeys()
    {
        var path = Path.Combine(Path.GetTempPath(), $"report-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """
            { "columns": [{ "key": "id" }, { "key": "ID" }] }
            """);

        try
        {
            Assert.Throws<JsonException>(() => ReportJson.Load(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
