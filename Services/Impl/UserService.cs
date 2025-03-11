using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PixelPerfect.Entities;
using PixelPerfect.Repos;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace PixelPerfect.Services.Impl
{
    public class UserService : IUserService
    {
        private readonly UserRepo _userRepo;
        private readonly IConfiguration _configuration;
        private readonly PhotoBookingDbContext _context;

        public UserService(UserRepo userRepo, IConfiguration configuration, PhotoBookingDbContext context)
        {
            _userRepo = userRepo;
            _configuration = configuration;
            _context = context;
        }

        // 基础用户操作
        public async Task<User> GetUserByIdAsync(int userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            return user;
        }

        public async Task<List<User>> GetUsersByRoleAsync(string roleType)
        {
            return await _userRepo.GetUsersByRoleAsync(roleType);
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            var user = await _userRepo.GetByUsernameAsync(username);
            if (user == null)
                throw new KeyNotFoundException($"User with username {username} not found.");
            return user;
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            var user = await _userRepo.GetByEmailAsync(email);
            if (user == null)
                throw new KeyNotFoundException($"User with email {email} not found.");
            return user;
        }

        public async Task<List<User>> GetAllUsersAsync(string? roleType = null)
        {
            if (string.IsNullOrEmpty(roleType))
            {
                return await _userRepo.GetAllAsync();
            }
            else
            {
                return await _userRepo.GetUsersByRoleAsync(roleType);
            }
        }

        public async Task<User> CreateUserAsync(User user, string password, List<string> roles = null)
        {
            // 验证用户名和邮箱的唯一性
            if (!await IsUsernameUniqueAsync(user.Username))
                throw new InvalidOperationException("Username is already taken.");

            if (!await IsEmailUniqueAsync(user.Email))
                throw new InvalidOperationException("Email is already registered.");

            // 生成密码哈希和盐值
            var salt = GenerateSalt();
            var passwordHash = HashPassword(password, salt);

            user.PasswordHash = passwordHash;
            user.Salt = salt;
            user.CreatedAt = DateTime.UtcNow;

            // 确保 Biography 字段不为 null (如果没有提供)
            if (user.Biography == null)
                user.Biography = string.Empty;

            // 创建用户
            var createdUser = await _userRepo.CreateAsync(user);

            // 添加用户角色
            roles ??= new List<string> { "Regular" };
            foreach (var role in roles)
            {
                await AddUserRoleAsync(createdUser.UserId, role);
            }

            return createdUser;
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            // 获取原始用户数据进行比较
            var existingUser = await _userRepo.GetByIdAsync(user.UserId);
            if (existingUser == null)
                throw new KeyNotFoundException($"User with ID {user.UserId} not found.");

            // 如果用户名改变，验证唯一性
            if (existingUser.Username != user.Username && !await IsUsernameUniqueAsync(user.Username))
                throw new InvalidOperationException("Username is already taken.");

            // 如果邮箱改变，验证唯一性
            if (existingUser.Email != user.Email && !await IsEmailUniqueAsync(user.Email))
                throw new InvalidOperationException("Email is already registered.");

            // 保持密码哈希不变
            user.PasswordHash = existingUser.PasswordHash;
            user.Salt = existingUser.Salt;

            return await _userRepo.UpdateAsync(user);
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            return await _userRepo.DeleteAsync(userId);
        }

        // 用户角色相关方法
        public async Task<List<string>> GetUserRolesAsync(int userId)
        {
            return await _context.Userroles
                .Where(r => r.UserId == userId)
                .Select(r => r.RoleType)
                .ToListAsync();
        }

        public async Task<bool> AddUserRoleAsync(int userId, string roleType)
        {
            // 检查用户是否存在
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            // 检查是否已有该角色
            var existingRole = await _context.Userroles
                .FirstOrDefaultAsync(r => r.UserId == userId && r.RoleType == roleType);
            if (existingRole != null) return true; // 已有该角色

            // 添加新角色
            var userRole = new Userrole
            {
                UserId = userId,
                RoleType = roleType,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Userroles.AddAsync(userRole);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> RemoveUserRoleAsync(int userId, string roleType)
        {
            var role = await _context.Userroles
                .FirstOrDefaultAsync(r => r.UserId == userId && r.RoleType == roleType);

            if (role == null) return false;

            _context.Userroles.Remove(role);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> HasRoleAsync(int userId, string roleType)
        {
            return await _context.Userroles
                .AnyAsync(r => r.UserId == userId && r.RoleType == roleType);
        }

        // 认证相关
        public async Task<(User? user, string token)> AuthenticateAsync(string usernameOrEmail, string password)
        {
            // 通过用户名或邮箱查找用户
            var user = await _userRepo.GetByUsernameAsync(usernameOrEmail)
                    ?? await _userRepo.GetByEmailAsync(usernameOrEmail);

            if (user == null)
                return (null, string.Empty);

            // 验证密码
            if (!VerifyPassword(password, user.PasswordHash, user.Salt))
                return (null, string.Empty);

            // 更新最后登录时间
            user.LastLogin = DateTime.UtcNow;
            await _userRepo.UpdateAsync(user);

            // 生成JWT令牌
            var token = await GenerateJwtTokenAsync(user);

            return (user, token);
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
                return false;

            // 验证当前密码
            if (!VerifyPassword(currentPassword, user.PasswordHash, user.Salt))
                return false;

            // 更新密码
            var salt = GenerateSalt();
            var passwordHash = HashPassword(newPassword, salt);

            user.PasswordHash = passwordHash;
            user.Salt = salt;

            return await _userRepo.UpdateAsync(user);
        }

        public async Task<bool> IsEmailUniqueAsync(string email)
        {
            var user = await _userRepo.GetByEmailAsync(email);
            return user == null;
        }

        public async Task<bool> IsUsernameUniqueAsync(string username)
        {
            var user = await _userRepo.GetByUsernameAsync(username);
            return user == null;
        }

        // 摄影师相关
        public async Task<Photographer?> GetPhotographerByUserIdAsync(int userId)
        {
            return await _userRepo.GetPhotographerByUserIdAsync(userId);
        }

        public async Task<Photographer?> GetPhotographerByIdAsync(int photographerId)
        {
            return await _userRepo.GetPhotographerByIdAsync(photographerId);
        }

        public async Task<List<Photographer>> GetAllPhotographersAsync(bool verifiedOnly = false)
        {
            return await _userRepo.GetAllPhotographersAsync(verifiedOnly);
        }

        public async Task<Photographer> CreatePhotographerProfileAsync(Photographer photographer)
        {
            // 确认用户存在
            var user = await _userRepo.GetByIdAsync(photographer.UserId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {photographer.UserId} not found.");

            // 添加摄影师角色
            await AddUserRoleAsync(photographer.UserId, "Photographer");

            // 创建摄影师档案
            photographer.IsVerified = false;
            return await _userRepo.CreatePhotographerAsync(photographer);
        }

        public async Task<bool> UpdatePhotographerProfileAsync(Photographer photographer)
        {
            var existingPhotographer = await _userRepo.GetPhotographerByIdAsync(photographer.PhotographerId);
            if (existingPhotographer == null)
                throw new KeyNotFoundException($"Photographer with ID {photographer.PhotographerId} not found.");

            // 保持验证状态不变
            photographer.IsVerified = existingPhotographer.IsVerified;
            photographer.VerifiedAt = existingPhotographer.VerifiedAt;

            return await _userRepo.UpdatePhotographerAsync(photographer);
        }

        public async Task<bool> VerifyPhotographerAsync(int photographerId)
        {
            var photographer = await _userRepo.GetPhotographerByIdAsync(photographerId);
            if (photographer == null)
                throw new KeyNotFoundException($"Photographer with ID {photographerId} not found.");

            photographer.IsVerified = true;
            photographer.VerifiedAt = DateTime.UtcNow;

            return await _userRepo.UpdatePhotographerAsync(photographer);
        }

        // 修图师相关
        public async Task<Retoucher?> GetRetoucherByUserIdAsync(int userId)
        {
            return await _userRepo.GetRetoucherByUserIdAsync(userId);
        }

        public async Task<Retoucher?> GetRetoucherByIdAsync(int retoucherId)
        {
            return await _userRepo.GetRetoucherByIdAsync(retoucherId);
        }

        public async Task<List<Retoucher>> GetAllRetouchersAsync(bool verifiedOnly = false)
        {
            return await _userRepo.GetAllRetouchersAsync(verifiedOnly);
        }

        public async Task<Retoucher> CreateRetoucherProfileAsync(Retoucher retoucher)
        {
            // 确认用户存在
            var user = await _userRepo.GetByIdAsync(retoucher.UserId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {retoucher.UserId} not found.");

            // 添加修图师角色
            await AddUserRoleAsync(retoucher.UserId, "Retoucher");

            // 创建修图师档案
            retoucher.IsVerified = false;
            return await _userRepo.CreateRetoucherAsync(retoucher);
        }

        public async Task<bool> UpdateRetoucherProfileAsync(Retoucher retoucher)
        {
            var existingRetoucher = await _userRepo.GetRetoucherByIdAsync(retoucher.RetoucherId);
            if (existingRetoucher == null)
                throw new KeyNotFoundException($"Retoucher with ID {retoucher.RetoucherId} not found.");

            // 保持验证状态不变
            retoucher.IsVerified = existingRetoucher.IsVerified;
            retoucher.VerifiedAt = existingRetoucher.VerifiedAt;

            return await _userRepo.UpdateRetoucherAsync(retoucher);
        }

        public async Task<bool> VerifyRetoucherAsync(int retoucherId)
        {
            var retoucher = await _userRepo.GetRetoucherByIdAsync(retoucherId);
            if (retoucher == null)
                throw new KeyNotFoundException($"Retoucher with ID {retoucherId} not found.");

            retoucher.IsVerified = true;
            retoucher.VerifiedAt = DateTime.UtcNow;

            return await _userRepo.UpdateRetoucherAsync(retoucher);
        }

        // 管理员功能
        public async Task<bool> SetUserActiveStatusAsync(int userId, bool isActive)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {userId} not found.");

            user.IsActive = isActive;
            return await _userRepo.UpdateAsync(user);
        }

        public async Task<List<User>> SearchUsersAsync(string searchTerm)
        {
            return await _userRepo.SearchAsync(searchTerm);
        }

        // 辅助方法
        private string GenerateSalt()
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return Convert.ToBase64String(salt);
        }

        private string HashPassword(string password, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256))
            {
                var hashBytes = pbkdf2.GetBytes(32);
                return Convert.ToBase64String(hashBytes);
            }
        }

        private bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            var salt = Convert.FromBase64String(storedSalt);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
            {
                var hashBytes = pbkdf2.GetBytes(32);
                var hash = Convert.ToBase64String(hashBytes);

                return hash == storedHash;
            }
        }

        private async Task<string> GenerateJwtTokenAsync(User user)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();

                // 使用正确的配置路径
                var jwtSecret = _configuration["JwtSettings:SecretKey"];

                Console.WriteLine($"JWT Secret 配置状态: {(string.IsNullOrEmpty(jwtSecret) ? "为空" : "已配置")}");

                if (string.IsNullOrEmpty(jwtSecret))
                {
                    Console.WriteLine("严重错误: JWT Secret未配置");
                    throw new InvalidOperationException("JWT Secret配置缺失。请检查appsettings.json文件。");
                }

                var key = Encoding.ASCII.GetBytes(jwtSecret);

                // 获取用户角色
                var userRoles = await GetUserRolesAsync(user.UserId);

                // 检查用户属性
                Console.WriteLine($"用户信息: ID={user.UserId}, Username={user.Username}, Email={user.Email}, Roles={string.Join(",", userRoles)}");

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email)
                };

                // 添加所有角色
                foreach (var role in userRoles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddDays(7),
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                Console.WriteLine("JWT令牌生成成功");
                return tokenString;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"生成JWT令牌时出错: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                throw; // 重新抛出异常以便在控制器中捕获
            }
        }

        // 用户关注相关
        public async Task<bool> FollowUserAsync(int followerId, int followedId)
        {
            // 不能关注自己
            if (followerId == followedId)
                return false;

            // 检查用户是否存在
            var follower = await _userRepo.GetByIdAsync(followerId);
            var followed = await _userRepo.GetByIdAsync(followedId);
            if (follower == null || followed == null)
                return false;

            // 检查是否已经关注
            var existingFollow = await _userRepo.GetFollowAsync(followerId, followedId);
            if (existingFollow != null)
            {
                // 如果之前取消了关注，可以重新激活
                if (existingFollow.Status != "Active")
                {
                    return await _userRepo.UpdateFollowStatusAsync(existingFollow, "Active");
                }
                return false; // 已经处于关注状态
            }

            // 创建关注关系
            return await _userRepo.CreateFollowAsync(followerId, followedId);
        }

        public async Task<bool> UnfollowUserAsync(int followerId, int followedId)
        {
            return await _userRepo.DeleteFollowAsync(followerId, followedId);
        }

        public async Task<bool> IsFollowingAsync(int followerId, int followedId)
        {
            var follow = await _userRepo.GetFollowAsync(followerId, followedId);
            return follow != null && follow.Status == "Active";
        }

        public async Task<List<User>> GetFollowersAsync(int userId, int pageNumber = 1, int pageSize = 20)
        {
            return await _userRepo.GetFollowersAsync(userId, pageNumber, pageSize);
        }

        public async Task<List<User>> GetFollowingAsync(int userId, int pageNumber = 1, int pageSize = 20)
        {
            return await _userRepo.GetFollowingAsync(userId, pageNumber, pageSize);
        }

        public async Task<(int followersCount, int followingCount)> GetFollowStatsAsync(int userId)
        {
            var followersCount = await _userRepo.GetFollowersCountAsync(userId);
            var followingCount = await _userRepo.GetFollowingCountAsync(userId);
            return (followersCount, followingCount);
        }
    }
}