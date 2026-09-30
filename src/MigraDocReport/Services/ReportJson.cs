using System.Text.Json;
using MigraDocReport.Models;

namespace MigraDocReport.Services;

public static class ReportJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static ReportResponse Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        using var stream = File.OpenRead(filePath);
        var response = JsonSerializer.Deserialize<ReportResponse>(stream, Options)
            ?? throw new JsonException("The report response was empty.");

        Validate(response);
        return response;
    }

    public static void Validate(ReportResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in response.Columns)
        {
            if (string.IsNullOrWhiteSpace(column.Key))
            {
                throw new JsonException("Every column definition requires a non-empty 'key'.");
            }

            if (!keys.Add(column.Key))
            {
                throw new JsonException($"Column key '{column.Key}' is defined more than once.");
            }
        }
    }
}
