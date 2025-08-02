using ClivoxApp.Models;
using ClivoxApp.Models.Clients;
using ClivoxApp.Models.Invoice;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ClivoxApp.Services;

/// <summary>
/// Service for exporting invoices to Excel format using NPOI library.
/// Provides functionality to create professionally formatted invoice documents.
/// </summary>
public class ExportInvoiceFile
{
    #region Constants
    
    private const int TextWrapThreshold = 30;
    private const int DescriptionWrapThreshold = 40;
    private const string CompanyLogoPath = "companyLogo.png";
    
    #endregion

    #region Logo Methods
    
    /// <summary>
    /// Adds company logo to the Excel sheet if the logo file exists.
    /// </summary>
    private static void AddCompanyLogo(IWorkbook workbook, ISheet sheet, int colStartPos, int colEndPos, int rowStartPos, int rowEndPos)
    {
        if (!File.Exists(CompanyLogoPath)) return;

        byte[] bytes = File.ReadAllBytes(CompanyLogoPath);
        int pictureIdx = workbook.AddPicture(bytes, PictureType.PNG);
        var patriarch = sheet.CreateDrawingPatriarch();
        var anchor = workbook.GetCreationHelper().CreateClientAnchor();
        
        anchor.Col1 = colStartPos;
        anchor.Row1 = rowStartPos;
        anchor.Col2 = colEndPos;
        anchor.Row2 = rowEndPos;

        var pict = patriarch.CreatePicture(anchor, pictureIdx);
    }
    
    #endregion

    #region Main Export Method
    
    /// <summary>
    /// Exports invoice data to Excel format with professional formatting.
    /// </summary>
    /// <param name="invoice">The invoice to export</param>
    /// <param name="client">The client information</param>
    /// <param name="businessOwner">The business owner information</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static MemoryStream ExportToExcel(Invoice invoice, Client client, BusinessOwner businessOwner)
    {
        IWorkbook workbook = new XSSFWorkbook();
        ISheet sheet = workbook.CreateSheet("Invoice");

        // Configure sheet setup
        AddCompanyLogo(workbook, sheet, 0, 2, 0, 5);
        ConfigureA4PrintSettings(sheet);
        SetColumnWidths(sheet);

        // Create styles for formatting
        var boldFont = workbook.CreateFont();
        boldFont.IsBold = true;

        var styles = CreateCellStyles(workbook, boldFont);

        // Populate invoice content
        PopulateBusinessOwnerInfo(sheet, businessOwner, invoice);
        PopulateClientInfo(sheet, client);
        
        int rowIdx = PopulateGreeting(sheet, client, 11);
        int lastItemRow = PopulateInvoiceTable(sheet, invoice, styles, rowIdx);
        PopulateTotalRow(sheet, invoice, styles, lastItemRow + 1);
        PopulatePaymentInstructions(sheet, businessOwner, invoice, lastItemRow + 3);

        // Create and return memory stream
        var ms = new MemoryStream();
        workbook.Write(ms, leaveOpen: true);
        ms.Position = 0;
        return ms;
    }
    
    #endregion
    
    #region Configuration Methods
    
    private static void ConfigureA4PrintSettings(ISheet sheet)
    {
        sheet.PrintSetup.PaperSize = (short)PaperSize.A4;
        sheet.PrintSetup.Landscape = false;
        sheet.FitToPage = true;
        sheet.PrintSetup.FitWidth = 1;
        sheet.PrintSetup.FitHeight = 0;
        
        sheet.SetMargin(MarginType.LeftMargin, 0.5);
        sheet.SetMargin(MarginType.RightMargin, 0.5);
        sheet.SetMargin(MarginType.TopMargin, 0.5);
        sheet.SetMargin(MarginType.BottomMargin, 0.5);
    }

    private static void SetColumnWidths(ISheet sheet)
    {
        sheet.SetColumnWidth(0, 18 * 256);
        sheet.SetColumnWidth(1, 18 * 256);
        sheet.SetColumnWidth(2, 18 * 256);
        sheet.SetColumnWidth(3, 16 * 256);
        sheet.SetColumnWidth(4, 14 * 256);
        sheet.SetColumnWidth(5, 14 * 256);
    }
    
