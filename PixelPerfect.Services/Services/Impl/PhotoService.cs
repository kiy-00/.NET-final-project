using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using PixelPerfect.Core.Entities;
using PixelPerfect.Core.Models;
using PixelPerfect.DataAccess.Repos;
using System.Text.Json;

namespace PixelPerfect.Services.Impl
{
    public class PhotoService : IPhotoService
    {
        private PhotoBookingDbContext _context;
        private readonly PhotoRepo _photoRepo;
        private readonly BookingRepo _bookingRepo;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly string _uploadDirectory;

        public PhotoService(
           PhotoBookingDbContext context,
            PhotoRepo photoRepo,
            BookingRepo bookingRepo,
            IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _photoRepo = photoRepo;
            _bookingRepo = bookingRepo;
            _hostEnvironment = hostEnvironment;
            _uploadDirectory = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "photos");

            // 确保目录存在
            if (!Directory.Exists(_uploadDirectory))
                Directory.CreateDirectory(_uploadDirectory);
        }

        public async Task<PhotoDto> GetPhotoByIdAsync(int photoId)
        {
            var photo = await _photoRepo.GetByIdAsync(photoId);
            if (photo == null)
                return null;

            return MapToDto(photo);
        }

        public async Task<List<PhotoDto>> GetPhotosByBookingIdAsync(int bookingId)
        {
            var photos = await _photoRepo.GetByBookingIdAsync(bookingId);
            return photos.Select(MapToDto).ToList();
        }

        public async Task<List<PhotoCollectionDto>> GetPhotoCollectionsByUserIdAsync(int userId)
        {
            var bookings = await _photoRepo.GetBookingsWithPhotosAsync(userId);
            return bookings.Select(b => new PhotoCollectionDto
            {
                BookingId = b.BookingId,
                BookingReference = $"Booking #{b.BookingId}",
                PhotographerName = $"{b.Photographer?.User?.FirstName} {b.Photographer?.User?.LastName}",
                ClientName = $"{b.User?.FirstName} {b.User?.LastName}",
                BookingDate = b.BookingDate,
                Photos = b.Photos.Select(MapToDto).ToList()
            }).ToList();
        }

        public async Task<List<PhotoCollectionDto>> GetPhotoCollectionsByPhotographerIdAsync(int photographerId)
        {
            var bookings = await _photoRepo.GetPhotographerBookingsWithPhotosAsync(photographerId);
            return bookings.Select(b => new PhotoCollectionDto
            {
                BookingId = b.BookingId,
                BookingReference = $"Booking #{b.BookingId}",
                PhotographerName = $"{b.Photographer?.User?.FirstName} {b.Photographer?.User?.LastName}",
                ClientName = $"{b.User?.FirstName} {b.User?.LastName}",
                BookingDate = b.BookingDate,
                Photos = b.Photos.Select(MapToDto).ToList()
            }).ToList();
        }

        public async Task<List<PhotoDto>> SearchPhotosAsync(PhotoSearchParams searchParams)
        {
            var photos = await _photoRepo.SearchAsync(
                searchParams.BookingId,
                searchParams.UserId,
                searchParams.PhotographerId,
                searchParams.IsPublic,
                searchParams.ClientApproved,
                searchParams.StartDate,
                searchParams.EndDate
            );

            return photos.Select(MapToDto).ToList();
        }

        public async Task<PhotoDto> UploadPhotoAsync(int photographerId, IFormFile file, PhotoUploadRequest request)
        {
            // 验证预约存在
            var booking = await _bookingRepo.GetByIdAsync(request.BookingId);
            if (booking == null)
                throw new KeyNotFoundException($"Booking with ID {request.BookingId} not found.");

            // 验证摄影师权限
            if (booking.PhotographerId != photographerId)
                throw new UnauthorizedAccessException("You are not the photographer for this booking.");

            // 验证文件类型
            string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                throw new ArgumentException("Invalid file type. Only jpg, jpeg and png files are allowed.");

            // 创建唯一文件名
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(_uploadDirectory, uniqueFileName);

            // 保存文件
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // 提取元数据
            var metadata = new
            {
                OriginalName = file.FileName,
                Size = file.Length,
                ContentType = file.ContentType,
                UploadedAt = DateTime.UtcNow
            };

            // 创建照片记录
            var newPhoto = new Photo
            {
                BookingId = request.BookingId,
                ImagePath = $"/uploads/photos/{uniqueFileName}",
                Title = request.Title,
                Description = request.Description,
                Metadata = JsonSerializer.Serialize(metadata),
                UploadedAt = DateTime.UtcNow,
                IsPublic = request.IsPublic,
                ClientApproved = false
            };

            var createdPhoto = await _photoRepo.CreateAsync(newPhoto);
            return MapToDto(createdPhoto);
        }

