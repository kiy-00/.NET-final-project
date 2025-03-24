using Microsoft.AspNetCore.Http;
using PixelPerfect.ImageProcessing.CLI;
using SixLabors.ImageSharp.Processing.Processors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PixelPerfect.Services
{
    /// <summary>
    /// 高级图像处理服务接口
    /// </summary>
    public interface IAdvancedImageService
    {
        // C++/CLI方法
        byte[] ApplyFilter(byte[] imageData, string filterType);
        byte[] AddWatermark(byte[] imageData, string watermarkText, float opacity, int position);
        bool AnalyzeImageQuality(byte[] imageData, out double qualityScore);
        Dictionary<string, string> ExtractMetadata(byte[] imageData);
        byte[] ResizeImage(byte[] imageData, int width, int height);
        byte[] CropImage(byte[] imageData, int x, int y, int width, int height);
        byte[] CreateThumbnail(byte[] imageData, int width, int height);

        // 原生DLL方法
        byte[] CompressImage(byte[] imageData, int quality);
        int DetectFaces(byte[] imageData);
        int CompareImageSimilarity(byte[] imageData1, byte[] imageData2);
        byte[] SharpenImage(byte[] imageData);

        // 便捷方法(基于文件路径)
        Task<bool> ApplyFilterAndSaveAsync(string imagePath, string filterType, string outputPath);
        Task<bool> AddWatermarkAndSaveAsync(string imagePath, string watermarkText, float opacity, int position, string outputPath);
        Task<byte[]> ProcessImageAsync(IFormFile file, string processingType, Dictionary<string, string> parameters);
    }

    /// <summary>
    /// 高级图像处理服务实现
    /// </summary>
    public class AdvancedImageService : IAdvancedImageService
    {
        private readonly ImageProcessor _imageProcessor;
        private readonly IFileStorageService _fileStorage;

        public AdvancedImageService(IFileStorageService fileStorage)
        {
            _imageProcessor = new ImageProcessor();
            _fileStorage = fileStorage;
        }

        #region C++/CLI方法

        public byte[] ApplyFilter(byte[] imageData, string filterType)
        {
            return _imageProcessor.ApplyFilter(imageData, filterType);
        }

        public byte[] AddWatermark(byte[] imageData, string watermarkText, float opacity, int position)
        {
            return _imageProcessor.AddWatermark(imageData, watermarkText, opacity, position);
        }

        public bool AnalyzeImageQuality(byte[] imageData, out double qualityScore)
        {
            return _imageProcessor.AnalyzeImageQuality(imageData, out qualityScore);
        }

        public Dictionary<string, string> ExtractMetadata(byte[] imageData)
        {
            var metadata = _imageProcessor.ExtractMetadata(imageData);
            return new Dictionary<string, string>(metadata);
        }

        public byte[] ResizeImage(byte[] imageData, int width, int height)
        {
            return _imageProcessor.ResizeImage(imageData, width, height);
        }

        public byte[] CropImage(byte[] imageData, int x, int y, int width, int height)
        {
            return _imageProcessor.CropImage(imageData, x, y, width, height);
        }

        public byte[] CreateThumbnail(byte[] imageData, int width, int height)
        {
            return _imageProcessor.CreateThumbnail(imageData, width, height);
        }

        #endregion

        #region 原生DLL方法

        public byte[] CompressImage(byte[] imageData, int quality)
        {
            return NativeImageProcessorWrapper.CompressImage(imageData, quality);
        }

        public int DetectFaces(byte[] imageData)
        {
            return NativeImageProcessorWrapper.DetectFaces(imageData);
        }

        public int CompareImageSimilarity(byte[] imageData1, byte[] imageData2)
        {
            return NativeImageProcessorWrapper.CompareImages(imageData1, imageData2);
        }

        public byte[] SharpenImage(byte[] imageData)
        {
            return NativeImageProcessorWrapper.SharpenImage(imageData);
        }

        #endregion

        #region 便捷方法

        public async Task<bool> ApplyFilterAndSaveAsync(string imagePath, string filterType, string outputPath)
        {
            try
            {
                byte[] imageData = await File.ReadAllBytesAsync(imagePath);
                byte[] filteredData = ApplyFilter(imageData, filterType);
                await File.WriteAllBytesAsync(outputPath, filteredData);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用滤镜并保存失败: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AddWatermarkAndSaveAsync(string imagePath, string watermarkText, float opacity, int position, string outputPath)
        {
            try
            {
                byte[] imageData = await File.ReadAllBytesAsync(imagePath);
                byte[] watermarkedData = AddWatermark(imageData, watermarkText, opacity, position);
                await File.WriteAllBytesAsync(outputPath, watermarkedData);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"添加水印并保存失败: {ex.Message}");
                return false;
            }
        }

        public async Task<byte[]> ProcessImageAsync(IFormFile file, string processingType, Dictionary<string, string> parameters)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("文件不能为空");

            // 读取上传的文件数据
            byte[] imageData;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                imageData = ms.ToArray();
            }

            // 根据处理类型执行不同的图像处理操作
            switch (processingType.ToLower())
            {
                case "filter":
                    string filterType = parameters.ContainsKey("type") ? parameters["type"] : "grayscale";
                    return ApplyFilter(imageData, filterType);

                case "watermark":
                    string text = parameters.ContainsKey("text") ? parameters["text"] : "PixelPerfect";
                    float opacity = parameters.ContainsKey("opacity") ? float.Parse(parameters["opacity"]) : 0.7f;
                    int position = parameters.ContainsKey("position") ? int.Parse(parameters["position"]) : 1;
                    return AddWatermark(imageData, text, opacity, position);

                case "resize":
                    int width = parameters.ContainsKey("width") ? int.Parse(parameters["width"]) : 800;
                    int height = parameters.ContainsKey("height") ? int.Parse(parameters["height"]) : 600;
                    return ResizeImage(imageData, width, height);

                case "crop":
                    int x = parameters.ContainsKey("x") ? int.Parse(parameters["x"]) : 0;
                    int y = parameters.ContainsKey("y") ? int.Parse(parameters["y"]) : 0;
                    int cropWidth = parameters.ContainsKey("width") ? int.Parse(parameters["width"]) : 400;
                    int cropHeight = parameters.ContainsKey("height") ? int.Parse(parameters["height"]) : 300;
                    return CropImage(imageData, x, y, cropWidth, cropHeight);

                case "thumbnail":
                    int thumbWidth = parameters.ContainsKey("width") ? int.Parse(parameters["width"]) : 150;
                    int thumbHeight = parameters.ContainsKey("height") ? int.Parse(parameters["height"]) : 150;
                    return CreateThumbnail(imageData, thumbWidth, thumbHeight);

                case "compress":
                    int quality = parameters.ContainsKey("quality") ? int.Parse(parameters["quality"]) : 80;
                    return CompressImage(imageData, quality);

                case "sharpen":
                    return SharpenImage(imageData);

                default:
                    throw new ArgumentException($"未知的处理类型: {processingType}");
            }
        }

        #endregion
    }
}