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

        // Add some sample data
        var data = new string[,]
        {
            { "Name", "Age", "Country" },
            { "Alice", "30", "USA" },
            { "Bob", "25", "UK" },
            { "Charlie", "28", "Canada" }
        };

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
        //for (int i = 0; i < data.GetLength(0); i++)
        //{
        //    IRow row = sheet.CreateRow(i);
        //    for (int j = 0; j < data.GetLength(1); j++)
        //    {
        //        row.CreateCell(j).SetCellValue(data[i, j]);
        //    }
        //}

        // Save the workbook to a memory stream
        var ms = new MemoryStream();
        workbook.Write(ms);
        ms.Position = 0;
        return ms;
    }
}
