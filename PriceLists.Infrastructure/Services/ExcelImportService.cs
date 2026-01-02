using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ClosedXML.Excel;
using PriceLists.Core.Abstractions;
using PriceLists.Core.Models;

namespace PriceLists.Infrastructure.Services;

public class ExcelImportService : IExcelImportService
{
    private static readonly string[] CodeKeywords = ["COD", "CODIGO", "SKU"];
    private static readonly string[] DescriptionKeywords = ["DESCRIP", "DESCRIPCION", "DETALLE"];
    private static readonly string[] PriceKeywords = ["PRECIO", "PVP", "PRICE", "$"];

    public Task<ImportPreview> ImportAsync(string filePath, int? maxRows = 20, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("El path del archivo es requerido.", nameof(filePath));
        }

        return Task.Run(() => LoadPreview(filePath, maxRows, ct), ct);
    }

    private ImportPreview LoadPreview(string filePath, int? maxRows, CancellationToken ct)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("El archivo no existe.", filePath);
        }

        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.FirstOrDefault(ws => ws.RangeUsed() is not null);

        if (worksheet is null)
        {
            throw new InvalidOperationException("No se encontró ninguna hoja con datos.");
        }

        var usedRange = worksheet.RangeUsed()!;
        var firstRow = usedRange.RangeAddress.FirstAddress.RowNumber;
        var lastRow = usedRange.RangeAddress.LastAddress.RowNumber;
        var firstColumn = usedRange.RangeAddress.FirstAddress.ColumnNumber;
        var lastColumn = usedRange.RangeAddress.LastAddress.ColumnNumber;

        var searchRowLimit = Math.Min(lastRow, firstRow + 99);
        var searchColLimit = Math.Min(lastColumn, firstColumn + 29);

        int? headerRow = null;
        int? codeColumn = null;
        int? descriptionColumn = null;
        int? priceColumn = null;

        for (var row = firstRow; row <= searchRowLimit; row++)
        {
            ct.ThrowIfCancellationRequested();

            int? rowCode = null;
            int? rowDescription = null;
            int? rowPrice = null;

            for (var column = firstColumn; column <= searchColLimit; column++)
            {
                var normalized = NormalizeHeader(worksheet.Cell(row, column).GetString());
                if (string.IsNullOrWhiteSpace(normalized))
                {
                    continue;
                }

                if (rowCode is null && ContainsKeyword(normalized, CodeKeywords))
                {
                    rowCode = column;
                }

                if (rowDescription is null && ContainsKeyword(normalized, DescriptionKeywords))
                {
                    rowDescription = column;
                }

                if (rowPrice is null && ContainsKeyword(normalized, PriceKeywords))
                {
                    rowPrice = column;
                }
            }

            if (rowDescription is not null && (rowPrice is not null || rowCode is not null))
            {
                headerRow = row;
                codeColumn = rowCode;
                descriptionColumn = rowDescription;
                priceColumn = rowPrice;
                break;
            }
        }

        if (headerRow is null || descriptionColumn is null)
        {
            throw new InvalidOperationException("No se encontró header.");
        }

        codeColumn ??= 1;
        priceColumn ??= descriptionColumn.Value + 1;

        var previewRows = ReadRows(worksheet, headerRow.Value, codeColumn.Value, descriptionColumn.Value, priceColumn.Value, lastRow, maxRows, ct);

        return new ImportPreview
        {
            SheetName = worksheet.Name,
            HeaderRow = headerRow.Value,
            CodeColumn = codeColumn.Value,
            DescriptionColumn = descriptionColumn.Value,
            PriceColumn = priceColumn.Value,
            Rows = previewRows
        };
    }

    private static IReadOnlyList<PriceItemPreviewRow> ReadRows(
        IXLWorksheet worksheet,
        int headerRow,
        int codeColumn,
        int descriptionColumn,
        int priceColumn,
        int lastRow,
        int? maxRows,
        CancellationToken ct)
    {
        var rows = new List<PriceItemPreviewRow>();
        var emptyInARow = 0;

        for (var row = headerRow + 1; row <= lastRow; row++)
        {
            ct.ThrowIfCancellationRequested();

            var code = worksheet.Cell(row, codeColumn).GetString().Trim();
            var description = worksheet.Cell(row, descriptionColumn).GetString().Trim();
            var priceValue = worksheet.Cell(row, priceColumn).GetString();

            var price = ParseDecimal(priceValue);

            if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(description) && price is null)
            {
                emptyInARow++;
                if (emptyInARow >= 5)
                {
                    break;
                }

                continue;
            }

            emptyInARow = 0;

            rows.Add(new PriceItemPreviewRow
            {
                Code = string.IsNullOrWhiteSpace(code) ? null : code,
                Description = string.IsNullOrWhiteSpace(description) ? null : description,
                Price = price
            });

            if (maxRows is not null && rows.Count >= maxRows)
            {
                break;
            }
        }

        return rows;
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.GetCultureInfo("es-ES"), out result))
        {
            return result;
        }

        var sanitized = normalized.Replace(" ", string.Empty).Replace(".", string.Empty).Replace(",", ".");
        if (decimal.TryParse(sanitized, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
        {
            return result;
        }

        return null;
    }

    private static string NormalizeHeader(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var upper = value.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var ch in upper)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }

    private static bool ContainsKeyword(string value, IEnumerable<string> keywords)
    {
        foreach (var keyword in keywords)
        {
            if (value.Contains(keyword, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
