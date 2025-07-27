using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClivoxApp
{
    internal class ExcelExporter
    {
        public static void ExportToExcel()
        {
            // Create a new workbook and a sheet
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

            for (int i = 0; i < data.GetLength(0); i++)
            {
                IRow row = sheet.CreateRow(i);
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    row.CreateCell(j).SetCellValue(data[i, j]);
                }
            }

            // Insert image (bache_enduit.png) into the Excel file
            string imagePath = "companyLogo.png";
            if (File.Exists(imagePath))
            {
                byte[] bytes = File.ReadAllBytes(imagePath);
                int pictureIdx = workbook.AddPicture(bytes, PictureType.PNG);
                var patriarch = sheet.CreateDrawingPatriarch();
                var anchor = workbook.GetCreationHelper().CreateClientAnchor();
                anchor.Col1 = 4; // Column A
                anchor.Row1 = data.GetLength(0) + 1; // Below the data

                // Set the image size (smaller)
                anchor.Col2 = 7; // End at column D (A-D)
                anchor.Row2 = anchor.Row1 + 7; // Span 10 rows

                var pict = patriarch.CreatePicture(anchor, pictureIdx);
                // Do not call pict.Resize() to keep the anchor size
            }
            else
            {
                System.Console.WriteLine($"Image file '{imagePath}' not found.");
            }

            // Save the workbook to a file
            using (var fs = new FileStream(@"C:\Users\hamdi\Downloads\Documents\SampleData1.xlsx", FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fs);
            }

            System.Console.WriteLine("Excel file 'SampleData.xlsx' generated successfully.");
        }
    }
}
