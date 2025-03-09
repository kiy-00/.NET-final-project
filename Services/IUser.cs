// Services/IUser.cs
using PixelPerfect.Entities;

namespace PixelPerfect.Services
{
    public interface IUser
    {
        // 基础用户操作
        Task<User> GetUserByIdAsync(int userId);
        Task<User> GetUserByUsernameAsync(string username);
        Task<User> GetUserByEmailAsync(string email);
        Task<List<User>> GetAllUsersAsync(string? userType = null);
        Task<User> CreateUserAsync(User user, string password);
        Task<bool> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int userId);

        // 认证相关
        Task<(User? user, string token)> AuthenticateAsync(string usernameOrEmail, string password);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<bool> IsEmailUniqueAsync(string email);
        Task<bool> IsUsernameUniqueAsync(string username);

        // 摄影师相关
        Task<Photographer?> GetPhotographerByUserIdAsync(int userId);
        Task<Photographer?> GetPhotographerByIdAsync(int photographerId);
        Task<List<Photographer>> GetAllPhotographersAsync(bool verifiedOnly = false);
        Task<Photographer> CreatePhotographerProfileAsync(Photographer photographer);
        Task<bool> UpdatePhotographerProfileAsync(Photographer photographer);
        Task<bool> VerifyPhotographerAsync(int photographerId);

        // 修图师相关
        Task<Retoucher?> GetRetoucherByUserIdAsync(int userId);
        Task<Retoucher?> GetRetoucherByIdAsync(int retoucherId);
        Task<List<Retoucher>> GetAllRetouchersAsync(bool verifiedOnly = false);
        Task<Retoucher> CreateRetoucherProfileAsync(Retoucher retoucher);
        Task<bool> UpdateRetoucherProfileAsync(Retoucher retoucher);
        Task<bool> VerifyRetoucherAsync(int retoucherId);

        // 管理员功能
        Task<bool> SetUserActiveStatusAsync(int userId, bool isActive);
        Task<List<User>> SearchUsersAsync(string searchTerm);
    }
}