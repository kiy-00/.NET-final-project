using System;

namespace PixelPerfect.Models
{
    // 点赞DTO
    public class LikeDto
    {
        public int LikeId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public int PostId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}