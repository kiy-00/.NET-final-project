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
    }
}