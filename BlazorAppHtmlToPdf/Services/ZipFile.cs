using System.IO.Compression;

namespace BlazorAppHtmlToPdf.Services
{
	public class ZipFile:IZipFile
	{
		private IWebHostEnvironment _env;
		public ZipFile(IWebHostEnvironment env	)
		{
			_env = env;
		}
		public async Task<string> CreateZipFileAsync(string folderName)
		{
			var pathRoot = Path.Combine(_env.WebRootPath, "ExportFile");
			// مسیر پوشه‌ای که در ExportFile ساخته شده
			var sourceFolderPath = Path.Combine(pathRoot, folderName);

			if (!Directory.Exists(sourceFolderPath))
			{
				throw new DirectoryNotFoundException($"پوشه {sourceFolderPath} یافت نشد");
			}

			// مسیر فایل ZIP خروجی
			var zipFilePath = Path.Combine(pathRoot, $"{folderName}.zip");

			// ایجاد فایل ZIP
			using var fileStream = new FileStream(zipFilePath, FileMode.Create);
			using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, true))
			{
				// اضافه کردن تمام فایل‌های پوشه به ZIP
				var files = Directory.GetFiles(sourceFolderPath, "*", SearchOption.AllDirectories);

				foreach (var file in files)
				{
					// نام نسبی فایل در ZIP (بدون مسیر کامل)
					var entryName = Path.GetRelativePath(sourceFolderPath, file);
					var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);

					using var entryStream = entry.Open();
					using var fileStreamToRead = File.OpenRead(file);
					await fileStreamToRead.CopyToAsync(entryStream);
				}
			}

			return zipFilePath; // مسیر فایل ZIP ایجاد شده
		}
	}
}