    #endregion

    #region Style Creation Methods
    
    private static CellStyles CreateCellStyles(IWorkbook workbook, IFont boldFont)
    {
        var headerStyle = workbook.CreateCellStyle();
        headerStyle.Alignment = HorizontalAlignment.Center;
        headerStyle.BorderTop = BorderStyle.Thin;
        headerStyle.BorderBottom = BorderStyle.Thin;
        headerStyle.BorderLeft = BorderStyle.Thin;
        headerStyle.BorderRight = BorderStyle.Thin;
        headerStyle.SetFont(boldFont);

        var bodyStyle = workbook.CreateCellStyle();
        bodyStyle.BorderLeft = BorderStyle.Thin;
        bodyStyle.BorderRight = BorderStyle.Thin;
        bodyStyle.Alignment = HorizontalAlignment.Center;

        var bodyLeftStyle = workbook.CreateCellStyle();
        bodyLeftStyle.BorderLeft = BorderStyle.Thin;
        bodyLeftStyle.BorderRight = BorderStyle.Thin;
        bodyLeftStyle.Alignment = HorizontalAlignment.Left;
        bodyLeftStyle.VerticalAlignment = VerticalAlignment.Top;
        bodyLeftStyle.WrapText = true;

        var totalLeftStyle = workbook.CreateCellStyle();
        totalLeftStyle.SetFont(boldFont);

        return new CellStyles
        {
            BoldFont = boldFont,
            HeaderStyle = headerStyle,
            BodyStyle = bodyStyle,
            BodyLeftStyle = bodyLeftStyle,
            TotalLeftStyle = totalLeftStyle
        };
    }
    
    #endregion

    #region Content Population Methods
    
    private static void PopulateBusinessOwnerInfo(ISheet sheet, BusinessOwner businessOwner, Invoice invoice)
    {
        SetStringCellValue(sheet, businessOwner.Email ?? "", 3, 3);
        SetStringCellValue(sheet, businessOwner.Address?.Street ?? "", 4, 3);
        SetStringCellValue(sheet, $"{businessOwner.Address?.PostalCode} {businessOwner.Address?.City}", 5, 3);
        SetStringCellValue(sheet, "Steuernummer", 6, 3);
        SetStringCellValue(sheet, businessOwner.TaxNumber ?? "", 6, 4);
        SetStringCellValue(sheet, "Rechnungsdatum", 7, 3);
        SetStringCellValue(sheet, invoice.InvoiceDate?.ToString("dd.MM.yyyy") ?? "", 7, 4);
        SetStringCellValue(sheet, "kundenNummer", 8, 3);
        SetStringCellValue(sheet, invoice.InvoiceNumber, 8, 4);
        SetStringCellValue(sheet, "Fälligkeitsdatum", 9, 3);
        SetStringCellValue(sheet, invoice.DueDate?.ToString("dd.MM.yyyy") ?? "", 9, 4);

        // Set ServiceDate with top vertical alignment
        var serviceDateLabelCell = SetStringCellValue(sheet, "Leistungsdatum", 10, 3);
        var serviceDateCell = SetStringCellValue(sheet, invoice.ServiceDate?.ToString("dd.MM.yyyy") ?? "", 10, 4);
        var topAlignStyle = sheet.Workbook.CreateCellStyle();
        topAlignStyle.VerticalAlignment = VerticalAlignment.Top;
        serviceDateCell.CellStyle = topAlignStyle;
        serviceDateLabelCell.CellStyle = topAlignStyle;
    }

    private static void PopulateClientInfo(ISheet sheet, Client client)
    {
        var salutation = client.Gender == Gender.Female ? "Frau" : "Herr";
        SetStringCellValue(sheet, salutation, 8, 0);
        SetStringCellValue(sheet, client.FullName, 9, 0);

        CreateWrappedStreetAddress(sheet, client.Address.Street, 10);
        SetStringCellValue(sheet, $"{client.Address.PostalCode} {client.Address.City}", 11, 0);
    }

