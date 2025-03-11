using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixelPerfect.Core.Models;
using PixelPerfect.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PixelPerfect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FollowController : ControllerBase
    {
        private readonly IFollowService _followService;

        public FollowController(IFollowService followService)
        {
            _followService = followService;
        }

        // 获取当前用户ID
        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        // 关注用户
        [HttpPost("{userId}")]
        public async Task<IActionResult> FollowUser(int userId)
        {
            var currentUserId = GetCurrentUserId();

            // 不能关注自己
            if (currentUserId == userId)
                return BadRequest(new { message = "不能关注自己" });

            var result = await _followService.FollowUserAsync(currentUserId, userId);

            if (!result)
                return BadRequest(new { message = "关注失败，可能已经关注了该用户" });

            return Ok(new { message = "关注成功" });
        }

        // 取消关注
        [HttpDelete("{userId}")]
        public async Task<IActionResult> UnfollowUser(int userId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _followService.UnfollowUserAsync(currentUserId, userId);

            if (!result)
                return BadRequest(new { message = "取消关注失败，可能尚未关注该用户" });

            return Ok(new { message = "已取消关注" });
        }

        // 检查关注状态
        [HttpGet("status/{userId}")]
        public async Task<IActionResult> CheckFollowStatus(int userId)
        {
            var currentUserId = GetCurrentUserId();
            var isFollowing = await _followService.IsFollowingAsync(currentUserId, userId);

            return Ok(new FollowStatusResponse { IsFollowing = isFollowing });
        }

        // 获取某用户的关注者列表
        [HttpGet("followers/{userId}")]
        public async Task<IActionResult> GetUserFollowers(
            int userId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var followers = await _followService.GetFollowersAsync(userId, pageNumber, pageSize);
            return Ok(followers);
        }

        // 获取某用户关注的人列表
        [HttpGet("following/{userId}")]
        public async Task<IActionResult> GetUserFollowing(
            int userId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var following = await _followService.GetFollowingAsync(userId, pageNumber, pageSize);
            return Ok(following);
        }

        // 获取关注统计信息
        [HttpGet("stats/{userId}")]
        public async Task<IActionResult> GetFollowStats(int userId)
        {
            var stats = await _followService.GetFollowStatsAsync(userId);
            return Ok(stats);
        }

        // 获取当前用户的关注者
        [HttpGet("my/followers")]
        public async Task<IActionResult> GetMyFollowers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var currentUserId = GetCurrentUserId();
            var followers = await _followService.GetFollowersAsync(currentUserId, pageNumber, pageSize);
            return Ok(followers);
        }

        // 获取当前用户关注的人
        [HttpGet("my/following")]
        public async Task<IActionResult> GetMyFollowing(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var currentUserId = GetCurrentUserId();
            var following = await _followService.GetFollowingAsync(currentUserId, pageNumber, pageSize);
            return Ok(following);
        }
    }
}