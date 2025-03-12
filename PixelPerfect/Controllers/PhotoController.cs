using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixelPerfect.Core.Models;
using PixelPerfect.Services;
using System.Security.Claims;

namespace PixelPerfect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhotoController : ControllerBase
    {
        private readonly IPhotoService _photoService;
        private readonly IPhotographerService _photographerService;
        private readonly IBookingService _bookingService;

        public PhotoController(
            IPhotoService photoService,
            IPhotographerService photographerService,
            IBookingService bookingService)
        {
            _photoService = photoService;
            _photographerService = photographerService;
            _bookingService = bookingService;
        }

        // 获取照片详情
        [HttpGet("{photoId}")]
        public async Task<IActionResult> GetPhotoById(int photoId)
        {
            try
            {
                int? userId = null;
                if (User.Identity.IsAuthenticated)
                {
                    userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                }

                var photo = await _photoService.GetPhotoByIdAsync(photoId);
                if (photo == null)
                    return NotFound(new { message = $"Photo with ID {photoId} not found." });

                // 检查访问权限
                if (userId.HasValue && !await _photoService.CanAccessPhotoAsync(photoId, userId.Value) && !User.IsInRole("Admin"))
                    return Forbid();

                return Ok(photo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving photo information." });
            }
        }

        // 获取预约的所有照片
        [HttpGet("booking/{bookingId}")]
        [Authorize]
        public async Task<IActionResult> GetPhotosByBooking(int bookingId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 验证访问权限
                bool isUserBooking = await _bookingService.IsUserBookingAsync(bookingId, userId);

                var photographer = await _photographerService.GetPhotographerByUserIdAsync(userId);
                bool isPhotographerBooking = photographer != null &&
                                          await _bookingService.IsPhotographerBookingAsync(bookingId, photographer.PhotographerId);

                if (!isUserBooking && !isPhotographerBooking && !User.IsInRole("Admin"))
                    return Forbid();

                var photos = await _photoService.GetPhotosByBookingIdAsync(bookingId);
                return Ok(photos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving booking photos." });
            }
        }

        // 获取用户的所有照片集合
        [HttpGet("user/collections")]
        [Authorize]
        public async Task<IActionResult> GetUserPhotoCollections()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var collections = await _photoService.GetPhotoCollectionsByUserIdAsync(userId);
                return Ok(collections);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving user photo collections." });
            }
        }

        // 获取摄影师的所有照片集合
        [HttpGet("photographer/collections")]
        [Authorize]
        public async Task<IActionResult> GetPhotographerPhotoCollections()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 获取当前用户的摄影师信息
                var photographer = await _photographerService.GetPhotographerByUserIdAsync(userId);
                if (photographer == null)
                    return BadRequest(new { message = "Current user is not a photographer." });

                var collections = await _photoService.GetPhotoCollectionsByPhotographerIdAsync(photographer.PhotographerId);
                return Ok(collections);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving photographer photo collections." });
            }
        }

        // 上传单张照片
        [HttpPost("upload")]
        [Authorize]
        public async Task<IActionResult> UploadPhoto([FromForm] PhotoUploadRequest request, IFormFile file)
        {
            if (file == null)
                return BadRequest(new { message = "No file uploaded." });

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 获取当前用户的摄影师信息
                var photographer = await _photographerService.GetPhotographerByUserIdAsync(userId);
                if (photographer == null)
                    return BadRequest(new { message = "Only photographers can upload photos." });

                var photo = await _photoService.UploadPhotoAsync(photographer.PhotographerId, file, request);
                return CreatedAtAction(nameof(GetPhotoById), new { photoId = photo.PhotoId }, photo);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while uploading the photo." });
            }
        }

        // 批量上传照片
        [HttpPost("batch-upload")]
        [Authorize]
        public async Task<IActionResult> BatchUploadPhotos([FromForm] BatchPhotoUploadRequest request, [FromForm] List<IFormFile> files)
        {
            if (files == null || !files.Any())
                return BadRequest(new { message = "No files uploaded." });

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 获取当前用户的摄影师信息
                var photographer = await _photographerService.GetPhotographerByUserIdAsync(userId);
                if (photographer == null)
                    return BadRequest(new { message = "Only photographers can upload photos." });

                var photos = await _photoService.BatchUploadPhotosAsync(photographer.PhotographerId, files, request);
                return Ok(new { message = $"Successfully uploaded {photos.Count} photos.", photos });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while uploading photos." });
            }
        }

        // 更新照片信息
        [HttpPut("{photoId}")]
        [Authorize]
        public async Task<IActionResult> UpdatePhoto(int photoId, [FromBody] PhotoUpdateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 检查权限 (摄影师或管理员)
                var photographer = await _photographerService.GetPhotographerByUserIdAsync(userId);
                bool isPhotographer = photographer != null &&
                                  await _photoService.IsPhotographerPhotoAsync(photoId, photographer.PhotographerId);

                if (!isPhotographer && !User.IsInRole("Admin"))
                    return Forbid();

                var success = await _photoService.UpdatePhotoAsync(photoId, request);
                if (success)
                    return Ok(new { message = "Photo updated successfully." });
                else
                    return BadRequest(new { message = "Failed to update photo." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the photo." });
            }
        }

        // 客户批准照片
        [HttpPut("{photoId}/approve")]
        [Authorize]
        public async Task<IActionResult> ApprovePhoto(int photoId, [FromBody] PhotoApprovalRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 检查权限 (只有预约客户可以批准照片)
                var photo = await _photoService.GetPhotoByIdAsync(photoId);
                if (photo == null)
                    return NotFound(new { message = $"Photo with ID {photoId} not found." });

                bool isClient = await _bookingService.IsUserBookingAsync(photo.BookingId, userId);

                if (!isClient && !User.IsInRole("Admin"))
                    return Forbid();

                var success = await _photoService.ApprovePhotoAsync(photoId, request.ClientApproved);
                if (success)
                    return Ok(new { message = $"Photo {(request.ClientApproved ? "approved" : "unapproved")} successfully." });
                else
                    return BadRequest(new { message = "Failed to update photo approval status." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating photo approval status." });
            }
        }

        // 删除照片
        [HttpDelete("{photoId}")]
        [Authorize]
        public async Task<IActionResult> DeletePhoto(int photoId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 检查权限 (摄影师或管理员)
                var photographer = await _photographerService.GetPhotographerByUserIdAsync(userId);
                bool isPhotographer = photographer != null &&
                                  await _photoService.IsPhotographerPhotoAsync(photoId, photographer.PhotographerId);

                if (!isPhotographer && !User.IsInRole("Admin"))
                    return Forbid();

                var success = await _photoService.DeletePhotoAsync(photoId);
                if (success)
                    return Ok(new { message = "Photo deleted successfully." });
                else
                    return BadRequest(new { message = "Failed to delete photo." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the photo." });
            }
        }

        // 搜索照片（管理员功能）
        [HttpGet("search")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SearchPhotos([FromQuery] PhotoSearchParams searchParams)
        {
            try
            {
                var photos = await _photoService.SearchPhotosAsync(searchParams);
                return Ok(photos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while searching photos." });
            }
        }

        // 通用单张图片上传接口
        [HttpPost("upload/general")]
        [Authorize]
        public async Task<IActionResult> UploadGeneralPhoto(IFormFile file, [FromForm] string title = null, [FromForm] string description = null)
        {
            if (file == null)
                return BadRequest(new { message = "No file uploaded." });

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 这个接口允许任何登录用户上传图片
                var uploadResult = await _photoService.UploadGeneralPhotoAsync(userId, file, title, description);
                return Ok(new
                {
                    message = "Photo uploaded successfully.",
                    photoUrl = uploadResult.Url,
                    thumbnailUrl = uploadResult.ThumbnailUrl,
                    photoId = uploadResult.PhotoId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while uploading the photo." });
            }
        }

        // 作品集封面图片上传接口
        [HttpPost("upload/portfolio-cover")]
        [Authorize]
        public async Task<IActionResult> UploadPortfolioCover(IFormFile file)
        {
            if (file == null)
                return BadRequest(new { message = "No file uploaded." });

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 可以同时支持摄影师和修图师上传封面
                var uploadResult = await _photoService.UploadPortfolioCoverAsync(userId, file);
                return Ok(new
                {
                    message = "Portfolio cover uploaded successfully.",
                    coverUrl = uploadResult.Url,
                    thumbnailUrl = uploadResult.ThumbnailUrl,
                    photoId = uploadResult.PhotoId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while uploading the portfolio cover." });
            }
        }

        // 作品项图片上传接口 - 可以同时上传前后对比图片（修图师需要）
        [HttpPost("upload/portfolio-item")]
        [Authorize]
        public async Task<IActionResult> UploadPortfolioItemPhoto(
            IFormFile mainFile,
            IFormFile beforeFile = null,
            [FromForm] string title = null,
            [FromForm] string description = null)
        {
            if (mainFile == null)
                return BadRequest(new { message = "No main file uploaded." });

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var uploadResult = await _photoService.UploadPortfolioItemPhotoAsync(
                    userId,
                    mainFile,
                    beforeFile,
                    title,
                    description);

                return Ok(new
                {
                    message = "Portfolio item photo(s) uploaded successfully.",
                    mainImageUrl = uploadResult.MainImageUrl,
                    mainThumbnailUrl = uploadResult.MainThumbnailUrl,
                    beforeImageUrl = uploadResult.BeforeImageUrl,
                    beforeThumbnailUrl = uploadResult.BeforeThumbnailUrl,
                    photoId = uploadResult.PhotoId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while uploading the portfolio item photo(s)." });
            }
        }

        // 临时图片上传接口
        [HttpPost("upload/temp")]
        [Authorize]
        public async Task<IActionResult> UploadTempPhoto(IFormFile file)
        {
            if (file == null)
                return BadRequest(new { message = "No file uploaded." });

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var uploadResult = await _photoService.UploadTempPhotoAsync(userId, file);
                return Ok(new
                {
                    message = "Temporary photo uploaded successfully.",
                    photoUrl = uploadResult.Url,
                    thumbnailUrl = uploadResult.ThumbnailUrl,
                    photoId = uploadResult.PhotoId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while uploading the temporary photo." });
            }
        }


    }
}