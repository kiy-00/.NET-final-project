using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixelPerfect.Core.Models;
using PixelPerfect.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PixelPerfect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageProcessingController : ControllerBase
    {
        private readonly IAdvancedImageService _advancedImageService;
        private readonly IPhotoService _photoService;
        private readonly IFileStorageService _fileStorage;

        public ImageProcessingController(
            IAdvancedImageService advancedImageService,
            IPhotoService photoService,
            IFileStorageService fileStorage)
        {
            _advancedImageService = advancedImageService;
            _photoService = photoService;
            _fileStorage = fileStorage;
        }

        /// <summary>
        /// 应用滤镜到图像
        /// </summary>
        /// <param name="file">要处理的图像文件</param>
        /// <param name="filterType">
        /// 滤镜类型:
        /// - grayscale: 灰度滤镜
        /// - sepia: 棕褐色滤镜
        /// - negative: 反色滤镜
        /// - highcontrast: 高对比度
        /// - blur: 模糊效果
        /// - redboost: 红色增强
        /// - greenboost: 绿色增强
        /// - blueboost: 蓝色增强
        /// - cold: 冷色调
        /// - warm: 暖色调
        /// </summary>
        [HttpPost("filter")]
        [Authorize]
        public async Task<IActionResult> ApplyFilter(IFormFile file, [FromForm] string filterType)
        {
            try
            {
                if (file == null)
                    return BadRequest(new { message = "没有上传文件" });

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var parameters = new Dictionary<string, string>
                {
                    ["type"] = filterType
                };

                byte[] processedImage = await _advancedImageService.ProcessImageAsync(file, "filter", parameters);

                // 保存处理后的图片
                using (var ms = new MemoryStream(processedImage))
                {
                    // 创建新的FormFile
                    var fileName = Path.GetFileNameWithoutExtension(file.FileName) + "_" + filterType + Path.GetExtension(file.FileName);
                    var processedFile = new FormFile(ms, 0, processedImage.Length, file.Name, fileName)
                    {
                        Headers = file.Headers,
                        ContentType = file.ContentType
                    };

                    // 上传处理后的图片
                    var uploadResult = await _photoService.UploadGeneralPhotoAsync(
                        userId,
                        processedFile,
                        $"使用{filterType}滤镜处理的图片",
                        $"原始图片：{file.FileName}，处理时间：{DateTime.Now}"
                    );

                    return Ok(new
                    {
                        message = $"成功应用{filterType}滤镜",
                        photoId = uploadResult.PhotoId,
                        url = uploadResult.Url,
                        thumbnailUrl = uploadResult.ThumbnailUrl
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"应用滤镜失败: {ex.Message}" });
            }
        }

        // 添加水印
        [HttpPost("watermark")]
        [Authorize]
        public async Task<IActionResult> AddWatermark(
            IFormFile file,
            [FromForm] string text,
            [FromForm] float opacity = 0.7f,
            [FromForm] int position = 1)
        {
            try
            {
                if (file == null)
                    return BadRequest(new { message = "没有上传文件" });

                if (string.IsNullOrEmpty(text))
                    return BadRequest(new { message = "水印文本不能为空" });

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var parameters = new Dictionary<string, string>
                {
                    ["text"] = text,
                    ["opacity"] = opacity.ToString(),
                    ["position"] = position.ToString()
                };

                byte[] processedImage = await _advancedImageService.ProcessImageAsync(file, "watermark", parameters);

                // 保存处理后的图片
                using (var ms = new MemoryStream(processedImage))
                {
                    // 创建新的FormFile
                    var fileName = Path.GetFileNameWithoutExtension(file.FileName) + "_watermark" + Path.GetExtension(file.FileName);
                    var processedFile = new FormFile(ms, 0, processedImage.Length, file.Name, fileName)
                    {
                        Headers = file.Headers,
                        ContentType = file.ContentType
                    };

                    // 上传处理后的图片
                    var uploadResult = await _photoService.UploadGeneralPhotoAsync(
                        userId,
                        processedFile,
                        $"添加水印的图片",
                        $"原始图片：{file.FileName}，水印文本：{text}，处理时间：{DateTime.Now}"
                    );

                    return Ok(new
                    {
                        message = "成功添加水印",
                        photoId = uploadResult.PhotoId,
                        url = uploadResult.Url,
                        thumbnailUrl = uploadResult.ThumbnailUrl
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"添加水印失败: {ex.Message}" });
            }
        }

        // 分析图像质量
        [HttpPost("analyze-quality")]
        [Authorize]
        public async Task<IActionResult> AnalyzeImageQuality(IFormFile file)
        {
            try
            {
                if (file == null)
                    return BadRequest(new { message = "没有上传文件" });

                // 读取上传的文件数据
                byte[] imageData;
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    imageData = ms.ToArray();
                }

                double qualityScore;
                bool success = _advancedImageService.AnalyzeImageQuality(imageData, out qualityScore);

                if (!success)
                    return BadRequest(new { message = "分析图像质量失败" });

                // 分析图像元数据
                var metadata = _advancedImageService.ExtractMetadata(imageData);

                return Ok(new
                {
                    fileName = file.FileName,
                    fileSize = file.Length,
                    qualityScore = Math.Round(qualityScore, 2),
                    quality = QualityLevel(qualityScore),
                    metadata
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"分析图像质量失败: {ex.Message}" });
            }
        }

        // 将质量分数转换为描述性文本
        private string QualityLevel(double score)
        {
            if (score >= 90) return "极佳";
            if (score >= 80) return "优秀";
            if (score >= 70) return "良好";
            if (score >= 60) return "一般";
            if (score >= 50) return "较差";
            return "差";
        }

        // 调整图像大小
        [HttpPost("resize")]
        [Authorize]
        public async Task<IActionResult> ResizeImage(
            IFormFile file,
            [FromForm] int width,
            [FromForm] int height)
        {
            try
            {
                if (file == null)
                    return BadRequest(new { message = "没有上传文件" });

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var parameters = new Dictionary<string, string>
                {
                    ["width"] = width.ToString(),
                    ["height"] = height.ToString()
                };

                byte[] processedImage = await _advancedImageService.ProcessImageAsync(file, "resize", parameters);

                // 保存处理后的图片
                using (var ms = new MemoryStream(processedImage))
                {
                    // 创建新的FormFile
                    var fileName = Path.GetFileNameWithoutExtension(file.FileName) + $"_{width}x{height}" + Path.GetExtension(file.FileName);
                    var processedFile = new FormFile(ms, 0, processedImage.Length, file.Name, fileName)
                    {
                        Headers = file.Headers,
                        ContentType = file.ContentType
                    };

                    // 上传处理后的图片
                    var uploadResult = await _photoService.UploadGeneralPhotoAsync(
                        userId,
                        processedFile,
                        $"调整大小后的图片({width}x{height})",
                        $"原始图片：{file.FileName}，处理时间：{DateTime.Now}"
                    );

                    return Ok(new
                    {
                        message = $"成功调整图像大小为{width}x{height}",
                        photoId = uploadResult.PhotoId,
                        url = uploadResult.Url,
                        thumbnailUrl = uploadResult.ThumbnailUrl
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"调整图像大小失败: {ex.Message}" });
            }
        }

        // 裁剪图像
        [HttpPost("crop")]
        [Authorize]
        public async Task<IActionResult> CropImage(
            IFormFile file,
            [FromForm] int x,
            [FromForm] int y,
            [FromForm] int width,
            [FromForm] int height)
        {
            try
            {
                if (file == null)
                    return BadRequest(new { message = "没有上传文件" });

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var parameters = new Dictionary<string, string>
                {
                    ["x"] = x.ToString(),
                    ["y"] = y.ToString(),
                    ["width"] = width.ToString(),
                    ["height"] = height.ToString()
                };

                byte[] processedImage = await _advancedImageService.ProcessImageAsync(file, "crop", parameters);

                // 保存处理后的图片
                using (var ms = new MemoryStream(processedImage))
                {
                    // 创建新的FormFile
                    var fileName = Path.GetFileNameWithoutExtension(file.FileName) + $"_crop" + Path.GetExtension(file.FileName);
                    var processedFile = new FormFile(ms, 0, processedImage.Length, file.Name, fileName)
                    {
                        Headers = file.Headers,
                        ContentType = file.ContentType
                    };

                    // 上传处理后的图片
                    var uploadResult = await _photoService.UploadGeneralPhotoAsync(
                        userId,
                        processedFile,
                        $"裁剪后的图片",
                        $"原始图片：{file.FileName}，裁剪区域：({x},{y},{width},{height})，处理时间：{DateTime.Now}"
                    );

                    return Ok(new
                    {
                        message = "成功裁剪图像",
                        photoId = uploadResult.PhotoId,
                        url = uploadResult.Url,
                        thumbnailUrl = uploadResult.ThumbnailUrl
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"裁剪图像失败: {ex.Message}" });
            }
        }

        // 压缩图像
        [HttpPost("compress")]
        [Authorize]
        public async Task<IActionResult> CompressImage(
            IFormFile file,
            [FromForm] int quality = 80)
        {
            try
            {
                if (file == null)
                    return BadRequest(new { message = "没有上传文件" });

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var parameters = new Dictionary<string, string>
                {
                    ["quality"] = quality.ToString()
                };

                byte[] processedImage;
                try
                {
                    processedImage = await _advancedImageService.ProcessImageAsync(file, "compress", parameters);
                }
                catch
                {
                    // 备选方案
                    using (var imageStream = new MemoryStream())
                    {
                        await file.CopyToAsync(imageStream);
                        var imageData = imageStream.ToArray();

                        using (var outputStream = new MemoryStream())
                        {
                            var image = SixLabors.ImageSharp.Image.Load(imageData);
                            var encoder = new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
                            {
                                Quality = quality
                            };
                            image.Save(outputStream, encoder);
                            processedImage = outputStream.ToArray();
                        }
                    }
                }

                // 保存处理后的图片
                using (var ms = new MemoryStream(processedImage))
                {
                    // 创建新的FormFile
                    var fileName = Path.GetFileNameWithoutExtension(file.FileName) + $"_compressed_{quality}" + Path.GetExtension(file.FileName);
                    var processedFile = new FormFile(ms, 0, processedImage.Length, file.Name, fileName)
                    {
                        Headers = file.Headers,
                        ContentType = file.ContentType
                    };

                    // 上传处理后的图片
                    var uploadResult = await _photoService.UploadGeneralPhotoAsync(
                        userId,
                        processedFile,
                        $"压缩质量为{quality}的图片",
                        $"原始图片：{file.FileName}，压缩质量：{quality}，处理时间：{DateTime.Now}"
                    );

                    return Ok(new
                    {
                        message = $"成功压缩图像，质量：{quality}",
                        photoId = uploadResult.PhotoId,
                        url = uploadResult.Url,
                        thumbnailUrl = uploadResult.ThumbnailUrl,
                        originalSize = file.Length,
                        compressedSize = processedImage.Length
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"压缩图像失败: {ex.Message}" });
            }
        }

        // 锐化图像
        [HttpPost("sharpen")]
        [Authorize]
        public async Task<IActionResult> SharpenImage(IFormFile file)
        {
            try
            {
                if (file == null)
                    return BadRequest(new { message = "没有上传文件" });

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                byte[] processedImage = await _advancedImageService.ProcessImageAsync(file, "sharpen", new Dictionary<string, string>());

                // 保存处理后的图片
                using (var ms = new MemoryStream(processedImage))
                {
                    // 创建新的FormFile
                    var fileName = Path.GetFileNameWithoutExtension(file.FileName) + "_sharpen" + Path.GetExtension(file.FileName);
                    var processedFile = new FormFile(ms, 0, processedImage.Length, file.Name, fileName)
                    {
                        Headers = file.Headers,
                        ContentType = file.ContentType
                    };

                    // 上传处理后的图片
                    var uploadResult = await _photoService.UploadGeneralPhotoAsync(
                        userId,
                        processedFile,
                        "锐化后的图片",
                        $"原始图片：{file.FileName}，处理时间：{DateTime.Now}"
                    );

                    return Ok(new
                    {
                        message = "成功锐化图像",
                        photoId = uploadResult.PhotoId,
                        url = uploadResult.Url,
                        thumbnailUrl = uploadResult.ThumbnailUrl
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"锐化图像失败: {ex.Message}" });
            }
        }

        // 检测人脸
        [HttpPost("detect-faces")]
        [Authorize]
        public async Task<IActionResult> DetectFaces(IFormFile file)
        {
            try
            {
                if (file == null)
                    return BadRequest(new { message = "没有上传文件" });

                // 读取上传的文件数据
                byte[] imageData;
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    imageData = ms.ToArray();
                }

                int faceCount = _advancedImageService.DetectFaces(imageData);

                return Ok(new
                {
                    fileName = file.FileName,
                    faceCount = faceCount,
                    message = faceCount > 0
                        ? $"检测到{faceCount}张人脸"
                        : "未检测到人脸"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"人脸检测失败: {ex.Message}" });
            }
        }

        // 比较两张图片的相似度
        [HttpPost("compare-images")]
        [Authorize]
        public async Task<IActionResult> CompareImages(IFormFile file1, IFormFile file2)
        {
            try
            {
                if (file1 == null || file2 == null)
                    return BadRequest(new { message = "需要上传两个文件进行比较" });

                // 读取上传的文件数据
                byte[] imageData1, imageData2;
                using (var ms = new MemoryStream())
                {
                    await file1.CopyToAsync(ms);
                    imageData1 = ms.ToArray();
                }

                using (var ms = new MemoryStream())
                {
                    await file2.CopyToAsync(ms);
                    imageData2 = ms.ToArray();
                }

                int similarity = _advancedImageService.CompareImageSimilarity(imageData1, imageData2);

                return Ok(new
                {
                    fileName1 = file1.FileName,
                    fileName2 = file2.FileName,
                    similarityPercentage = similarity,
                    similarityLevel = GetSimilarityLevel(similarity),
                    message = $"图像相似度: {similarity}%"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"比较图像失败: {ex.Message}" });
            }
        }

        // 获取相似度等级描述
        private string GetSimilarityLevel(int similarity)
        {
            if (similarity >= 90) return "极高";
            if (similarity >= 70) return "高";
            if (similarity >= 50) return "中等";
            if (similarity >= 30) return "低";
            return "极低";
        }
    }
}