using Microsoft.EntityFrameworkCore;
using PixelPerfect.Entities;
using PixelPerfect.Models;
using PixelPerfect.Repos;

namespace PixelPerfect.Services.Impl
{
	public class PhotographerService : IPhotographerService
	{
		private readonly PhotoBookingDbContext _context;
		private readonly PhotographerRepo _photographerRepo;
		private readonly IUserService _userService;

		public PhotographerService(PhotoBookingDbContext context, PhotographerRepo photographerRepo, IUserService userService)
		{
			_context = context;
			_photographerRepo = photographerRepo;
			_userService = userService;
		}

		public async Task<PhotographerDto> GetPhotographerByIdAsync(int photographerId)
		{
			var photographer = await _photographerRepo.GetByIdAsync(photographerId);
			if (photographer == null)
				return null;

			return MapToDto(photographer);
		}

		public async Task<List<PhotographerDto>> GetAllPhotographersAsync(bool verifiedOnly = false)
		{
			var photographers = await _photographerRepo.GetAllAsync(verifiedOnly);
			return photographers.Select(MapToDto).ToList();
		}

		public async Task<PhotographerDto> GetPhotographerByUserIdAsync(int userId)
		{
			var photographer = await _photographerRepo.GetByUserIdAsync(userId);
			if (photographer == null)
				return null;

			return MapToDto(photographer);
		}

		public async Task<PhotographerDto> CreatePhotographerProfileAsync(int userId, PhotographerCreateRequest request)
		{
			// 检查用户是否存在
			var user = await _context.Users.FindAsync(userId);
			if (user == null)
				throw new KeyNotFoundException($"User with ID {userId} not found.");

			// 检查用户是否已经是摄影师
			var existingPhotographer = await _photographerRepo.GetByUserIdAsync(userId);
			if (existingPhotographer != null)
				throw new InvalidOperationException("User already has a photographer profile.");

			// 创建摄影师档案
			var newPhotographer = new Photographer
			{
				UserId = userId,
				Bio = request.Bio,
				Experience = request.Experience,
				EquipmentInfo = request.EquipmentInfo,
				Location = request.Location,
				PriceRangeMin = request.PriceRangeMin,
				PriceRangeMax = request.PriceRangeMax,
				IsVerified = false
			};

			// 添加摄影师角色
			await _userService.AddUserRoleAsync(userId, "Photographer");

			var createdPhotographer = await _photographerRepo.CreateAsync(newPhotographer);
			return MapToDto(createdPhotographer);
		}

		public async Task<bool> UpdatePhotographerProfileAsync(int photographerId, PhotographerUpdateRequest request)
		{
			var photographer = await _photographerRepo.GetByIdAsync(photographerId);
			if (photographer == null)
				throw new KeyNotFoundException($"Photographer with ID {photographerId} not found.");

			// 更新字段
			photographer.Bio = request.Bio ?? photographer.Bio;
			photographer.Experience = request.Experience ?? photographer.Experience;
			photographer.EquipmentInfo = request.EquipmentInfo ?? photographer.EquipmentInfo;
			photographer.Location = request.Location ?? photographer.Location;
			photographer.PriceRangeMin = request.PriceRangeMin ?? photographer.PriceRangeMin;
			photographer.PriceRangeMax = request.PriceRangeMax ?? photographer.PriceRangeMax;

			return await _photographerRepo.UpdateAsync(photographer);
		}

		public async Task<bool> IsOwnerAsync(int photographerId, int userId)
		{
			var photographer = await _photographerRepo.GetByIdAsync(photographerId);
			if (photographer == null)
				return false;

			return photographer.UserId == userId;
		}

		public async Task<List<PhotographerDto>> SearchPhotographersAsync(PhotographerSearchParams searchParams)
		{
			// 首先获取所有具有摄影师角色的用户
			var photographerUserIds = await _context.Userroles
				.Where(r => r.RoleType == "Photographer")
				.Select(r => r.UserId)
				.ToListAsync();

			var query = _context.Photographers
				.Where(p => photographerUserIds.Contains(p.UserId))
				.Include(p => p.User)
				.AsQueryable();

			// 仅返回已验证的摄影师
			if (searchParams.VerifiedOnly)
				query = query.Where(p => p.IsVerified);

			// 按地区筛选
			if (!string.IsNullOrEmpty(searchParams.Location))
				query = query.Where(p => p.Location.Contains(searchParams.Location));

			// 按价格范围筛选
			if (searchParams.MinPrice.HasValue)
				query = query.Where(p => p.PriceRangeMin >= searchParams.MinPrice);

			if (searchParams.MaxPrice.HasValue)
				query = query.Where(p => p.PriceRangeMax <= searchParams.MaxPrice);

			// 按关键词搜索（可以搜索用户名、Bio、经验等）
			if (!string.IsNullOrEmpty(searchParams.Keyword))
				query = query.Where(p =>
					p.User.Username.Contains(searchParams.Keyword) ||
					p.Bio.Contains(searchParams.Keyword) ||
					p.Experience.Contains(searchParams.Keyword));

			var photographers = await query.ToListAsync();
			return photographers.Select(MapToDto).ToList();
		}

		// 辅助方法 - 实体映射到DTO
		private PhotographerDto MapToDto(Photographer photographer)
		{
			return new PhotographerDto
			{
				PhotographerId = photographer.PhotographerId,
				UserId = photographer.UserId,
				Username = photographer.User?.Username,
				FirstName = photographer.User?.FirstName,
				LastName = photographer.User?.LastName,
				Bio = photographer.Bio,
				Experience = photographer.Experience,
				EquipmentInfo = photographer.EquipmentInfo,
				Location = photographer.Location,
				PriceRangeMin = photographer.PriceRangeMin,
				PriceRangeMax = photographer.PriceRangeMax,
				IsVerified = photographer.IsVerified,
				VerifiedAt = photographer.VerifiedAt
			};
		}
	}
}