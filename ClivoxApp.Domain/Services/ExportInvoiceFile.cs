using ClivoxApp.Models;
using ClivoxApp.Models.Clients;
using ClivoxApp.Models.Invoice;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.IO;

namespace ClivoxApp.Services;

public class ExportInvoiceFile
{
    private static void AddCompanyLogo(IWorkbook workbook, ISheet sheet, int colStartPos, int colEndPos, int rowStartPos, int rowEndPos)
    {
        // Insert image (companyLogo.png) into the Excel file
        string imagePath = "companyLogo.png";
        if (File.Exists(imagePath))
        {
            byte[] bytes = File.ReadAllBytes(imagePath);
            int pictureIdx = workbook.AddPicture(bytes, PictureType.PNG);
            var patriarch = sheet.CreateDrawingPatriarch();
            var anchor = workbook.GetCreationHelper().CreateClientAnchor();
            anchor.Col1 = colStartPos; // Column E
            anchor.Row1 = rowStartPos; // Below the data

            // Set the image size (smaller)
            anchor.Col2 = colEndPos; 
            anchor.Row2 = rowEndPos; // Span 6 rows

            var pict = patriarch.CreatePicture(anchor, pictureIdx);
            // Do not call pict.Resize() to keep the anchor size
        }
    }
    public static MemoryStream ExportToExcel(Invoice invoice, Client client, BusinessOwner businessOwner)
    {
        IWorkbook workbook = new XSSFWorkbook();
        ISheet sheet = workbook.CreateSheet("SampleSheet");

        AddCompanyLogo(workbook, sheet, 0, 2, 0, 5);

        // Set up A4 print settings
        sheet.PrintSetup.PaperSize = (short)PaperSize.A4;
        sheet.PrintSetup.Landscape = false; // Portrait
        sheet.FitToPage = true;
        sheet.PrintSetup.FitWidth = 1;
        sheet.PrintSetup.FitHeight = 0;
        sheet.SetMargin(MarginType.LeftMargin, 0.5);
        sheet.SetMargin(MarginType.RightMargin, 0.5);
        sheet.SetMargin(MarginType.TopMargin, 0.5);
        sheet.SetMargin(MarginType.BottomMargin, 0.5);

        // Add invoice details here
        // Set column widths for better appearance
        int cellWidth = 18 * 256; // 18 characters wide
        sheet.SetColumnWidth(0, 18 * 256);
        sheet.SetColumnWidth(1, 18 * 256);
        sheet.SetColumnWidth(2, 18 * 256);
        sheet.SetColumnWidth(3, 16 * 256);
        sheet.SetColumnWidth(4, 14 * 256);
        sheet.SetColumnWidth(5, 14 * 256);

        // Business owner info (right)
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
        SetStringCellValue(sheet, "Leistungsdatum", 10, 3);
        SetStringCellValue(sheet, invoice.ServiceDate?.ToString("dd.MM.yyyy") ?? "", 10, 4);

        // Client info (left)
        string salutation = "Herr";
        if (client.Gender == Gender.Female)
            salutation = "Frau";
        SetStringCellValue(sheet, salutation, 8, 0);
        SetStringCellValue(sheet, client.FullName, 9, 0);

        // Merge cells for the street (columns 0 and 1 in row 10)
        sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(10, 10, 0, 1));
        // Pseudocode plan:
        // 1. Check if the street text length exceeds a threshold (e.g., 30 chars).
        // 2. If so, insert a line break at the last space before the threshold.
        // 3. Set the cell value with the possibly multi-line string.
        // 4. Set the cell style to wrap text for the merged cell.

        string street = client.Address.Street;
        int wrapThreshold = 30;
        if (!string.IsNullOrEmpty(street) && street.Length > wrapThreshold)
        {
            int breakPos = street.LastIndexOf(' ', wrapThreshold);
            if (breakPos > 0)
                street = street.Substring(0, breakPos) + "\n" + street.Substring(breakPos + 1);
        }

        // Set the value (possibly with line break)
        SetStringCellValue(sheet, street, 10, 0);

        // Ensure the merged cell wraps text and set row height
        var row10 = sheet.GetRow(10) ?? sheet.CreateRow(10);
        var cellStreet = row10.GetCell(0) ?? row10.CreateCell(0);
        var wrapStyle = sheet.Workbook.CreateCellStyle();
        wrapStyle.WrapText = true;
        cellStreet.CellStyle = wrapStyle;
        // Set row height to accommodate two lines of text
        row10.HeightInPoints = 2 * sheet.DefaultRowHeightInPoints;
        SetStringCellValue(sheet, $"{client.Address.PostalCode} {client.Address.City}", 11, 0);

