using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixelPerfect.Core.Models;
using PixelPerfect.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PixelPerfect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LikeController : ControllerBase
    {
        private readonly ILikeService _likeService;

        public LikeController(ILikeService likeService)
        {
            _likeService = likeService;
        }

        // 获取帖子的点赞
        [HttpGet("post/{postId}")]
        public async Task<IActionResult> GetLikesByPostId(int postId)
        {
            try
            {
                var likes = await _likeService.GetLikesByPostIdAsync(postId);
                return Ok(likes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving post likes." });
            }
        }

        // 获取用户的点赞
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetLikesByUserId(int userId)
        {
            try
            {
                var likes = await _likeService.GetLikesByUserIdAsync(userId);
                return Ok(likes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving user likes." });
            }
        }

        // 点赞帖子
        [HttpPost("post/{postId}")]
        [Authorize]
        public async Task<IActionResult> LikePost(int postId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var like = await _likeService.LikePostAsync(userId, postId);
                return Ok(like);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while liking the post." });
            }
        }

        // 取消点赞
        [HttpDelete("post/{postId}")]
        [Authorize]
        public async Task<IActionResult> UnlikePost(int postId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var success = await _likeService.UnlikePostAsync(userId, postId);
                if (success)
                    return Ok(new { message = "Post unliked successfully." });
                else
                    return NotFound(new { message = "Like not found or already removed." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while unliking the post." });
            }
        }

        // 检查用户是否已点赞帖子
        [HttpGet("check/{postId}")]
        [Authorize]
        public async Task<IActionResult> HasUserLikedPost(int postId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var hasLiked = await _likeService.HasUserLikedPostAsync(userId, postId);
                return Ok(new { hasLiked });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while checking like status." });
            }
        }

        // 获取帖子点赞数
        [HttpGet("count/post/{postId}")]
        public async Task<IActionResult> GetLikesCountByPostId(int postId)
        {
            try
            {
                var count = await _likeService.GetLikesCountByPostIdAsync(postId);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving post likes count." });
            }
        }

        // 获取用户获得的点赞数
        [HttpGet("count/user/{userId}")]
        public async Task<IActionResult> GetLikesCountByUserId(int userId)
        {
            try
            {
                var count = await _likeService.GetLikesCountByUserIdAsync(userId);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving user likes count." });
            }
        }
    }
}