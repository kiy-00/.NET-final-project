using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using PixelPerfect.Core.Models;
using PixelPerfect.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PixelPerfect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostController : ControllerBase
    {
        private readonly IPostService _postService;

        public PostController(IPostService postService)
        {
            _postService = postService;
        }

        // 获取单个帖子
        [HttpGet("{postId}")]
        public async Task<IActionResult> GetPostById(int postId)
        {
            try
            {
                int? userId = null;
                if (User.Identity.IsAuthenticated)
                {
                    userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                }

                var post = await _postService.GetPostByIdAsync(postId, userId);
                if (post == null)
                    return NotFound(new { message = $"Post with ID {postId} not found." });

                if (!post.IsApproved && !User.IsInRole("Admin") && post.UserId != userId)
                    return Forbid();

                return Ok(post);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the post." });
            }
        }

        // 获取帖子详情（包括点赞信息）
        [HttpGet("{postId}/detail")]
        public async Task<IActionResult> GetPostDetailById(int postId)
        {
            try
            {
                int? userId = null;
                if (User.Identity.IsAuthenticated)
                {
                    userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                }

                var post = await _postService.GetPostDetailByIdAsync(postId, userId);
                if (post == null)
                    return NotFound(new { message = $"Post with ID {postId} not found." });

                if (!post.IsApproved && !User.IsInRole("Admin") && post.UserId != userId)
                    return Forbid();

                return Ok(post);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the post details." });
            }
        }

        // 获取用户的帖子
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetPostsByUserId(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                int? currentUserId = null;
                if (User.Identity.IsAuthenticated)
                {
                    currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                }

                var posts = await _postService.GetPostsByUserIdAsync(userId, page, pageSize, currentUserId);
                return Ok(posts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving user posts." });
            }
        }

        // 搜索帖子
        [HttpGet("search")]
        public async Task<IActionResult> SearchPosts([FromQuery] PostSearchParams searchParams)
        {
            try
            {
                int? userId = null;
                if (User.Identity.IsAuthenticated)
                {
                    userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                }

                // 只有管理员可以看到未审核的帖子
                if (searchParams.IsApproved.HasValue && !searchParams.IsApproved.Value && !User.IsInRole("Admin"))
                {
                    searchParams.IsApproved = true;
                }

                var posts = await _postService.SearchPostsAsync(searchParams, userId);
                return Ok(posts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while searching posts." });
            }
        }

        // 创建帖子
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePost([FromForm] PostCreateRequest request, IFormFile image = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var post = await _postService.CreatePostAsync(userId, request, image);
                return CreatedAtAction(nameof(GetPostById), new { postId = post.PostId }, post);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the post." });
            }
        }

        // 更新帖子
        [HttpPut("{postId}")]
        [Authorize]
        public async Task<IActionResult> UpdatePost(int postId, [FromBody] PostUpdateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 检查是否是帖子作者或管理员
                if (!await _postService.IsPostOwnerAsync(postId, userId) && !User.IsInRole("Admin"))
                    return Forbid();

                var post = await _postService.UpdatePostAsync(postId, request);
                return Ok(post);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the post." });
            }
        }

        // 上传帖子图片
        [HttpPost("{postId}/image")]
        [Authorize]
        public async Task<IActionResult> UploadPostImage(int postId, IFormFile image)
        {
            if (image == null)
                return BadRequest(new { message = "No image provided." });

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 检查是否是帖子作者或管理员
                if (!await _postService.IsPostOwnerAsync(postId, userId) && !User.IsInRole("Admin"))
                    return Forbid();

                var imagePath = await _postService.UploadPostImageAsync(postId, image);
                return Ok(new { imagePath });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while uploading the image." });
            }
        }

        // 删除帖子
        [HttpDelete("{postId}")]
        [Authorize]
        public async Task<IActionResult> DeletePost(int postId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 检查是否是帖子作者或管理员
                if (!await _postService.IsPostOwnerAsync(postId, userId) && !User.IsInRole("Admin"))
                    return Forbid();

                var success = await _postService.DeletePostAsync(postId);
                if (success)
                    return Ok(new { message = "Post deleted successfully." });
                else
                    return NotFound(new { message = "Post not found or already deleted." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the post." });
            }
        }

        // 审核帖子（仅管理员）
        [HttpPut("{postId}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApprovePost(int postId, [FromBody] PostApproveRequest request)
        {
            try
            {
                var adminUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var post = await _postService.ApprovePostAsync(postId, request.IsApproved, adminUserId);
                return Ok(post);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while approving the post." });
            }
        }

        // 获取待审核的帖子（仅管理员）
        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var posts = await _postService.GetPendingPostsAsync(page, pageSize);
                return Ok(posts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving pending posts." });
            }
        }

        // 获取待审核帖子数量（仅管理员）
        [HttpGet("pending/count")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingPostsCount()
        {
            try
            {
                var count = await _postService.GetPendingPostsCountAsync();
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving pending posts count." });
            }
        }
    }
}