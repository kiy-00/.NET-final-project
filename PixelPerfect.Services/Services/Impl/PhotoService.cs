﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PixelPerfect.Core.Entities;
using PixelPerfect.Core.Models;
using PixelPerfect.DataAccess.Repos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PixelPerfect.Services.Impl
{
    public class PhotoService : IPhotoService
    {
        private readonly PhotoBookingDbContext _context;
        private readonly PhotoRepo _photoRepo;
        private readonly BookingRepo _bookingRepo;
        private readonly IFileStorageService _fileStorage;
        private readonly IConfiguration _config;

        public PhotoService(
           PhotoBookingDbContext context,
            PhotoRepo photoRepo,
            BookingRepo bookingRepo,
            IFileStorageService fileStorage,
            IConfiguration config)
        {
            _context = context;
            _photoRepo = photoRepo;
            _bookingRepo = bookingRepo;
            _fileStorage = fileStorage;
            _config = config;
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

            // 使用文件存储服务保存文件
            string directory = $"photos/{request.BookingId}";
            string filePath = await _fileStorage.SaveFileAsync(file, directory);

            // 生成缩略图
            string thumbnailPath = await _fileStorage.GenerateThumbnailAsync(
                filePath,
                _config.GetValue<int>("FileStorage:ThumbnailWidth", 300),
                _config.GetValue<int>("FileStorage:ThumbnailHeight", 300)
            );

            // 提取元数据
            var metadata = new
            {
                OriginalName = file.FileName,
                Size = file.Length,
                ContentType = file.ContentType,
                ThumbnailPath = thumbnailPath,
                UploadedAt = DateTime.UtcNow
            };

            // 创建照片记录
            var newPhoto = new Photo
            {
                BookingId = request.BookingId,
                ImagePath = filePath,
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
            string directory = $"photos/{request.BookingId}";

            foreach (var file in files)
            {
                try
                {
                    // 使用文件存储服务保存文件
                    string filePath = await _fileStorage.SaveFileAsync(file, directory);

                    // 生成缩略图
                    string thumbnailPath = await _fileStorage.GenerateThumbnailAsync(
                        filePath,
                        _config.GetValue<int>("FileStorage:ThumbnailWidth", 300),
                        _config.GetValue<int>("FileStorage:ThumbnailHeight", 300)
                    );

                    // 提取元数据
                    var metadata = new
                    {
                        OriginalName = file.FileName,
                        Size = file.Length,
                        ContentType = file.ContentType,
                        ThumbnailPath = thumbnailPath,
                        UploadedAt = DateTime.UtcNow
                    };

                    // 创建照片记录
                    var newPhoto = new Photo
                    {
                        BookingId = request.BookingId,
                        ImagePath = filePath,
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
                catch (Exception ex)
                {
                    // 记录错误
                    Console.WriteLine($"上传文件失败: {ex.Message}");
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
                await _fileStorage.DeleteFileAsync(photo.ImagePath);

                // 尝试解析元数据删除缩略图
                try
                {
                    var metadataObj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(photo.Metadata);
                    if (metadataObj != null && metadataObj.ContainsKey("ThumbnailPath"))
                    {
                        string thumbnailPath = metadataObj["ThumbnailPath"].GetString();
                        if (!string.IsNullOrEmpty(thumbnailPath))
                        {
                            await _fileStorage.DeleteFileAsync(thumbnailPath);
                        }
                    }
                }
                catch
                {
                    // 忽略元数据解析错误
                }
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
            var dto = new PhotoDto
            {
                PhotoId = photo.PhotoId,
                BookingId = photo.BookingId,
                ImagePath = photo.ImagePath,
                ImageUrl = _fileStorage.GetFileUrl(photo.ImagePath),
                Title = photo.Title,
                Description = photo.Description,
                Metadata = photo.Metadata,
                UploadedAt = photo.UploadedAt,
                IsPublic = photo.IsPublic,
                ClientApproved = photo.ClientApproved
            };

            // 尝试从元数据中获取缩略图URL
            try
            {
                var metadataObj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(photo.Metadata);
                if (metadataObj != null && metadataObj.ContainsKey("ThumbnailPath"))
                {
                    string thumbnailPath = metadataObj["ThumbnailPath"].GetString();
                    if (!string.IsNullOrEmpty(thumbnailPath))
                    {
                        dto.ThumbnailUrl = _fileStorage.GetFileUrl(thumbnailPath);
                    }
                }
            }
            catch
            {
                // 忽略元数据解析错误
            }

            return dto;
        }
    }
}