        public async Task<List<PhotoDto>> BatchUploadPhotosAsync(int photographerId, List<IFormFile> files, BatchPhotoUploadRequest request)
        {
            // 验证预约存在
            var booking = await _bookingRepo.GetByIdAsync(request.BookingId);
            if (booking == null)
                throw new KeyNotFoundException($"Booking with ID {request.BookingId} not found.");

            // 验证摄影师权限
            if (booking.PhotographerId != photographerId)
                throw new UnauthorizedAccessException("You are not the photographer for this booking.");

            var uploadedPhotos = new List<PhotoDto>();

            foreach (var file in files)
            {
                try
                {
                    // 验证文件类型
                    string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extension))
                        continue; // 跳过无效文件类型

                    // 创建唯一文件名
                    var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(_uploadDirectory, uniqueFileName);

                    // 保存文件
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    // 提取元数据
                    var metadata = new
                    {
                        OriginalName = file.FileName,
                        Size = file.Length,
                        ContentType = file.ContentType,
                        UploadedAt = DateTime.UtcNow
                    };

                    // 创建照片记录
                    var newPhoto = new Photo
                    {
                        BookingId = request.BookingId,
                        ImagePath = $"/uploads/photos/{uniqueFileName}",
                        Title = Path.GetFileNameWithoutExtension(file.FileName),
                        Description = $"Uploaded on {DateTime.UtcNow:yyyy-MM-dd}",
                        Metadata = JsonSerializer.Serialize(metadata),
                        UploadedAt = DateTime.UtcNow,
                        IsPublic = request.IsPublic,
                        ClientApproved = false
                    };

                    var createdPhoto = await _photoRepo.CreateAsync(newPhoto);
                    uploadedPhotos.Add(MapToDto(createdPhoto));
                }
                catch
                {
                    // 跳过处理失败的文件
                    continue;
                }
            }

            return uploadedPhotos;
        }

        public async Task<bool> UpdatePhotoAsync(int photoId, PhotoUpdateRequest request)
        {
            var photo = await _photoRepo.GetByIdAsync(photoId);
            if (photo == null)
                throw new KeyNotFoundException($"Photo with ID {photoId} not found.");

            // 更新字段
            if (request.Title != null)
                photo.Title = request.Title;

            if (request.Description != null)
                photo.Description = request.Description;

            if (request.IsPublic.HasValue)
                photo.IsPublic = request.IsPublic.Value;

            return await _photoRepo.UpdateAsync(photo);
        }

        public async Task<bool> ApprovePhotoAsync(int photoId, bool clientApproved)
        {
            var photo = await _photoRepo.GetByIdAsync(photoId);
            if (photo == null)
                throw new KeyNotFoundException($"Photo with ID {photoId} not found.");

            photo.ClientApproved = clientApproved;
            return await _photoRepo.UpdateAsync(photo);
        }

        public async Task<bool> DeletePhotoAsync(int photoId)
        {
            var photo = await _photoRepo.GetByIdAsync(photoId);
            if (photo == null)
                return false;

            // 删除物理文件
            try
            {
                var filePath = Path.Combine(_hostEnvironment.WebRootPath, photo.ImagePath.TrimStart('/'));
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch
            {
                // 即使文件删除失败，也继续删除数据库记录
            }

            return await _photoRepo.DeleteAsync(photoId);
        }

        public async Task<bool> CanAccessPhotoAsync(int photoId, int userId)
        {
            var photo = await _photoRepo.GetByIdAsync(photoId);
            if (photo == null)
                return false;

            // 公开照片任何人可见
            if (photo.IsPublic)
                return true;

            // 预约的用户可见
            if (photo.Booking.UserId == userId)
                return true;

            // 摄影师可见
            if (photo.Booking.Photographer.UserId == userId)
                return true;

            return false;
        }

        public async Task<bool> IsPhotographerPhotoAsync(int photoId, int photographerId)
        {
            var photo = await _photoRepo.GetByIdAsync(photoId);
            if (photo == null)
                return false;

            return photo.Booking.PhotographerId == photographerId;
        }

        // 辅助方法 - 实体映射到DTO
        private PhotoDto MapToDto(Photo photo)
        {
            return new PhotoDto
            {
                PhotoId = photo.PhotoId,
                BookingId = photo.BookingId,
                ImagePath = photo.ImagePath,
                Title = photo.Title,
                Description = photo.Description,
                Metadata = photo.Metadata,
                UploadedAt = photo.UploadedAt,
                IsPublic = photo.IsPublic,
                ClientApproved = photo.ClientApproved
            };
        }
    }
}