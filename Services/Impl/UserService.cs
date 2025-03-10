// Services/Impl/UserService.cs
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PixelPerfect.Entities;
using PixelPerfect.Repos;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PixelPerfect.Services.Impl
{
    public class UserService : IUserService
    {
        private readonly UserRepo _userRepo;
        private readonly IConfiguration _configuration;

        public UserService(UserRepo userRepo, IConfiguration configuration)
        {
            _userRepo = userRepo;
            _configuration = configuration;
        }

        // 基础用户操作
        public async Task<User> GetUserByIdAsync(int userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            return user;
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

        public async Task<List<User>> GetAllUsersAsync(string? userType = null)
        {
            return await _userRepo.GetAllAsync(userType);
        }

        public async Task<User> CreateUserAsync(User user, string password)
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

            return await _userRepo.CreateAsync(user);
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
            var token = GenerateJwtToken(user);

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
            // 确认用户存在且类型正确
            var user = await _userRepo.GetByIdAsync(photographer.UserId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {photographer.UserId} not found.");

            // 更新用户类型为摄影师
            user.UserType = "Photographer";
            await _userRepo.UpdateAsync(user);

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
            // 确认用户存在且类型正确
            var user = await _userRepo.GetByIdAsync(retoucher.UserId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {retoucher.UserId} not found.");

            // 更新用户类型为修图师
            user.UserType = "Retoucher";
            await _userRepo.UpdateAsync(user);

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

        private string GenerateJwtToken(User user)
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

                // 检查用户属性
                Console.WriteLine($"用户信息: ID={user.UserId}, Username={user.Username}, Email={user.Email}, Type={user.UserType}");

                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.UserType)
        };

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
    }
}