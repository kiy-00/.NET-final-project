using PixelPerfect.Entities;
using PixelPerfect.Models;
using PixelPerfect.Repos;

namespace PixelPerfect.Services.Impl
{
    public class RetouchOrderService : IRetouchOrderService
    {
        private readonly PhotobookingdbContext _context;
        private readonly RetouchOrderRepo _retouchOrderRepo;
        private readonly RetoucherRepo _retoucherRepo;
        private readonly PhotoRepo _photoRepo;

        public RetouchOrderService(
            PhotobookingdbContext context,
            RetouchOrderRepo retouchOrderRepo,
            RetoucherRepo retoucherRepo,
            PhotoRepo photoRepo)
        {
            _context = context;
            _retouchOrderRepo = retouchOrderRepo;
            _retoucherRepo = retoucherRepo;
            _photoRepo = photoRepo;
        }

        public async Task<RetouchOrderDto> GetOrderByIdAsync(int orderId)
        {
            var order = await _retouchOrderRepo.GetByIdAsync(orderId);
            if (order == null)
                return null;

            return MapToDto(order);
        }

        public async Task<List<RetouchOrderDto>> GetOrdersByUserIdAsync(int userId)
        {
            var orders = await _retouchOrderRepo.GetByUserIdAsync(userId);
            return orders.Select(MapToDto).ToList();
        }

        public async Task<List<RetouchOrderDto>> GetOrdersByRetoucherIdAsync(int retoucherId)
        {
            var orders = await _retouchOrderRepo.GetByRetoucherIdAsync(retoucherId);
            return orders.Select(MapToDto).ToList();
        }

        public async Task<List<RetouchOrderDto>> GetOrdersByPhotoIdAsync(int photoId)
        {
            var orders = await _retouchOrderRepo.GetByPhotoIdAsync(photoId);
            return orders.Select(MapToDto).ToList();
        }

        public async Task<List<RetouchOrderDto>> SearchOrdersAsync(RetouchOrderSearchParams searchParams)
        {
            var orders = await _retouchOrderRepo.SearchAsync(
                searchParams.UserId,
                searchParams.RetoucherId,
                searchParams.PhotoId,
                searchParams.Status,
                searchParams.StartDate,
                searchParams.EndDate
            );

            return orders.Select(MapToDto).ToList();
        }

        public async Task<RetouchOrderDto> CreateOrderAsync(int userId, RetouchOrderCreateRequest request)
        {
            // 检查修图师是否存在
            var retoucher = await _retoucherRepo.GetByIdAsync(request.RetoucherId);
            if (retoucher == null)
                throw new KeyNotFoundException($"Retoucher with ID {request.RetoucherId} not found.");

            // 检查照片是否存在
            var photo = await _photoRepo.GetByIdAsync(request.PhotoId);
            if (photo == null)
                throw new KeyNotFoundException($"Photo with ID {request.PhotoId} not found.");

            // 检查用户是否是照片的拥有者
            if (photo.Booking.UserId != userId)
                throw new UnauthorizedAccessException("User is not the owner of the photo.");

            // 检查照片是否已经有修图订单
            var existingOrders = await _retouchOrderRepo.GetByPhotoIdAsync(request.PhotoId);
            if (existingOrders.Any(o => o.Status != "Cancelled" && o.Status != "Rejected"))
                throw new InvalidOperationException("Photo already has an active retouch order.");

            // 计算价格（使用修图师的单价）
            decimal price = retoucher.PricePerPhoto ?? 0;

            // 创建订单
            var newOrder = new Retouchorder
            {
                UserId = userId,
                RetoucherId = request.RetoucherId,
                PhotoId = request.PhotoId,
                Status = "Pending", // 初始状态为待确认
                Requirements = request.Requirements,
                Price = price,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = null
            };

            var createdOrder = await _retouchOrderRepo.CreateAsync(newOrder);
            return MapToDto(createdOrder);
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            var order = await _retouchOrderRepo.GetByIdAsync(orderId);
            if (order == null)
                throw new KeyNotFoundException($"Order with ID {orderId} not found.");

            // 验证状态变更是否有效
            var validStatuses = new[] { "Pending", "Accepted", "InProgress", "Completed", "Cancelled", "Rejected" };
            if (!validStatuses.Contains(status))
                throw new ArgumentException($"Invalid order status: {status}");

            // 特定状态转换的业务规则
            if (status == "Completed" && order.Status != "InProgress")
                throw new InvalidOperationException("Only orders in 'InProgress' status can be completed.");

            order.Status = status;

            // 如果状态变为已完成，设置完成时间
            if (status == "Completed")
                order.CompletedAt = DateTime.UtcNow;

            return await _retouchOrderRepo.UpdateAsync(order);
        }

        public async Task<bool> CompleteOrderAsync(int orderId)
        {
            var order = await _retouchOrderRepo.GetByIdAsync(orderId);
            if (order == null)
                throw new KeyNotFoundException($"Order with ID {orderId} not found.");

            if (order.Status != "InProgress")
                throw new InvalidOperationException("Only orders in 'InProgress' status can be completed.");

            order.Status = "Completed";
            order.CompletedAt = DateTime.UtcNow;

            return await _retouchOrderRepo.UpdateAsync(order);
        }

        public async Task<bool> IsUserOrderAsync(int orderId, int userId)
        {
            var order = await _retouchOrderRepo.GetByIdAsync(orderId);
            if (order == null)
                return false;

            return order.UserId == userId;
        }

        public async Task<bool> IsRetoucherOrderAsync(int orderId, int retoucherId)
        {
            var order = await _retouchOrderRepo.GetByIdAsync(orderId);
            if (order == null)
                return false;

            return order.RetoucherId == retoucherId;
        }

        // 辅助方法 - 实体映射到DTO
        private RetouchOrderDto MapToDto(Retouchorder order)
        {
            return new RetouchOrderDto
            {
                OrderId = order.OrderId,
                UserId = order.UserId,
                Username = order.User?.Username,
                RetoucherId = order.RetoucherId,
                RetoucherName = $"{order.Retoucher?.User?.FirstName} {order.Retoucher?.User?.LastName}",
                PhotoId = order.PhotoId,
                PhotoTitle = order.Photo?.Title,
                PhotoPath = order.Photo?.ImagePath,
                Status = order.Status,
                Requirements = order.Requirements,
                Price = order.Price,
                CreatedAt = order.CreatedAt,
                CompletedAt = order.CompletedAt
            };
        }
    }
}