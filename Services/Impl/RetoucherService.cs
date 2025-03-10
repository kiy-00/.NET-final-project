using Microsoft.EntityFrameworkCore;
using PixelPerfect.Entities;
using PixelPerfect.Models;
using PixelPerfect.Repos;

namespace PixelPerfect.Services.Impl
{
    public class RetoucherService : IRetoucherService
    {
        private readonly PhotobookingdbContext _context;
        private readonly RetoucherRepo _retoucherRepo;

        public RetoucherService(PhotobookingdbContext context, RetoucherRepo retoucherRepo)
        {
            _context = context;
            _retoucherRepo = retoucherRepo;
        }

        public async Task<RetoucherDto> GetRetoucherByIdAsync(int retoucherId)
        {
            var retoucher = await _retoucherRepo.GetByIdAsync(retoucherId);
            if (retoucher == null)
                return null;

            return MapToDto(retoucher);
        }

        public async Task<List<RetoucherDto>> GetAllRetouchersAsync(bool verifiedOnly = false)
        {
            var retouchers = await _retoucherRepo.GetAllAsync(verifiedOnly);
            return retouchers.Select(MapToDto).ToList();
        }

        public async Task<RetoucherDto> GetRetoucherByUserIdAsync(int userId)
        {
            var retoucher = await _retoucherRepo.GetByUserIdAsync(userId);
            if (retoucher == null)
                return null;

            return MapToDto(retoucher);
        }

        public async Task<RetoucherDto> CreateRetoucherProfileAsync(int userId, RetoucherCreateRequest request)
        {
            // 检查用户是否存在
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {userId} not found.");

            // 检查用户是否已经是修图师
            var existingRetoucher = await _retoucherRepo.GetByUserIdAsync(userId);
            if (existingRetoucher != null)
                throw new InvalidOperationException("User already has a retoucher profile.");

            // 创建修图师档案
            var newRetoucher = new Retoucher
            {
                UserId = userId,
                Bio = request.Bio,
                Expertise = request.Expertise,
                Software = request.Software,
                PricePerPhoto = request.PricePerPhoto,
                IsVerified = false
            };

            // 更新用户类型
            user.UserType = "Retoucher";
            _context.Users.Update(user);

            var createdRetoucher = await _retoucherRepo.CreateAsync(newRetoucher);
            return MapToDto(createdRetoucher);
        }

        public async Task<bool> UpdateRetoucherProfileAsync(int retoucherId, RetoucherUpdateRequest request)
        {
            var retoucher = await _retoucherRepo.GetByIdAsync(retoucherId);
            if (retoucher == null)
                throw new KeyNotFoundException($"Retoucher with ID {retoucherId} not found.");

            // 更新字段
            retoucher.Bio = request.Bio ?? retoucher.Bio;
            retoucher.Expertise = request.Expertise ?? retoucher.Expertise;
            retoucher.Software = request.Software ?? retoucher.Software;
            retoucher.PricePerPhoto = request.PricePerPhoto ?? retoucher.PricePerPhoto;

            return await _retoucherRepo.UpdateAsync(retoucher);
        }

        public async Task<bool> IsOwnerAsync(int retoucherId, int userId)
        {
            var retoucher = await _retoucherRepo.GetByIdAsync(retoucherId);
            if (retoucher == null)
                return false;

            return retoucher.UserId == userId;
        }

        public async Task<List<RetoucherDto>> SearchRetouchersAsync(RetoucherSearchParams searchParams)
        {
            var query = _context.Retouchers
                .Include(r => r.User)
                .AsQueryable();

            // 仅返回已验证的修图师
            if (searchParams.VerifiedOnly)
                query = query.Where(r => r.IsVerified);

            // 按价格范围筛选
            if (searchParams.MinPrice.HasValue)
                query = query.Where(r => r.PricePerPhoto >= searchParams.MinPrice);

            if (searchParams.MaxPrice.HasValue)
                query = query.Where(r => r.PricePerPhoto <= searchParams.MaxPrice);

            // 按专业领域筛选
            if (!string.IsNullOrEmpty(searchParams.Expertise))
                query = query.Where(r => r.Expertise.Contains(searchParams.Expertise));

            // 按使用软件筛选
            if (!string.IsNullOrEmpty(searchParams.Software))
                query = query.Where(r => r.Software.Contains(searchParams.Software));

            // 按关键词搜索（可以搜索用户名、Bio等）
            if (!string.IsNullOrEmpty(searchParams.Keyword))
                query = query.Where(r =>
                    r.User.Username.Contains(searchParams.Keyword) ||
                    r.Bio.Contains(searchParams.Keyword) ||
                    r.Expertise.Contains(searchParams.Keyword) ||
                    r.Software.Contains(searchParams.Keyword));

            var retouchers = await query.ToListAsync();
            return retouchers.Select(MapToDto).ToList();
        }

        // 辅助方法 - 实体映射到DTO
        private RetoucherDto MapToDto(Retoucher retoucher)
        {
            return new RetoucherDto
            {
                RetoucherId = retoucher.RetoucherId,
                UserId = retoucher.UserId,
                Username = retoucher.User?.Username,
                FirstName = retoucher.User?.FirstName,
                LastName = retoucher.User?.LastName,
                Bio = retoucher.Bio,
                Expertise = retoucher.Expertise,
                Software = retoucher.Software,
                PricePerPhoto = retoucher.PricePerPhoto,
                IsVerified = retoucher.IsVerified,
                VerifiedAt = retoucher.VerifiedAt
            };
        }
    }
}