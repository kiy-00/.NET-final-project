using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace PixelPerfect.Services
{
    /// <summary>
    /// 文件存储服务接口
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// 保存上传的文件
        /// </summary>
        /// <param name="file">上传的文件</param>
        /// <param name="directory">目标目录</param>
        /// <param name="fileName">可选的文件名，若不提供则自动生成</param>
        /// <returns>文件存储路径</returns>
        Task<string> SaveFileAsync(IFormFile file, string directory, string fileName = null);

        /// <summary>
        /// 获取文件URL
        /// </summary>
        /// <param name="filePath">文件相对路径</param>
        /// <returns>可访问的URL</returns>
        string GetFileUrl(string filePath);

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="filePath">文件相对路径</param>
        /// <returns>删除结果</returns>
        Task<bool> DeleteFileAsync(string filePath);

        /// <summary>
        /// 生成缩略图
        /// </summary>
        /// <param name="originalPath">原始图片路径</param>
        /// <param name="maxWidth">最大宽度</param>
        /// <param name="maxHeight">最大高度</param>
        /// <returns>缩略图路径</returns>
        Task<string> GenerateThumbnailAsync(string originalPath, int maxWidth = 300, int maxHeight = 300);

        Task CleanEmptyDirectoriesAsync(string basePath);
    }
}