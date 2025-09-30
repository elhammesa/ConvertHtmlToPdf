using ClosedXML.Excel;

namespace BlazorAppHtmlToPdf.Services
{
    public class ExcelService : IExcelService
    {
        public async Task<byte[]> GenerateExcelFromModelAsync<T>(List<T> data)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Data");

            // اگر داده‌ای وجود ندارد
            if (data == null || !data.Any())
            {
                worksheet.Cell(1, 1).Value = "No data available";
                return SaveWorkbookToBytes(workbook);
            }

            // گرفتن propertyها
            var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .ToList();

            // ایجاد header
            for (int i = 0; i < properties.Count; i++)
            {
        
                worksheet.Cell(1, i + 1).Value = properties[i].Name;
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            }

            // پر کردن داده‌ها
            for (int row = 0; row < data.Count; row++)
            {
                for (int col = 0; col < properties.Count; col++)
                {
                    var value = properties[col].GetValue(data[row])?.ToString() ?? "";
                    worksheet.Cell(row + 2, col + 1).Value = value;
                }
            }

            // تنظیم اتوسایز ستون‌ها
            worksheet.Columns().AdjustToContents();

            return SaveWorkbookToBytes(workbook);
        }

        public async Task<byte[]> GenerateExcelFromHtmlTableAsync(string htmlContent)
        {
            // این متد نیاز به پارسر HTML دارد
            // می‌توانید از HtmlAgilityPack استفاده کنید
            throw new NotImplementedException("این قابلیت نیاز به پیاده‌سازی پارسر HTML دارد");
        }

        public async Task<string> GenerateExcelBase64Async<T>(List<T> data)
        {
            var excelBytes = await GenerateExcelFromModelAsync(data);
            return Convert.ToBase64String(excelBytes);
        }

        private byte[] SaveWorkbookToBytes(XLWorkbook workbook)
        {
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

      
    }
}

