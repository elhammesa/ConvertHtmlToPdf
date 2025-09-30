namespace BlazorAppHtmlToPdf.Services
{
    public interface IExcelService
    {
        Task<byte[]> GenerateExcelFromModelAsync<T>(List<T> data);
        Task<byte[]> GenerateExcelFromHtmlTableAsync(string htmlContent);
        Task<string> GenerateExcelBase64Async<T>(List<T> data);
    }
}