    private static void CreateWrappedStreetAddress(ISheet sheet, string street, int rowIndex)
    {
        sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 0, 1));
        
        var wrappedStreet = WrapTextAtThreshold(street, TextWrapThreshold);
        var hasWrapped = wrappedStreet.Contains('\n');
        SetStringCellValue(sheet, wrappedStreet, rowIndex, 0);

        var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
        var cell = row.GetCell(0) ?? row.CreateCell(0);
        var wrapStyle = sheet.Workbook.CreateCellStyle();
        wrapStyle.WrapText = true;
        cell.CellStyle = wrapStyle;
        
        // Only increase height if text was actually wrapped
        if (hasWrapped)
        {
            row.HeightInPoints = 2 * sheet.DefaultRowHeightInPoints;
        }
    }

    private static int PopulateGreeting(ISheet sheet, Client client, int startRowIdx)
    {
        var rowIdx = startRowIdx + 2;
        var genderSuffix = client.Gender == Gender.Female ? " Frau" : "r Herr";
        
        SetStringCellValue(sheet, $"Sehr geehrte{genderSuffix} {client.FullName}", rowIdx, 0);
        rowIdx++;
        SetStringCellValue(sheet, "für die Erledigung der von Ihnen beauftragten Tätigkeiten berechne ich Ihnen wie folgt:", rowIdx, 0);
        
        return rowIdx + 2;
    }

    private static int PopulateInvoiceTable(ISheet sheet, Invoice invoice, CellStyles styles, int startRowIdx)
    {
        var rowIdx = CreateTableHeader(sheet, styles, startRowIdx);
        var lastItemRow = PopulateTableItems(sheet, invoice, styles, rowIdx);
        AddBottomBorderToLastRow(sheet, lastItemRow);
        
        return lastItemRow;
    }

    private static int CreateTableHeader(ISheet sheet, CellStyles styles, int rowIdx)
    {
        rowIdx++;
        sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(rowIdx, rowIdx, 1, 2));
        
        SetStringCellValue(sheet, "Position", rowIdx, 0);
        SetStringCellValue(sheet, "Beschreibung", rowIdx, 1);
        SetStringCellValue(sheet, "Menge", rowIdx, 3);
        SetStringCellValue(sheet, "Einzelpreis/€", rowIdx, 4);
        SetStringCellValue(sheet, "Gesamt/€", rowIdx, 5);

        for (int col = 0; col <= 5; col++)
        {
            var cell = sheet.GetRow(rowIdx).GetCell(col) ?? SetStringCellValue(sheet, "", rowIdx, col);
            
            if (col == 1 || col == 2)
            {
                var headerMergedStyle = sheet.Workbook.CreateCellStyle();
                headerMergedStyle.CloneStyleFrom(styles.HeaderStyle);
                headerMergedStyle.SetFont(styles.BoldFont);
                cell.CellStyle = headerMergedStyle;
            }
            else
            {
                cell.CellStyle = styles.HeaderStyle;
            }
        }
        
        return rowIdx;
    }

    private static int PopulateTableItems(ISheet sheet, Invoice invoice, CellStyles styles, int startRowIdx)
    {
        var rowIdx = startRowIdx;
        var pos = 1;
        var lastItemRow = 0;

        foreach (var item in invoice.Items)
        {
            rowIdx++;
            lastItemRow = rowIdx;
            var currentRow = sheet.GetRow(rowIdx) ?? sheet.CreateRow(rowIdx);

            SetDecimalCellValue(sheet, pos++, rowIdx, 0);

            var (description, lineCount) = ProcessDescription(item.Description);
            SetStringCellValue(sheet, description, rowIdx, 1);

            PopulateItemValues(sheet, item, rowIdx);
            ApplyItemRowStyles(sheet, styles, rowIdx);
            
            sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(rowIdx, rowIdx, 1, 2));

            if (lineCount > 1)
            {
                currentRow.HeightInPoints = lineCount * sheet.DefaultRowHeightInPoints;
            }
        }

        return lastItemRow;
    }

    private static (string description, int lineCount) ProcessDescription(string originalDescription)
    {
        if (string.IsNullOrEmpty(originalDescription) || originalDescription.Length <= DescriptionWrapThreshold)
        {
            return (originalDescription, 1);
        }

        var lines = originalDescription.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var wrappedDescription = new StringBuilder();
        var lineCount = 0;

        foreach (var line in lines)
        {
            var currentLine = line;
            while (currentLine.Length > DescriptionWrapThreshold)
            {
                var breakPos = currentLine.LastIndexOf(' ', DescriptionWrapThreshold);
                if (breakPos <= 0) breakPos = DescriptionWrapThreshold;
                
                wrappedDescription.AppendLine(currentLine.Substring(0, breakPos).Trim());
                currentLine = currentLine.Substring(breakPos).Trim();
                lineCount++;
            }
            wrappedDescription.AppendLine(currentLine);
            lineCount++;
        }

        return (wrappedDescription.ToString().Trim(), lineCount);
    }

    private static void PopulateItemValues(ISheet sheet, InvoiceItem item, int rowIdx)
    {
        switch (item.BillingType)
        {
            case BillingType.PerHour:
                SetDecimalCellValue(sheet, item.Quantity, rowIdx, 3);
                SetDecimalCellValue(sheet, item.UnitPrice, rowIdx, 4);
                break;
            case BillingType.PerSquareMeter:
                SetDecimalCellValue(sheet, item.Area, rowIdx, 3);
                SetDecimalCellValue(sheet, item.PricePerSquareMeter, rowIdx, 4);
                break;
            case BillingType.PerObject:
                SetDecimalCellValue(sheet, item.Quantity, rowIdx, 3);
                SetDecimalCellValue(sheet, item.UnitPrice, rowIdx, 4);
                break;
            case BillingType.FixedPrice:
                SetStringCellValue(sheet, "", rowIdx, 3);
                SetStringCellValue(sheet, "", rowIdx, 4);
                break;
        }
        SetDecimalCellValue(sheet, item.Total, rowIdx, 5);
    }

    private static void ApplyItemRowStyles(ISheet sheet, CellStyles styles, int rowIdx)
    {
        for (int col = 0; col <= 5; col++)
        {
            var cell = sheet.GetRow(rowIdx).GetCell(col) ?? SetStringCellValue(sheet, "", rowIdx, col);

            if (col == 1)
            {
                var descLeftStyle = sheet.Workbook.CreateCellStyle();
                descLeftStyle.CloneStyleFrom(styles.BodyLeftStyle);
                descLeftStyle.BorderLeft = BorderStyle.Thin;
                descLeftStyle.BorderTop = BorderStyle.Thin;
                descLeftStyle.BorderBottom = BorderStyle.Thin;
                descLeftStyle.BorderRight = BorderStyle.None;
                cell.CellStyle = descLeftStyle;
            }
            else if (col == 2)
            {
                var descRightStyle = sheet.Workbook.CreateCellStyle();
                descRightStyle.CloneStyleFrom(styles.BodyLeftStyle);
                descRightStyle.BorderLeft = BorderStyle.None;
                descRightStyle.BorderTop = BorderStyle.Thin;
                descRightStyle.BorderBottom = BorderStyle.Thin;
                descRightStyle.BorderRight = BorderStyle.Thin;
                cell.CellStyle = descRightStyle;
            }
            else
            {
                var descRightLeftStyle = sheet.Workbook.CreateCellStyle();
                descRightLeftStyle.CloneStyleFrom(styles.BodyLeftStyle);
                descRightLeftStyle.BorderLeft = BorderStyle.Thin;
                descRightLeftStyle.BorderTop = BorderStyle.Thin;
                descRightLeftStyle.BorderBottom = BorderStyle.Thin;
                descRightLeftStyle.BorderRight = BorderStyle.Thin;
                cell.CellStyle = descRightLeftStyle;
            }
        }
    }

    private static void AddBottomBorderToLastRow(ISheet sheet, int lastItemRow)
    {
        for (int col = 0; col <= 5; col++)
        {
            var cell = sheet.GetRow(lastItemRow).GetCell(col) ?? SetStringCellValue(sheet, "", lastItemRow, col);
            var bottomCellStyle = sheet.Workbook.CreateCellStyle();
            bottomCellStyle.CloneStyleFrom(cell.CellStyle);
            bottomCellStyle.BorderBottom = BorderStyle.Thin;
            cell.CellStyle = bottomCellStyle;
        }
    }

    private static void PopulateTotalRow(ISheet sheet, Invoice invoice, CellStyles styles, int rowIdx)
    {
        SetStringCellValue(sheet, "Rechnungsbetrag", rowIdx, 0);
        sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(rowIdx, rowIdx, 0, 4));
        SetDecimalCellValue(sheet, invoice.Items.Sum(i => i.Total), rowIdx, 5);

        var totalCellStyle = sheet.Workbook.CreateCellStyle();
        totalCellStyle.SetFont(styles.BoldFont);
        totalCellStyle.Alignment = HorizontalAlignment.Center;
        totalCellStyle.BorderTop = BorderStyle.Thin;
        totalCellStyle.BorderBottom = BorderStyle.Thin;
        totalCellStyle.BorderLeft = BorderStyle.Thin;
        totalCellStyle.BorderRight = BorderStyle.Thin;

        sheet.GetRow(rowIdx).GetCell(5).CellStyle = totalCellStyle;
        sheet.GetRow(rowIdx).GetCell(0).CellStyle = styles.TotalLeftStyle;
    }

    private static void PopulatePaymentInstructions(ISheet sheet, BusinessOwner businessOwner, Invoice invoice, int startRowIdx)
    {
        var rowIdx = startRowIdx;
        
        SetStringCellValue(sheet, "Vielen Dank für Ihren Auftrag!", rowIdx, 0);
        rowIdx++;
        SetStringCellValue(sheet, "Ich bitte um Überweisung des Rechnungsbetrages innerhalb von 14 Tagen an", rowIdx, 0);
        rowIdx++;
        
        SetStringCellValue(sheet, "Kontoinhaber", rowIdx, 0);
        SetStringCellValue(sheet, businessOwner.bankAccount?.AccountHolder ?? "", rowIdx, 1);
        rowIdx++;
        
        SetStringCellValue(sheet, "IBAN", rowIdx, 0);
        SetStringCellValue(sheet, businessOwner.bankAccount?.IBAN ?? "", rowIdx, 1);
        rowIdx++;
        
        SetStringCellValue(sheet, "Bic", rowIdx, 0);
        SetStringCellValue(sheet, businessOwner.bankAccount?.BIC ?? "", rowIdx, 1);
        rowIdx++;
        
        SetStringCellValue(sheet, "Verwendungszweck", rowIdx, 0);
        SetStringCellValue(sheet, invoice.InvoiceNumber, rowIdx, 1);
        rowIdx += 2;
        
        SetStringCellValue(sheet, "Hinweis: Als Kleinunternehmer im Sinne von § 19 Abs. 1 UStG wird Umsatzsteuer nicht berechnet", rowIdx, 0);
    }
    
    #endregion

    #region Utility Methods
    
    private static string WrapTextAtThreshold(string text, int threshold)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= threshold) 
            return text;

        var breakPos = text.LastIndexOf(' ', threshold);
        if (breakPos > 0)
        {
            return text.Substring(0, breakPos) + "\n" + text.Substring(breakPos + 1);
        }
        return text;
    }
    
    #endregion

    #region Cell Value Helper Methods

    public static ICell SetStringCellValue(ISheet sheet, string value, int row, int col)
    {
        var r = sheet.GetRow(row) ?? sheet.CreateRow(row);
        var cell = r.GetCell(col) ?? r.CreateCell(col);
        cell.SetCellValue(value);
        return cell;
    }

    public static void SetDecimalCellValue(ISheet sheet, decimal value, int row, int col)
    {
        var r = sheet.GetRow(row) ?? sheet.CreateRow(row);
        var cell = r.GetCell(col) ?? r.CreateCell(col);
        cell.SetCellValue((double)value);
    }
    
    #endregion

    #region Helper Classes

    private class CellStyles
    {
        public IFont BoldFont { get; set; } = null!;
        public ICellStyle HeaderStyle { get; set; } = null!;
        public ICellStyle BodyStyle { get; set; } = null!;
        public ICellStyle BodyLeftStyle { get; set; } = null!;
        public ICellStyle TotalLeftStyle { get; set; } = null!;
    }
    
    #endregion
}
