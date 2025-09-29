namespace BlazorAppHtmlToPdf.Components.Services
{
    public interface IPdfService
    {
        Task<byte[]> GeneratePdfFromModelAsync<T>(T model);
        Task<string> GeneratePdfBase64Async<T>(T model);
    }
}
