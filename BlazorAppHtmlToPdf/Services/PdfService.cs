using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace BlazorAppHtmlToPdf.Components.Services
{
    public class PdfService : IPdfService
    {
        private readonly IWebHostEnvironment _environment;

        public PdfService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }
        public async Task<byte[]> GeneratePdfFromModelAsync<T>(T model)
        {
            var htmlContent = GenerateHtmlFromModel(model);
            return await GeneratePdfFromHtmlAsync(htmlContent);
        }

        public async Task<string> GeneratePdfBase64Async<T>(T model)
        {
            var pdfBytes = await GeneratePdfFromModelAsync(model);
            return Convert.ToBase64String(pdfBytes);
        }

        private string GenerateHtmlFromModel<T>(T model)
        {
           
            const string templateName = "TableTemplate";

            //  خواندن از فایل تمپلیت
            var templatePath = Path.Combine(_environment.WebRootPath, "Templates", $"{templateName}.html");
            if (File.Exists(templatePath))
            {
                var template = File.ReadAllText(templatePath);
                return FillTemplate(template, model);
            }

            return GenerateDefaultHtmlTemplate(model);
        }

        private string GenerateDefaultHtmlTemplate<T>(T model)
        {
            var modelType = typeof(T);
            var properties = modelType.GetProperties();

            var htmlBuilder = new StringBuilder();
            htmlBuilder.AppendLine("<!DOCTYPE html>");
            htmlBuilder.AppendLine("<html lang='fa' dir='rtl'>");
            htmlBuilder.AppendLine("<head>");
            htmlBuilder.AppendLine("<meta charset='UTF-8'>");
            htmlBuilder.AppendLine("<title>گزارش</title>");
            htmlBuilder.AppendLine("<style>");
            htmlBuilder.AppendLine("body { font-family: 'Tahoma', 'Arial', sans-serif; direction: rtl; background: #f8f9fa; }");
            htmlBuilder.AppendLine(".container { max-width: 800px; margin: 20px auto; padding: 20px; background: white; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }");
            htmlBuilder.AppendLine(".header { text-align: center; margin-bottom: 30px; border-bottom: 2px solid #007bff; padding-bottom: 15px; }");
            htmlBuilder.AppendLine(".header h1 { color: #007bff; margin: 0; }");
            htmlBuilder.AppendLine(".info-table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
            htmlBuilder.AppendLine(".info-table th { background: #007bff; color: white; padding: 12px; text-align: right; }");
            htmlBuilder.AppendLine(".info-table td { padding: 12px; border-bottom: 1px solid #ddd; }");
            htmlBuilder.AppendLine(".info-table tr:nth-child(even) { background: #f8f9fa; }");
            htmlBuilder.AppendLine(".footer { margin-top: 30px; text-align: center; color: #666; font-size: 12px; }");
            htmlBuilder.AppendLine("</style>");
            htmlBuilder.AppendLine("</head>");
            htmlBuilder.AppendLine("<body>");
            htmlBuilder.AppendLine("<div class='container'>");
            htmlBuilder.AppendLine("<div class='header'>");
            htmlBuilder.AppendLine($"<h1>گزارش اطلاعات {modelType.Name}</h1>");
            htmlBuilder.AppendLine("</div>");

            htmlBuilder.AppendLine("<table class='info-table'>");
            htmlBuilder.AppendLine("<tr><th>ویژگی</th><th>مقدار</th></tr>");

            foreach (var property in properties)
            {
                var value = property.GetValue(model)?.ToString() ?? "-";
                htmlBuilder.AppendLine($"<tr><td><strong>{property.Name}</strong></td><td>{value}</td></tr>");
            }

            htmlBuilder.AppendLine("</table>");
            htmlBuilder.AppendLine("<div class='footer'>");
            htmlBuilder.AppendLine($"<p>تاریخ تولید: {DateTime.Now:yyyy/MM/dd HH:mm}</p>");
            htmlBuilder.AppendLine("</div>");
            htmlBuilder.AppendLine("</div>");
            htmlBuilder.AppendLine("</body>");
            htmlBuilder.AppendLine("</html>");

            return htmlBuilder.ToString();
        }

        private string FillTemplate<T>(string template, T model)
        {
    
            var modelType = typeof(T);
            var properties = modelType.GetProperties();

            var filledTemplate = template;

            // جایگزینی متغیرهای اصلی
            filledTemplate = filledTemplate.Replace("{{ModelType}}", modelType.Name);
            filledTemplate = filledTemplate.Replace("{{CurrentDate}}", DateTime.Now.ToString("yyyy/MM/dd"));
            filledTemplate = filledTemplate.Replace("{{CurrentTime}}", DateTime.Now.ToString("HH:mm"));

        
            string propertiesContent=string.Empty;


            // تولید محتوای properties به صورت table rows
            propertiesContent = GenerateTableRows(properties, model);
            filledTemplate = filledTemplate.Replace("{{Properties}}", propertiesContent);

            return filledTemplate;
        }
        private string GenerateTableRows(PropertyInfo[] properties, object model)
        {
            var sb = new StringBuilder();

            foreach (var property in properties)
            {
                var value = property.GetValue(model)?.ToString() ?? "-";
              
                sb.AppendLine($@"
                <tr>
                    <td class='property-name'><strong>{property.Name}</strong></td>
                    <td class='property-value'>{value}</td>
                </tr>");
            }

            return sb.ToString();
        }

    

        private async Task<byte[]> GeneratePdfFromHtmlAsync(string htmlContent)
        {
            var tempHtmlPath = Path.GetTempFileName() + ".html";
            var tempPdfPath = Path.GetTempFileName() + ".pdf";

            try
            {
                await File.WriteAllTextAsync(tempHtmlPath, htmlContent, Encoding.UTF8);
                await RunWkhtmltopdf($"--enable-local-file-access \"{tempHtmlPath}\" \"{tempPdfPath}\"");
                return await File.ReadAllBytesAsync(tempPdfPath);
            }
            finally
            {
                if (File.Exists(tempHtmlPath)) File.Delete(tempHtmlPath);
                if (File.Exists(tempPdfPath)) File.Delete(tempPdfPath);
            }
        }

        private async Task RunWkhtmltopdf(string arguments)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = GetWkhtmltopdfExecutablePath(),
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
                throw new InvalidOperationException("Failed to start wkhtmltopdf process");

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"wkhtmltopdf failed: {error}");
            }
        }

        private string GetWkhtmltopdfExecutablePath()
        {
            if (OperatingSystem.IsWindows())
            {
                var paths = new[]
                {
                @"C:\Program Files\wkhtmltopdf\bin\wkhtmltopdf.exe",
                @"C:\Program Files (x86)\wkhtmltopdf\bin\wkhtmltopdf.exe"
            };

                return paths.FirstOrDefault(File.Exists) ?? "wkhtmltopdf";
            }

            return "wkhtmltopdf";
        }
    }

}
