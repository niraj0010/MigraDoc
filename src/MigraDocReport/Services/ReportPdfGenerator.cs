using System.Text;
using System.Text.Json;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using MigraDocReport.Models;

namespace MigraDocReport.Services;

public sealed class ReportPdfGenerator
{
    private const string ReportTitleStyle = "ReportTitle";
    private const string KpiLabelStyle = "KpiLabel";
    private const string KpiValueStyle = "KpiValue";
    private const string KpiDetailStyle = "KpiDetail";
    private const string TableHeaderStyle = "TableHeader";

    public void Generate(ReportResponse response, string outputPath, DateTimeOffset? generatedAt = null)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ReportJson.Validate(response);
        PdfFontBootstrapper.Configure();

        var document = CreateDocument(response, generatedAt ?? DateTimeOffset.Now);
        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath))!;
        Directory.CreateDirectory(outputDirectory);

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        renderer.PdfDocument.Save(outputPath);
    }

    private static Document CreateDocument(ReportResponse response, DateTimeOffset generatedAt)
    {
        var document = new Document();
        document.Info.Title = DisplayText(response.Title, "Untitled report");
        DefineStyles(document);

        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.TopMargin = Unit.FromCentimeter(1.6);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(1.6);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(1.5);
        section.PageSetup.RightMargin = Unit.FromCentimeter(1.5);

        var title = section.AddParagraph(document.Info.Title);
        title.Style = ReportTitleStyle;

        var date = section.AddParagraph($"Generated {generatedAt.LocalDateTime:MMMM d, yyyy h:mm tt}");
        date.Format.Font.Color = Colors.DarkSlateGray;
        date.Format.SpaceAfter = Unit.FromPoint(14);

        AddKpis(section, response.Kpis);
        AddDataTable(section, response.Columns, response.Rows);
        AddFooter(section);
        return document;
    }

    private static void DefineStyles(Document document)
    {
        var normal = document.Styles[StyleNames.Normal]!;
        normal.Font.Name = "Arial";
        normal.Font.Size = 9;
        normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(4);

        var title = document.Styles.AddStyle(ReportTitleStyle, StyleNames.Normal);
        title.Font.Name = "Arial";
        title.Font.Size = 21;
        title.Font.Bold = true;
        title.Font.Color = Colors.DarkSlateGray;
        title.ParagraphFormat.SpaceAfter = Unit.FromPoint(3);

        var heading = document.Styles[StyleNames.Heading1]!;
        heading.Font.Name = "Arial";
        heading.Font.Size = 13;
        heading.Font.Bold = true;
        heading.Font.Color = Colors.DarkSlateGray;
        heading.ParagraphFormat.SpaceBefore = Unit.FromPoint(12);
        heading.ParagraphFormat.SpaceAfter = Unit.FromPoint(7);

        var kpiLabel = document.Styles.AddStyle(KpiLabelStyle, StyleNames.Normal);
        kpiLabel.Font.Size = 8;
        kpiLabel.Font.Bold = true;
        kpiLabel.Font.Color = Colors.DarkSlateGray;
        kpiLabel.ParagraphFormat.SpaceAfter = Unit.FromPoint(2);

        var kpiValue = document.Styles.AddStyle(KpiValueStyle, StyleNames.Normal);
        kpiValue.Font.Size = 16;
        kpiValue.Font.Bold = true;
        kpiValue.Font.Color = Colors.SteelBlue;
        kpiValue.ParagraphFormat.SpaceAfter = Unit.FromPoint(2);

        var kpiDetail = document.Styles.AddStyle(KpiDetailStyle, StyleNames.Normal);
        kpiDetail.Font.Size = 8;
        kpiDetail.Font.Color = Colors.DimGray;

        var tableHeader = document.Styles.AddStyle(TableHeaderStyle, StyleNames.Normal);
        tableHeader.Font.Bold = true;
        tableHeader.Font.Color = Colors.White;
        tableHeader.ParagraphFormat.SpaceAfter = 0;
    }

    private static void AddKpis(Section section, IReadOnlyList<KpiCard> kpis)
    {
        section.AddParagraph("Key performance indicators", StyleNames.Heading1);
        if (kpis.Count == 0)
        {
            section.AddParagraph("No KPI values were provided.");
            return;
        }

        var cardsPerRow = Math.Min(3, kpis.Count);
        var table = section.AddTable();
        table.Borders.Visible = false;
        table.LeftPadding = Unit.FromPoint(7);
        table.RightPadding = Unit.FromPoint(7);
        table.TopPadding = Unit.FromPoint(6);
        table.BottomPadding = Unit.FromPoint(6);

        for (var column = 0; column < cardsPerRow; column++)
        {
            table.AddColumn(Unit.FromCentimeter(18.0 / cardsPerRow));
        }

        for (var index = 0; index < kpis.Count; index++)
        {
            if (index % cardsPerRow == 0)
            {
                table.AddRow();
            }

            var cell = table.Rows[table.Rows.Count - 1].Cells[index % cardsPerRow];
            cell.Shading.Color = Colors.GhostWhite;
            cell.VerticalAlignment = VerticalAlignment.Center;
            AddStyledParagraph(cell, DisplayText(kpis[index].Label, "KPI"), KpiLabelStyle);
            AddStyledParagraph(cell, DisplayText(kpis[index].Value), KpiValueStyle);
            AddStyledParagraph(cell, DisplayText(kpis[index].Detail), KpiDetailStyle);
        }

        section.AddParagraph().Format.SpaceAfter = Unit.FromPoint(3);
    }

    private static void AddDataTable(
        Section section,
        IReadOnlyList<TableColumnDefinition> columns,
        IReadOnlyList<Dictionary<string, JsonElement>> rows)
    {
        section.AddParagraph("Detailed results", StyleNames.Heading1);
        if (columns.Count == 0)
        {
            section.AddParagraph("No table column definitions were provided.");
            return;
        }

        var table = section.AddTable();
        table.Borders.Width = Unit.FromPoint(0.5);
        table.Borders.Color = Colors.LightGray;
        table.LeftPadding = Unit.FromPoint(5);
        table.RightPadding = Unit.FromPoint(5);
        table.TopPadding = Unit.FromPoint(4);
        table.BottomPadding = Unit.FromPoint(4);

        var totalWeight = columns.Sum(static column => column.WidthWeight.GetValueOrDefault() > 0
            ? column.WidthWeight!.Value
            : 1);
        foreach (var column in columns)
        {
            var weight = column.WidthWeight.GetValueOrDefault() > 0 ? column.WidthWeight!.Value : 1;
            table.AddColumn(Unit.FromCentimeter(18.0 * weight / totalWeight));
        }

        var header = table.AddRow();
        header.HeadingFormat = true;
        header.Shading.Color = Colors.DarkSlateGray;
        header.VerticalAlignment = VerticalAlignment.Center;
        foreach (var (column, index) in columns.Select((column, index) => (column, index)))
        {
            var cell = header.Cells[index];
            cell.Format.Alignment = IsRightAligned(column) ? ParagraphAlignment.Right : ParagraphAlignment.Left;
            AddStyledParagraph(cell, DisplayText(column.Header, column.Key!), TableHeaderStyle);
        }

        foreach (var sourceRow in rows)
        {
            var row = table.AddRow();
            row.VerticalAlignment = VerticalAlignment.Top;
            foreach (var (column, index) in columns.Select((column, index) => (column, index)))
            {
                sourceRow.TryGetValue(column.Key!, out var value);
                var cell = row.Cells[index];
                cell.Format.Alignment = IsRightAligned(column) ? ParagraphAlignment.Right : ParagraphAlignment.Left;
                cell.AddParagraph(DisplayJsonValue(value));
            }
        }

        if (rows.Count == 0)
        {
            var emptyRow = table.AddRow();
            emptyRow.Cells[0].MergeRight = columns.Count - 1;
            emptyRow.Cells[0].AddParagraph("No table rows were provided.");
        }
    }

    private static void AddFooter(Section section)
    {
        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Font.Size = 8;
        footer.Format.Font.Color = Colors.Gray;
        footer.Format.Alignment = ParagraphAlignment.Center;
        footer.AddText("Generated with MigraDoc · Page ");
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
        JsonValueKind.Undefined or JsonValueKind.Null => "—",
        JsonValueKind.String => DisplayText(value.GetString()),
        JsonValueKind.True => "Yes",
        JsonValueKind.False => "No",
        _ => MakeLongTextBreakable(value.GetRawText()),
    };

    private static string DisplayText(string? value, string missingValue = "—") =>
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
