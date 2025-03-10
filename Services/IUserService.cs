// Services/IUser.cs
using PixelPerfect.Entities;
namespace PixelPerfect.Services
{
    public interface IUserService
    {
        // 基础用户操作
        Task<User> GetUserByIdAsync(int userId);
        Task<User> GetUserByUsernameAsync(string username);
        Task<User> GetUserByEmailAsync(string email);
        Task<List<User>> GetAllUsersAsync(string? roleType = null);
        Task<List<User>> GetUsersByRoleAsync(string roleType);
        Task<User> CreateUserAsync(User user, string password, List<string> roles = null);
        Task<bool> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int userId);

        // 用户角色相关
        Task<List<string>> GetUserRolesAsync(int userId);
        Task<bool> AddUserRoleAsync(int userId, string roleType);
        Task<bool> RemoveUserRoleAsync(int userId, string roleType);
        Task<bool> HasRoleAsync(int userId, string roleType);

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