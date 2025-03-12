using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PixelPerfect.Services.Impl
{
    /// <summary>
    /// 本地文件系统存储实现
    /// </summary>
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly string _baseStoragePath;

        public LocalFileStorageService(IWebHostEnvironment env, IConfiguration config)
        {
            _env = env;
            _config = config;
            _baseStoragePath = Path.Combine(_env.WebRootPath, "uploads");

            // 确保基础上传目录存在
            if (!Directory.Exists(_baseStoragePath))
                Directory.CreateDirectory(_baseStoragePath);
        }

        public async Task<string> SaveFileAsync(IFormFile file, string directory, string fileName = null)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("无效的文件");

            // 安全检查 - 仅允许图片文件
            string fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

            if (!Array.Exists(allowedExtensions, ext => ext.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("只允许上传图片文件");

            // 确保目标目录存在
            string targetDirectory = Path.Combine(_baseStoragePath, directory);
            if (!Directory.Exists(targetDirectory))
                Directory.CreateDirectory(targetDirectory);

            // 生成文件名
            fileName = fileName ?? $"{Guid.NewGuid()}{fileExtension}";
            string fullPath = Path.Combine(targetDirectory, fileName);
            string relativePath = Path.Combine(directory, fileName).Replace("\\", "/");

            // 保存文件
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return relativePath;
        }

        public string GetFileUrl(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            // 确保路径格式正确
            filePath = filePath.Replace("\\", "/");

            // 如果是完整URL，直接返回
            if (filePath.StartsWith("http://") || filePath.StartsWith("https://"))
                return filePath;

            // 如果不是以/开头，添加/
            if (!filePath.StartsWith("/"))
                filePath = "/" + filePath;

            return filePath;
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            try
            {
                string fullPath = Path.Combine(_env.WebRootPath, filePath.TrimStart('/'));

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);

                    // 同时检查并删除缩略图
                    string directory = Path.GetDirectoryName(fullPath);
                    string thumbsDir = Path.Combine(directory, "thumbs");
                    string filename = Path.GetFileName(fullPath);
                    string thumbPath = Path.Combine(thumbsDir, $"thumb_{filename}");

                    if (File.Exists(thumbPath))
                        File.Delete(thumbPath);

                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GenerateThumbnailAsync(string originalPath, int maxWidth = 300, int maxHeight = 300)
        {
            if (string.IsNullOrEmpty(originalPath))
                return null;

            try
            {
                string fullPath = Path.Combine(_env.WebRootPath, originalPath.TrimStart('/'));
                string directory = Path.GetDirectoryName(fullPath);
                string filename = Path.GetFileName(fullPath);

                // 创建缩略图目录
                string thumbsDir = Path.Combine(directory, "thumbs");
                if (!Directory.Exists(thumbsDir))
                    Directory.CreateDirectory(thumbsDir);

                string thumbFilename = $"thumb_{filename}";
                string thumbPath = Path.Combine(thumbsDir, thumbFilename);

                // 生成相对路径
                string originalDir = Path.GetDirectoryName(originalPath.Replace("\\", "/"));
                string thumbRelativePath = $"{originalDir}/thumbs/{thumbFilename}".Replace("\\", "/");

                // 生成缩略图
                using (var image = await Image.LoadAsync(fullPath))
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(maxWidth, maxHeight)
                    }));

                    await image.SaveAsync(thumbPath);
                }

                return thumbRelativePath;
            }
            catch (Exception ex)
            {
                // 如果生成失败，记录错误并返回原图路径
                Console.WriteLine($"生成缩略图失败: {ex.Message}");
                return originalPath;
            }
        }
    }
}