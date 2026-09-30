using System.Text.Json;

namespace MigraDocReport.Models;

/// <summary>A purpose-built contract for a credit-report style document.</summary>
public sealed class CreditReportResponse
{
    public string? Title { get; init; }

    public CreditCustomer Customer { get; init; } = new();

    public List<CreditSummaryItem> Summary { get; init; } = [];

    public List<BureauReportLine> Bureaus { get; init; } = [];

    public ReferenceSection References { get; init; } = new();
}

public sealed class CreditCustomer
{
    public string? Name { get; init; }

    public List<string> AddressLines { get; init; } = [];
}

public sealed class CreditSummaryItem
{
    public string? Label { get; init; }

    public string? Value { get; init; }
}

public sealed class BureauReportLine
{
    public string? Bureau { get; init; }

    public string? ReportDate { get; init; }

    public string? Score { get; init; }

    public string? Status { get; init; }

    public string? Details { get; init; }
}

public sealed class ReferenceSection
{
    public List<TableColumnDefinition> Columns { get; init; } = [];

    public List<Dictionary<string, JsonElement>> Rows { get; init; } = [];
}
