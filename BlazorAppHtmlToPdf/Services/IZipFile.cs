namespace BlazorAppHtmlToPdf.Services
{
	public interface IZipFile
	{
		public Task<string> CreateZipFileAsync(string folderName);
	}
}
