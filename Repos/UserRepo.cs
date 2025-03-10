// Repos/UserRepo.cs
using Microsoft.EntityFrameworkCore;
using PixelPerfect.Entities;

namespace PixelPerfect.Repos
{
	public class UserRepo
	{
		private readonly PhotoBookingDbContext _context;

		public UserRepo(PhotoBookingDbContext context)
		{
			_context = context;
		}

		// 基础用户操作
		public async Task<User?> GetByIdAsync(int userId)
		{
			return await _context.Users.FindAsync(userId);
		}

		public async Task<User?> GetByUsernameAsync(string username)
		{
			return await _context.Users
				.FirstOrDefaultAsync(u => u.Username == username);
		}

		public async Task<User?> GetByEmailAsync(string email)
		{
			return await _context.Users
				.FirstOrDefaultAsync(u => u.Email == email);
		}

		public async Task<List<User>> GetAllAsync()
		{
			return await _context.Users.ToListAsync();
		}

		public async Task<List<User>> GetUsersByRoleAsync(string roleType)
		{
			return await _context.Users
				.Where(u => _context.Userroles.Any(ur => ur.UserId == u.UserId && ur.RoleType == roleType))
				.ToListAsync();
		}

		public async Task<User> CreateAsync(User user)
		{
			_context.Users.Add(user);
			await _context.SaveChangesAsync();
			return user;
		}

		public async Task<bool> UpdateAsync(User user)
		{
			_context.Users.Update(user);
			var affected = await _context.SaveChangesAsync();
			return affected > 0;
		}

		public async Task<bool> DeleteAsync(int userId)
		{
			var user = await GetByIdAsync(userId);
			if (user == null) return false;

			_context.Users.Remove(user);
			var affected = await _context.SaveChangesAsync();
			return affected > 0;
		}

		// 用户角色相关
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

		// 摄影师相关
		public async Task<Photographer?> GetPhotographerByUserIdAsync(int userId)
		{
			return await _context.Photographers
				.FirstOrDefaultAsync(p => p.UserId == userId);
		}

		public async Task<Photographer?> GetPhotographerByIdAsync(int photographerId)
		{
			return await _context.Photographers.FindAsync(photographerId);
		}

		public async Task<List<Photographer>> GetAllPhotographersAsync(bool verifiedOnly = false)
		{
			var query = _context.Photographers.AsQueryable();

			if (verifiedOnly)
			{
				query = query.Where(p => p.IsVerified);
			}

			return await query
				.Include(p => p.User)
				.ToListAsync();
		}

		public async Task<Photographer> CreatePhotographerAsync(Photographer photographer)
		{
			_context.Photographers.Add(photographer);
			await _context.SaveChangesAsync();
			return photographer;
		}

		public async Task<bool> UpdatePhotographerAsync(Photographer photographer)
		{
			_context.Photographers.Update(photographer);
			var affected = await _context.SaveChangesAsync();
			return affected > 0;
		}

		// 修图师相关
		public async Task<Retoucher?> GetRetoucherByUserIdAsync(int userId)
		{
			return await _context.Retouchers
				.FirstOrDefaultAsync(r => r.UserId == userId);
		}

		public async Task<Retoucher?> GetRetoucherByIdAsync(int retoucherId)
		{
			return await _context.Retouchers.FindAsync(retoucherId);
		}

		public async Task<List<Retoucher>> GetAllRetouchersAsync(bool verifiedOnly = false)
		{
			var query = _context.Retouchers.AsQueryable();

			if (verifiedOnly)
			{
				query = query.Where(r => r.IsVerified);
			}

			return await query
				.Include(r => r.User)
				.ToListAsync();
		}

		public async Task<Retoucher> CreateRetoucherAsync(Retoucher retoucher)
		{
			_context.Retouchers.Add(retoucher);
			await _context.SaveChangesAsync();
			return retoucher;
		}

		public async Task<bool> UpdateRetoucherAsync(Retoucher retoucher)
		{
			_context.Retouchers.Update(retoucher);
			var affected = await _context.SaveChangesAsync();
			return affected > 0;
		}

		// 搜索功能
		public async Task<List<User>> SearchAsync(string searchTerm)
		{
			return await _context.Users
				.Where(u => u.Username.Contains(searchTerm) ||
							u.Email.Contains(searchTerm) ||
							u.FirstName.Contains(searchTerm) ||
							u.LastName.Contains(searchTerm))
				.ToListAsync();
		}
	}
}