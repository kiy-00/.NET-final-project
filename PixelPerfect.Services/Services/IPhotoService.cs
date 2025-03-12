using Microsoft.AspNetCore.Http;
using PixelPerfect.Core.Models;

namespace PixelPerfect.Services
{
    public interface IPhotoService
    {
        // 获取照片
        Task<PhotoDto> GetPhotoByIdAsync(int photoId);
        Task<List<PhotoDto>> GetPhotosByBookingIdAsync(int bookingId);
        Task<List<PhotoCollectionDto>> GetPhotoCollectionsByUserIdAsync(int userId);
        Task<List<PhotoCollectionDto>> GetPhotoCollectionsByPhotographerIdAsync(int photographerId);
        Task<List<PhotoDto>> SearchPhotosAsync(PhotoSearchParams searchParams);

        // 上传照片
        Task<PhotoDto> UploadPhotoAsync(int photographerId, IFormFile file, PhotoUploadRequest request);
        Task<List<PhotoDto>> BatchUploadPhotosAsync(int photographerId, List<IFormFile> files, BatchPhotoUploadRequest request);

        // 更新照片
        Task<bool> UpdatePhotoAsync(int photoId, PhotoUpdateRequest request);
        Task<bool> ApprovePhotoAsync(int photoId, bool clientApproved);
        Task<bool> DeletePhotoAsync(int photoId);

        // 权限检查
        Task<bool> CanAccessPhotoAsync(int photoId, int userId);
        Task<bool> IsPhotographerPhotoAsync(int photoId, int photographerId);

        // 新增通用单张图片上传方法
        Task<PhotoUploadResult> UploadGeneralPhotoAsync(int userId, IFormFile file, string title = null, string description = null);

        // 新增作品集封面图片上传方法
        Task<PhotoUploadResult> UploadPortfolioCoverAsync(int userId, IFormFile file);

        // 新增作品项图片上传方法
        Task<PortfolioItemUploadResult> UploadPortfolioItemPhotoAsync(int userId, IFormFile mainFile, IFormFile beforeFile = null, string title = null, string description = null);

        // 新增临时图片上传方法
        Task<PhotoUploadResult> UploadTempPhotoAsync(int userId, IFormFile file);
    }
}