        int rowIdx = 11;
        // Invoice meta info (right)
        int metaStart = 8;
        for (int i = 0; i < 5; i++)
        {
            if (sheet.GetRow(metaStart + i) == null)
                sheet.CreateRow(metaStart + i);
        }
        

        // Salutation and description
        rowIdx += 2;
        SetStringCellValue(sheet, $"Sehr geehrte{(client.Gender == Gender.Female ? " Frau" : "r Herr")} {client.FullName}", rowIdx, 0);
        rowIdx++;
        SetStringCellValue(sheet, "für die Erledigung der von Ihnen beauftragten Tätigkeiten berechne ich Ihnen wie folgt:", rowIdx, 0);
        rowIdx++;

        // Create a centered cell style for the Position column
        var centerStyle = workbook.CreateCellStyle();
        centerStyle.Alignment = HorizontalAlignment.Center;
        
        // Create fonts
var boldFont = workbook.CreateFont();
boldFont.IsBold = true;

// Header style: all borders, bold
var headerStyle = workbook.CreateCellStyle();
headerStyle.Alignment = HorizontalAlignment.Center;
headerStyle.BorderTop = BorderStyle.Thin;
headerStyle.BorderBottom = BorderStyle.Thin;
headerStyle.BorderLeft = BorderStyle.Thin;
headerStyle.BorderRight = BorderStyle.Thin;
headerStyle.SetFont(boldFont);

// Body style: only left/right borders, centered
var bodyStyle = workbook.CreateCellStyle();
bodyStyle.BorderLeft = BorderStyle.Thin;
bodyStyle.BorderRight = BorderStyle.Thin;
bodyStyle.Alignment = HorizontalAlignment.Center;

// Body style for description: only left/right borders, left aligned, wrap text
var bodyLeftStyle = workbook.CreateCellStyle();
bodyLeftStyle.BorderLeft = BorderStyle.Thin;
bodyLeftStyle.BorderRight = BorderStyle.Thin;
bodyLeftStyle.Alignment = HorizontalAlignment.Left;
bodyLeftStyle.VerticalAlignment = VerticalAlignment.Top;
bodyLeftStyle.WrapText = true;

// Bottom border style: all borders thin
var bottomStyle = workbook.CreateCellStyle();
bottomStyle.BorderTop = BorderStyle.None;
bottomStyle.BorderBottom = BorderStyle.Thin;
bottomStyle.BorderLeft = BorderStyle.Thin;
bottomStyle.BorderRight = BorderStyle.Thin;

// Rechnungsbetrag left: bold, no border
var totalLeftStyle = workbook.CreateCellStyle();
totalLeftStyle.SetFont(boldFont);

// Rechnungsbetrag right: top border
var totalRightStyle = workbook.CreateCellStyle();
totalRightStyle.BorderTop = BorderStyle.Thin;

// Table header
        rowIdx++;
        // Merge header cells for Beschreibung (B+C)
        sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(rowIdx, rowIdx, 1, 2));
        SetStringCellValue(sheet, "Position", rowIdx, 0);
        SetStringCellValue(sheet, "Beschreibung", rowIdx, 1);
        SetStringCellValue(sheet, "Menge", rowIdx, 3);
        SetStringCellValue(sheet, "Einzelpreis/€", rowIdx, 4);
        SetStringCellValue(sheet, "Gesamt/€", rowIdx, 5);
        // Style header, ensure right border on both B and C
        for (int col = 0; col <= 5; col++)
        {
            var cell = sheet.GetRow(rowIdx).GetCell(col);
            if (cell == null)
            {
                cell = SetStringCellValue(sheet, "", rowIdx, col);
            }
            if (cell != null)
            {
                if (col == 1 || col == 2) {
                    var headerMergedStyle = workbook.CreateCellStyle();
                    headerMergedStyle.CloneStyleFrom(headerStyle);
                    headerMergedStyle.BorderRight = BorderStyle.Thin;
                    headerMergedStyle.BorderTop = BorderStyle.Thin;
                    headerMergedStyle.BorderBottom = BorderStyle.Thin;
                    headerMergedStyle.BorderLeft = BorderStyle.Thin;
                    headerMergedStyle.SetFont(boldFont);
                    cell.CellStyle = headerMergedStyle;
                } else {
                    cell.CellStyle = headerStyle;
                }
            }
        }

        // Table items
        int pos = 1;
        int lastItemRow = 0;
        foreach (var item in invoice.Items)
        {
            rowIdx++;
            lastItemRow = rowIdx;
            var currentRow = sheet.GetRow(rowIdx) ?? sheet.CreateRow(rowIdx);

            SetDecimalCellValue(sheet, pos++, rowIdx, 0);

            // Wrap description text if it's too long
            string description = item.Description;
            int lineCount = 1;
            if (!string.IsNullOrEmpty(description) && description.Length > 40) // Threshold for description column
            {
                var lines = description.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                var wrappedDescription = new System.Text.StringBuilder();
                foreach(var line in lines)
                {
                    string currentLine = line;
                    while(currentLine.Length > 40)
                    {
                        int breakPos = currentLine.LastIndexOf(' ', 40);
                        if (breakPos <= 0) breakPos = 40;
                        wrappedDescription.AppendLine(currentLine.Substring(0, breakPos).Trim());
                        currentLine = currentLine.Substring(breakPos).Trim();
                        lineCount++;
                    }
                    wrappedDescription.AppendLine(currentLine);
                }
                description = wrappedDescription.ToString().Trim();
                lineCount = description.Split('\n').Length;
            }
            
            SetStringCellValue(sheet, description, rowIdx, 1);

            // Quantity/Area in D
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

            // Apply body style to all cells in this row
            for (int col = 0; col <= 5; col++)
            {
                var cell = sheet.GetRow(rowIdx).GetCell(col);
                if (cell == null)
                {
                    cell = SetStringCellValue(sheet, "", rowIdx, col);
                }
                if (cell != null)
                {
                    if (col == 1)
                    {
                        // Description column B: left border, top/bottom, no right
                        var descLeftStyle = workbook.CreateCellStyle();
                        descLeftStyle.CloneStyleFrom(bodyLeftStyle);
                        descLeftStyle.BorderLeft = BorderStyle.Thin;
                        descLeftStyle.BorderTop = BorderStyle.Thin;
                        descLeftStyle.BorderBottom = BorderStyle.Thin;
                        descLeftStyle.BorderRight = BorderStyle.None;
                        cell.CellStyle = descLeftStyle;
                    }
                    else if (col == 2)
                    {
                        // Description column C: right border, top/bottom, no left  
                        var descRightStyle = workbook.CreateCellStyle();
                        descRightStyle.CloneStyleFrom(bodyLeftStyle);
                        descRightStyle.BorderLeft = BorderStyle.None;
                        descRightStyle.BorderTop = BorderStyle.Thin;
                        descRightStyle.BorderBottom = BorderStyle.Thin;
                        descRightStyle.BorderRight = BorderStyle.Thin;
                        cell.CellStyle = descRightStyle;
                    }
                    else
                    {
                        cell.CellStyle = bodyStyle;
                    }
                }
            }
            // Merge description cells (B+C) AFTER setting styles
            sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(rowIdx, rowIdx, 1, 2));

            // Adjust row height if text is wrapped
            if (lineCount > 1)
            {
                currentRow.HeightInPoints = lineCount * sheet.DefaultRowHeightInPoints;
            }
        }

