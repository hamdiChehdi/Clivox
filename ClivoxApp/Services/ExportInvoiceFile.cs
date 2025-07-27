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
    public static MemoryStream ExportToExcel(Invoice invoice, Client client, BusinessOwner businessOwner)
    {
        IWorkbook workbook = new XSSFWorkbook();
        ISheet sheet = workbook.CreateSheet("SampleSheet");

        // Insert image (companyLogo.png) into the Excel file
        string imagePath = "companyLogo.png";
        if (File.Exists(imagePath))
        {
            byte[] bytes = File.ReadAllBytes(imagePath);
            int pictureIdx = workbook.AddPicture(bytes, PictureType.PNG);
            var patriarch = sheet.CreateDrawingPatriarch();
            var anchor = workbook.GetCreationHelper().CreateClientAnchor();
            anchor.Col1 = 4; // Column E
            anchor.Row1 = 1; // Below the data

            // Set the image size (smaller)
            anchor.Col2 = 7; // End at column H
            anchor.Row2 = anchor.Row1 + 7; // Span 7 rows

            var pict = patriarch.CreatePicture(anchor, pictureIdx);
            // Do not call pict.Resize() to keep the anchor size
        }

        // Add invoice details here
        // Set column widths for better appearance
        for (int i = 0; i <= 7; i++)
            sheet.SetColumnWidth(i, 20 * 256);

        int rowIdx = 0;
        // Logo is already inserted at (E2:H8), so start after that
        rowIdx = 8;

        // Business owner info (left)
        sheet.CreateRow(rowIdx).CreateCell(0).SetCellValue(businessOwner.CompanyName ?? "");
        sheet.GetRow(rowIdx).CreateCell(4).SetCellValue(businessOwner.Email ?? "");
        rowIdx++;
        sheet.CreateRow(rowIdx).CreateCell(0).SetCellValue(businessOwner.PhoneNumber ?? "");
        sheet.GetRow(rowIdx).CreateCell(4).SetCellValue(businessOwner.Address?.Street ?? "");
        rowIdx++;
        sheet.CreateRow(rowIdx).CreateCell(0).SetCellValue(businessOwner.Address?.Street ?? "");
        sheet.GetRow(rowIdx).CreateCell(4).SetCellValue($"{businessOwner.Address?.PostalCode} {businessOwner.Address?.City}");
        rowIdx++;

        // Client info (left)
        rowIdx++;
        sheet.CreateRow(rowIdx).CreateCell(0).SetCellValue("Herr");
        rowIdx++;
        sheet.CreateRow(rowIdx).CreateCell(0).SetCellValue(client.FullName);
        rowIdx++;
        sheet.CreateRow(rowIdx).CreateCell(0).SetCellValue(client.Address.Street);
        rowIdx++;
        sheet.CreateRow(rowIdx).CreateCell(0).SetCellValue($"{client.Address.PostalCode} {client.Address.City}");

        // Invoice meta info (right)
        int metaStart = 8;
        for (int i = 0; i < 5; i++)
        {
            if (sheet.GetRow(metaStart + i) == null)
                sheet.CreateRow(metaStart + i);
        }
        sheet.GetRow(metaStart).CreateCell(6).SetCellValue("Steuernummer");
        sheet.GetRow(metaStart).CreateCell(7).SetCellValue(businessOwner.TaxNumber ?? "");
        sheet.GetRow(metaStart+1).CreateCell(6).SetCellValue("Rechnungsdatum");
        sheet.GetRow(metaStart+1).CreateCell(7).SetCellValue(invoice.InvoiceDate?.ToString("dd.MM.yyyy") ?? "");
        sheet.GetRow(metaStart+2).CreateCell(6).SetCellValue("kundenNummer");
        sheet.GetRow(metaStart+2).CreateCell(7).SetCellValue(invoice.InvoiceNumber);
        sheet.GetRow(metaStart+3).CreateCell(6).SetCellValue("Fälligkeitsdatum");
        sheet.GetRow(metaStart+3).CreateCell(7).SetCellValue(invoice.DueDate?.ToString("dd.MM.yyyy") ?? "");
        sheet.GetRow(metaStart+4).CreateCell(6).SetCellValue("Leistungsdatum");
        sheet.GetRow(metaStart+4).CreateCell(7).SetCellValue(invoice.ServiceDate?.ToString("dd.MM.yyyy") ?? "");

        // Salutation and description
        rowIdx += 2;
        sheet.CreateRow(rowIdx).CreateCell(0).SetCellValue($"Sehr geehrter Herr {client.FullName}");
        rowIdx++;
        sheet.CreateRow(rowIdx).CreateCell(0).SetCellValue("für die Erledigung der von Ihnen beauftragten Tätigkeiten berechne ich Ihnen wie folgt:");
        rowIdx++;

        // Table header
        rowIdx++;
        var headerRow = sheet.CreateRow(rowIdx);
        headerRow.CreateCell(0).SetCellValue($"Rechnung Nr.   {invoice.InvoiceNumber}");
        rowIdx++;
        var tableHeader = sheet.CreateRow(rowIdx);
        tableHeader.CreateCell(0).SetCellValue("Position");
        tableHeader.CreateCell(1).SetCellValue("Beschreibung");
        tableHeader.CreateCell(2).SetCellValue("Menge");
        tableHeader.CreateCell(3).SetCellValue("Einzelpreis/€");
        tableHeader.CreateCell(4).SetCellValue("Gesamt/€");

        // Table items
        int pos = 1;
        foreach (var item in invoice.Items)
        {
            rowIdx++;
            var row = sheet.CreateRow(rowIdx);
            row.CreateCell(0).SetCellValue(pos++);
            row.CreateCell(1).SetCellValue(item.Description);
            switch (item.BillingType)
            {
                case BillingType.PerHour:
                    row.CreateCell(2).SetCellValue((double)item.Quantity);
                    row.CreateCell(3).SetCellValue(item.UnitPrice.ToString("C"));
                    break;
                case BillingType.PerSquareMeter:
                    row.CreateCell(2).SetCellValue((double)item.Area);
                    row.CreateCell(3).SetCellValue(item.PricePerSquareMeter.ToString("C"));
                    break;
                case BillingType.PerObject:
                    row.CreateCell(2).SetCellValue((double)item.Quantity);
                    row.CreateCell(3).SetCellValue(item.UnitPrice.ToString("C"));
                    break;
                case BillingType.FixedPrice:
                    row.CreateCell(2).SetCellValue("");
                    row.CreateCell(3).SetCellValue("");
                    break;
            }
            row.CreateCell(4).SetCellValue(item.Total.ToString("C"));
        }

        // Total row
        rowIdx++;
        var totalRow = sheet.CreateRow(rowIdx);
        totalRow.CreateCell(0).SetCellValue("Rechnungsbetrag");
        totalRow.CreateCell(4).SetCellValue(invoice.Items.Sum(i => i.Total).ToString("C"));

        // Payment instructions
        rowIdx += 2;
        sheet.CreateRow(rowIdx).CreateCell(0).SetCellValue("Vielen Dank für Ihren Auftrag!");
        rowIdx++;
        sheet.CreateRow(rowIdx).CreateCell(0).SetCellValue("Ich bitte um Überweisung des Rechnungsbetrages innerhalb von 14 Tagen an");
        rowIdx++;
        sheet.CreateRow(rowIdx).CreateCell(0).SetCellValue("Kontoinhaber");
        sheet.GetRow(rowIdx).CreateCell(1).SetCellValue(businessOwner.bankAccount?.AccountHolder ?? "");
        rowIdx++;
        sheet.CreateRow(rowIdx).CreateCell(0).SetCellValue("IBAN");
        sheet.GetRow(rowIdx).CreateCell(1).SetCellValue(businessOwner.bankAccount?.IBAN ?? "");
        rowIdx++;
        sheet.CreateRow(rowIdx).CreateCell(0).SetCellValue("Bic");
        sheet.GetRow(rowIdx).CreateCell(1).SetCellValue(businessOwner.bankAccount?.BIC ?? "");
        rowIdx++;
        sheet.CreateRow(rowIdx).CreateCell(0).SetCellValue("Verwendungszweck");
        sheet.GetRow(rowIdx).CreateCell(1).SetCellValue(invoice.InvoiceNumber);

        // Legal note
        rowIdx += 2;
        sheet.CreateRow(rowIdx).CreateCell(0).SetCellValue("Hinweis: Als Kleinunternehmer im Sinne von § 19 Abs. 1 UStG wird Umsatzsteuer nicht berechnet");

        // Save the workbook to a memory stream
        var ms = new MemoryStream();
        workbook.Write(ms, leaveOpen: true);
        ms.Position = 0;
        return ms;
    }
}
