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
        for (int i = 0; i <= 7; i++)
            sheet.SetColumnWidth(i, 18 * 256);

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
        SetStringCellValue(sheet, client.Address.Street, 10, 0);
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

        // Table header
        rowIdx++;
        SetStringCellValue(sheet, $"Rechnung Nr.   {invoice.InvoiceNumber}", rowIdx, 0);
        rowIdx++;
        SetStringCellValue(sheet, "Position", rowIdx, 0);
        // Create a centered cell style for the Position column
        var centerStyle = workbook.CreateCellStyle();
        centerStyle.Alignment = HorizontalAlignment.Center;
        sheet.GetRow(rowIdx).GetCell(0).CellStyle = centerStyle;
        SetStringCellValue(sheet, "Beschreibung", rowIdx, 1);
        sheet.GetRow(rowIdx).GetCell(1).CellStyle = centerStyle;
        SetStringCellValue(sheet, "Menge", rowIdx, 2);
        sheet.GetRow(rowIdx).GetCell(2).CellStyle = centerStyle;
        SetStringCellValue(sheet, "Einzelpreis/€", rowIdx, 3);
        sheet.GetRow(rowIdx).GetCell(3).CellStyle = centerStyle;
        SetStringCellValue(sheet, "Gesamt/€", rowIdx, 4);
        sheet.GetRow(rowIdx).GetCell(4).CellStyle = centerStyle;

        // Table items
        int pos = 1;
        foreach (var item in invoice.Items)
        {
            rowIdx++;
            SetDecimalCellValue(sheet, pos++, rowIdx, 0);
            sheet.GetRow(rowIdx).GetCell(0).CellStyle = centerStyle;
            
            SetStringCellValue(sheet, item.Description, rowIdx, 1);
            sheet.GetRow(rowIdx).GetCell(1).CellStyle = centerStyle;
            switch (item.BillingType)
            {
                case BillingType.PerHour:
                    SetDecimalCellValue(sheet, item.Quantity, rowIdx, 2);
                    SetDecimalCellValue(sheet, item.UnitPrice, rowIdx, 3);
                    break;
                case BillingType.PerSquareMeter:
                    SetDecimalCellValue(sheet, item.Area, rowIdx, 2);
                    SetDecimalCellValue(sheet, item.PricePerSquareMeter, rowIdx, 3);
                    break;
                case BillingType.PerObject:
                    SetDecimalCellValue(sheet, item.Quantity, rowIdx, 2);
                    SetDecimalCellValue(sheet, item.UnitPrice, rowIdx, 3);
                    break;
                case BillingType.FixedPrice:
                    SetStringCellValue(sheet, "", rowIdx, 2);
                    SetStringCellValue(sheet, "", rowIdx, 3);
                    break;
            }
            sheet.GetRow(rowIdx).GetCell(2).CellStyle = centerStyle;
            sheet.GetRow(rowIdx).GetCell(3).CellStyle = centerStyle;
            
            SetDecimalCellValue(sheet, item.Total, rowIdx, 4);
            sheet.GetRow(rowIdx).GetCell(4).CellStyle = centerStyle;
        }

        // Total row
        rowIdx++;
        SetStringCellValue(sheet, "Rechnungsbetrag", rowIdx, 0);
        SetDecimalCellValue(sheet, invoice.Items.Sum(i => i.Total), rowIdx, 4);
        sheet.GetRow(rowIdx).GetCell(4).CellStyle = centerStyle;

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

    public static void SetStringCellValue(ISheet sheet, string value, int row, int col)
    {
        var r = sheet.GetRow(row) ?? sheet.CreateRow(row);
        var cell = r.GetCell(col) ?? r.CreateCell(col);
        cell.SetCellValue(value);
    }

    public static void SetDecimalCellValue(ISheet sheet, decimal value, int row, int col)
    {
        var r = sheet.GetRow(row) ?? sheet.CreateRow(row);
        var cell = r.GetCell(col) ?? r.CreateCell(col);
        cell.SetCellValue((double)value);
    }
}