// Add bottom border to the last table row
for (int col = 0; col <= 5; col++)
{
    var cell = sheet.GetRow(lastItemRow).GetCell(col);
    if (cell == null)
            {
                cell = SetStringCellValue(sheet, "", lastItemRow, col);
            }
    if (cell != null)
    {
        var bottomCellStyle = workbook.CreateCellStyle();
        bottomCellStyle.CloneStyleFrom(cell.CellStyle);
        bottomCellStyle.BorderBottom = BorderStyle.Thin;
        cell.CellStyle = bottomCellStyle;
    }
}
        // Rechnungsbetrag row
        rowIdx++;
SetStringCellValue(sheet, "Rechnungsbetrag", rowIdx, 0);
sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(rowIdx, rowIdx, 0, 4));
SetDecimalCellValue(sheet, invoice.Items.Sum(i => i.Total), rowIdx, 5);
// Create a style with all borders and bold for the total cell
var totalCellStyle = workbook.CreateCellStyle();
totalCellStyle.SetFont(boldFont);
totalCellStyle.Alignment = HorizontalAlignment.Center;
totalCellStyle.BorderTop = BorderStyle.Thin;
totalCellStyle.BorderBottom = BorderStyle.Thin;
totalCellStyle.BorderLeft = BorderStyle.Thin;
totalCellStyle.BorderRight = BorderStyle.Thin;
sheet.GetRow(rowIdx).GetCell(5).CellStyle = totalCellStyle;
sheet.GetRow(rowIdx).GetCell(0).CellStyle = totalLeftStyle;

// Payment instructions
        rowIdx += 2;
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

        // Legal note
        rowIdx += 2;
        SetStringCellValue(sheet, "Hinweis: Als Kleinunternehmer im Sinne von § 19 Abs. 1 UStG wird Umsatzsteuer nicht berechnet", rowIdx, 0);

        // Save the workbook to a memory stream
        var ms = new MemoryStream();
        workbook.Write(ms, leaveOpen: true);
        ms.Position = 0;
        return ms;
    }

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
}
