using System.Text.Json;
using MigraDocReport.Models;

namespace MigraDocReport.Services;

public static class CreditReportJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static CreditReportResponse Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        using var stream = File.OpenRead(filePath);
        var response = JsonSerializer.Deserialize<CreditReportResponse>(stream, Options)
            ?? throw new JsonException("The credit report response was empty.");

        Validate(response);
        return response;
    }

    public static void Validate(CreditReportResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (response.References is null)
        {
            throw new JsonException("A references object is required.");
        }

        ReportJson.Validate(new ReportResponse { Columns = response.References.Columns });
    }
}
