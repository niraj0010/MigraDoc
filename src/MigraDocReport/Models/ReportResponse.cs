using System.Text.Json;

namespace MigraDocReport.Models;

/// <summary>Data contract supplied by the reporting API.</summary>
public sealed class ReportResponse
{
    public string? Title { get; init; }

    public List<KpiCard> Kpis { get; init; } = [];

    public List<TableColumnDefinition> Columns { get; init; } = [];

    public List<Dictionary<string, JsonElement>> Rows { get; init; } = [];
}

public sealed class KpiCard
{
    public string? Label { get; init; }

    public string? Value { get; init; }

    public string? Detail { get; init; }
}

public sealed class TableColumnDefinition
{
    public string? Key { get; init; }

    public string? Header { get; init; }

    /// <summary>Relative column width. Missing or non-positive values use 1.</summary>
    public double? WidthWeight { get; init; }

    /// <summary>Use "right" for numeric columns; any other value is left aligned.</summary>
    public string? Alignment { get; init; }
}
