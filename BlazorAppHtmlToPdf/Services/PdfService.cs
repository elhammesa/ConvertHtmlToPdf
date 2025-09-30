using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using BlazorAppHtmlToPdf.Components.Model;

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

            var properties = GetObjectProperties(model);
            var filledTemplate = template;

            // جایگزینی متغیرهای اصلی
            filledTemplate = filledTemplate.Replace("{{ModelType}}", "گزارش اطلاعات");
            filledTemplate = filledTemplate.Replace("{{CurrentDate}}", DateTime.Now.ToString("yyyy/MM/dd"));
            filledTemplate = filledTemplate.Replace("{{CurrentTime}}", DateTime.Now.ToString("HH:mm"));

            // تولید header row (property names)
            var headerRow = GenerateHeaderRow(properties);
            filledTemplate = filledTemplate.Replace("{{HeaderRow}}", headerRow);

            // تولید data rows (property values)
            var dataRows = GenerateDataRows(properties, model);
            filledTemplate = filledTemplate.Replace("{{DataRows}}", dataRows);

            return filledTemplate;
        }
        private string GenerateHeaderRow(List<string> properties)
        {
            var sb = new StringBuilder();

            foreach (var property in properties)
            {
            
                sb.AppendLine($"<th>{property}</th>");
            }

            return sb.ToString();
        }
        private List<string> GetObjectProperties<T>(T model)
        {
            if (model == null)
                return new List<string>();

            var type = model.GetType();
            var properties = new List<string>();

            // بررسی آیا مدل یک لیست/آرایه است
            if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            {
                // اگر لیست خالی است، propertyهای پیش‌فرض برگردان
                var enumerable = model as IEnumerable;
                if (enumerable != null)
                {
                    var firstItem = enumerable.Cast<object>().FirstOrDefault();
                    if (firstItem != null)
                    {
                        // propertyهای اولین آیتم در لیست را بگیر
                        var itemType = firstItem.GetType();
                        properties = itemType.GetProperties()
                                           .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                                           .Select(p => p.Name)
                                           .ToList();
                    }
                    else
                    {
                        // اگر لیست خالی است، propertyهای پیش‌فرض Message را برگردان
                        properties = typeof(Message).GetProperties()
                                                  .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                                                  .Select(p => p.Name)
                                                  .ToList();
                    }
                }
            }
            // برای anonymous types و dynamic objects
            else if (type.Name.Contains("AnonymousType") || type.Name.StartsWith("<>f__AnonymousType"))
            {
                properties = type.GetProperties()
                               .Where(p => p.CanRead)
                               .Select(p => p.Name)
                               .ToList();
            }
            // برای کلاس‌های معمولی
            else if (type.IsClass && type != typeof(string))
            {
                properties = type.GetProperties()
                               .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                               .Select(p => p.Name)
                               .ToList();
            }
            // برای انواع ساده
            else
            {
                properties.Add("Value");
            }

            return properties;
        }
        private string GenerateDataRows(List<string> properties, object model)
        {
            var sb = new StringBuilder();

            // اگر model یک لیست است
            if (model is System.Collections.IEnumerable enumerable && model.GetType() != typeof(string))
            {
                foreach (var item in enumerable)
                {
                    sb.AppendLine("<tr>");
                    foreach (var property in properties)
                    {
                        var value = GetPropertyValue(item, property)?.ToString() ?? "-";
                        sb.AppendLine($"<td>{value}</td>");
                    }
                    sb.AppendLine("</tr>");
                }
            }
            else
            {
                // برای یک object تک
                sb.AppendLine("<tr>");
                foreach (var property in properties)
                {
                    var value = GetPropertyValue(model, property)?.ToString() ?? "-";
                    sb.AppendLine($"<td>{value}</td>");
                }
                sb.AppendLine("</tr>");
            }

            return sb.ToString();
        }
    
        private object GetPropertyValue<T>(T model, string propertyName)
        {
            if (model == null)
                return null;

            var type = model.GetType();

            // اگر propertyName برابر "Value" باشد و نوع ساده باشد
            if (propertyName == "Value" && (type.IsValueType || type == typeof(string)))
            {
                return model;
            }

            try
            {
                var property = type.GetProperty(propertyName);
                if (property != null && property.CanRead)
                {
                    return property.GetValue(model);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting property value for {propertyName}: {ex.Message}");
            }

            return null;
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
