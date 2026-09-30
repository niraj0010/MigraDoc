using System.Text;
using System.Text.Json;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using MigraDocReport.Models;

namespace MigraDocReport.Services;

public sealed class CreditReportPdfGenerator
{
    private const string TitleStyle = "CreditReportTitle";
    private const string CustomerNameStyle = "CustomerName";
    private const string SummaryLabelStyle = "SummaryLabel";
    private const string SummaryValueStyle = "SummaryValue";
    private const string TableHeaderStyle = "CreditTableHeader";

    public void Generate(CreditReportResponse response, string outputPath, DateTimeOffset? generatedAt = null)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        CreditReportJson.Validate(response);
        PdfFontBootstrapper.Configure();

        var document = CreateDocument(response, generatedAt ?? DateTimeOffset.Now);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        renderer.PdfDocument.Save(outputPath);
    }

    private static Document CreateDocument(CreditReportResponse response, DateTimeOffset generatedAt)
    {
        var document = new Document();
        document.Info.Title = DisplayText(response.Title, "Credit report");
        DefineStyles(document);

        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.TopMargin = Unit.FromCentimeter(1.4);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(1.5);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(1.5);
        section.PageSetup.RightMargin = Unit.FromCentimeter(1.5);

        var title = section.AddParagraph(document.Info.Title);
        title.Style = TitleStyle;
        var generated = section.AddParagraph($"Prepared {generatedAt.LocalDateTime:MMMM d, yyyy h:mm tt}  |  Sample data only");
        generated.Format.Font.Size = 8;
        generated.Format.Font.Color = Colors.DimGray;
        generated.Format.SpaceAfter = Unit.FromPoint(11);

        AddCustomer(section, response.Customer);
        AddSummary(section, response.Summary);
        AddBureaus(section, response.Bureaus);
        AddReferences(section, response.References);
        AddFooter(section);
        return document;
    }

    private static void DefineStyles(Document document)
    {
        var normal = document.Styles[StyleNames.Normal]!;
        normal.Font.Name = "Arial";
        normal.Font.Size = 8.5;
        normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(2);

        var title = document.Styles.AddStyle(TitleStyle, StyleNames.Normal);
        title.Font.Name = "Arial";
        title.Font.Size = 20;
        title.Font.Bold = true;
        title.Font.Color = Colors.DarkSlateGray;
        title.ParagraphFormat.SpaceAfter = Unit.FromPoint(2);

        var heading = document.Styles[StyleNames.Heading1]!;
        heading.Font.Name = "Arial";
        heading.Font.Size = 12;
        heading.Font.Bold = true;
        heading.Font.Color = Colors.DarkSlateGray;
        heading.ParagraphFormat.SpaceBefore = Unit.FromPoint(10);
        heading.ParagraphFormat.SpaceAfter = Unit.FromPoint(5);

        var customerName = document.Styles.AddStyle(CustomerNameStyle, StyleNames.Normal);
        customerName.Font.Size = 13;
        customerName.Font.Bold = true;
        customerName.Font.Color = Colors.SteelBlue;
        customerName.ParagraphFormat.SpaceAfter = Unit.FromPoint(1);

        var summaryLabel = document.Styles.AddStyle(SummaryLabelStyle, StyleNames.Normal);
        summaryLabel.Font.Size = 7.5;
        summaryLabel.Font.Bold = true;
        summaryLabel.Font.Color = Colors.DarkSlateGray;
        summaryLabel.ParagraphFormat.SpaceAfter = Unit.FromPoint(1);

        var summaryValue = document.Styles.AddStyle(SummaryValueStyle, StyleNames.Normal);
        summaryValue.Font.Size = 9;
        summaryValue.Font.Bold = true;
        summaryValue.Font.Color = Colors.Black;
        summaryValue.ParagraphFormat.SpaceAfter = 0;

        var tableHeader = document.Styles.AddStyle(TableHeaderStyle, StyleNames.Normal);
        tableHeader.Font.Bold = true;
        tableHeader.Font.Color = Colors.White;
        tableHeader.ParagraphFormat.SpaceAfter = 0;
    }

    private static void AddCustomer(Section section, CreditCustomer customer)
    {
        section.AddParagraph("Customer", StyleNames.Heading1);
        var name = section.AddParagraph(DisplayText(customer.Name, "Customer name unavailable"));
        name.Style = CustomerNameStyle;

        if (customer.AddressLines.Count == 0)
        {
            section.AddParagraph("Address unavailable");
            return;
        }

        foreach (var addressLine in customer.AddressLines.Where(static line => !string.IsNullOrWhiteSpace(line)))
        {
            section.AddParagraph(DisplayText(addressLine));
        }
    }

    private static void AddSummary(Section section, IReadOnlyList<CreditSummaryItem> summary)
    {
        section.AddParagraph("Credit profile summary", StyleNames.Heading1);
        if (summary.Count == 0)
        {
            section.AddParagraph("No credit-profile values were provided.");
            return;
        }

        const int columnsPerRow = 4;
        var table = section.AddTable();
        table.Borders.Width = Unit.FromPoint(0.5);
        table.Borders.Color = Colors.LightGray;
        table.Shading.Color = Colors.GhostWhite;
        table.LeftPadding = Unit.FromPoint(6);
        table.RightPadding = Unit.FromPoint(6);
        table.TopPadding = Unit.FromPoint(4);
        table.BottomPadding = Unit.FromPoint(4);
        for (var column = 0; column < columnsPerRow; column++)
        {
            table.AddColumn(Unit.FromCentimeter(18.0 / columnsPerRow));
        }

        for (var index = 0; index < summary.Count; index++)
        {
            if (index % columnsPerRow == 0)
            {
                table.AddRow();
            }

            var cell = table.Rows[table.Rows.Count - 1].Cells[index % columnsPerRow];
            cell.VerticalAlignment = VerticalAlignment.Center;
            AddStyledParagraph(cell, DisplayText(summary[index].Label, "Metric"), SummaryLabelStyle);
            AddStyledParagraph(cell, DisplayText(summary[index].Value), SummaryValueStyle);
        }
    }

    private static void AddBureaus(Section section, IReadOnlyList<BureauReportLine> bureaus)
    {
        section.AddParagraph("Bureau report data", StyleNames.Heading1);
        if (bureaus.Count == 0)
        {
            section.AddParagraph("No bureau report data was provided.");
            return;
        }

        var table = section.AddTable();
        table.Borders.Width = Unit.FromPoint(0.5);
        table.Borders.Color = Colors.LightGray;
        table.LeftPadding = Unit.FromPoint(4);
        table.RightPadding = Unit.FromPoint(4);
        table.TopPadding = Unit.FromPoint(3);
        table.BottomPadding = Unit.FromPoint(3);
        foreach (var width in new[] { 3.1, 2.8, 1.5, 3.2, 7.4 })
        {
            table.AddColumn(Unit.FromCentimeter(width));
        }

        var header = table.AddRow();
        header.HeadingFormat = true;
        header.Shading.Color = Colors.DarkSlateGray;
        foreach (var (label, index) in new[] { "Bureau", "Report date", "Score", "Status", "Report summary" }.Select((label, index) => (label, index)))
        {
            AddStyledParagraph(header.Cells[index], label, TableHeaderStyle);
        }

        foreach (var bureau in bureaus)
        {
            var row = table.AddRow();
            row.VerticalAlignment = VerticalAlignment.Center;
            row.Cells[0].AddParagraph(DisplayText(bureau.Bureau, "Bureau"));
            row.Cells[1].AddParagraph(DisplayText(bureau.ReportDate));
            row.Cells[2].Format.Alignment = ParagraphAlignment.Right;
            row.Cells[2].AddParagraph(DisplayText(bureau.Score));
            row.Cells[3].AddParagraph(DisplayText(bureau.Status));
            row.Cells[4].AddParagraph(DisplayText(bureau.Details));
        }
    }

    private static void AddReferences(Section section, ReferenceSection references)
    {
        section.AddParagraph("References", StyleNames.Heading1);
        if (references.Columns.Count == 0)
        {
            section.AddParagraph("No reference column definitions were provided.");
            return;
        }

        var table = section.AddTable();
        table.Borders.Width = Unit.FromPoint(0.5);
        table.Borders.Color = Colors.LightGray;
        table.LeftPadding = Unit.FromPoint(4);
        table.RightPadding = Unit.FromPoint(4);
        table.TopPadding = Unit.FromPoint(3);
        table.BottomPadding = Unit.FromPoint(3);

        var totalWeight = references.Columns.Sum(static column => column.WidthWeight.GetValueOrDefault() > 0
            ? column.WidthWeight!.Value
            : 1);
        foreach (var column in references.Columns)
        {
            var weight = column.WidthWeight.GetValueOrDefault() > 0 ? column.WidthWeight!.Value : 1;
            table.AddColumn(Unit.FromCentimeter(18.0 * weight / totalWeight));
        }

        var header = table.AddRow();
        header.HeadingFormat = true;
        header.Shading.Color = Colors.DarkSlateGray;
        foreach (var (column, index) in references.Columns.Select((column, index) => (column, index)))
        {
            header.Cells[index].Format.Alignment = IsRightAligned(column) ? ParagraphAlignment.Right : ParagraphAlignment.Left;
            AddStyledParagraph(header.Cells[index], DisplayText(column.Header, column.Key!), TableHeaderStyle);
        }

        foreach (var sourceRow in references.Rows)
        {
            var row = table.AddRow();
            row.VerticalAlignment = VerticalAlignment.Top;
            foreach (var (column, index) in references.Columns.Select((column, index) => (column, index)))
            {
                sourceRow.TryGetValue(column.Key!, out var value);
                row.Cells[index].Format.Alignment = IsRightAligned(column) ? ParagraphAlignment.Right : ParagraphAlignment.Left;
                row.Cells[index].AddParagraph(DisplayJsonValue(value));
            }
        }

        if (references.Rows.Count == 0)
        {
            var row = table.AddRow();
            row.Cells[0].MergeRight = references.Columns.Count - 1;
            row.Cells[0].AddParagraph("No references were provided.");
        }
    }

    private static void AddFooter(Section section)
    {
        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Font.Size = 7.5;
        footer.Format.Font.Color = Colors.Gray;
        footer.Format.Alignment = ParagraphAlignment.Center;
        footer.AddText("Sample credit report - not for lending or consumer-reporting use - Page ");
        footer.AddPageField();
    }

    private static void AddStyledParagraph(Cell cell, string text, string style)
    {
        var paragraph = cell.AddParagraph(text);
        paragraph.Style = style;
    }

    private static bool IsRightAligned(TableColumnDefinition column) =>
        string.Equals(column.Alignment, "right", StringComparison.OrdinalIgnoreCase);

    private static string DisplayJsonValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Undefined or JsonValueKind.Null => "-",
        JsonValueKind.String => DisplayText(value.GetString()),
        JsonValueKind.True => "Yes",
        JsonValueKind.False => "No",
        _ => MakeLongTextBreakable(value.GetRawText()),
    };

    private static string DisplayText(string? value, string missingValue = "-") =>
        string.IsNullOrWhiteSpace(value) ? missingValue : MakeLongTextBreakable(value.Trim());

    private static string MakeLongTextBreakable(string value)
    {
        const int maximumUnbrokenCharacters = 32;
        var result = new StringBuilder(value.Length + value.Length / maximumUnbrokenCharacters);
        var consecutiveNonWhitespace = 0;
        foreach (var character in value)
        {
            result.Append(character);
            consecutiveNonWhitespace = char.IsWhiteSpace(character) ? 0 : consecutiveNonWhitespace + 1;
            if (consecutiveNonWhitespace == maximumUnbrokenCharacters)
            {
                result.Append('\u200B');
                consecutiveNonWhitespace = 0;
            }
        }

        return result.ToString();
    }
}
