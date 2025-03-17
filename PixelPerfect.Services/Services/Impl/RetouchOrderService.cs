using PixelPerfect.Core.Entities;
using PixelPerfect.Core.Models;
using PixelPerfect.DataAccess.Repos;
using Microsoft.AspNetCore.Http;

namespace PixelPerfect.Services.Impl
{
    public class RetouchOrderService : IRetouchOrderService
    {
        private PhotoBookingDbContext _context;
        private readonly RetouchOrderRepo _retouchOrderRepo;
        private readonly RetoucherRepo _retoucherRepo;
        private readonly PhotoRepo _photoRepo;
        private readonly IFileStorageService _fileStorageService;
        private readonly INotificationService _notificationService;

        public RetouchOrderService(
           PhotoBookingDbContext context,
            RetouchOrderRepo retouchOrderRepo,
            RetoucherRepo retoucherRepo,
            PhotoRepo photoRepo,
            IFileStorageService fileStorageService,
            INotificationService notificationService)
        {
            _context = context;
            _retouchOrderRepo = retouchOrderRepo;
            _retoucherRepo = retoucherRepo;
            _photoRepo = photoRepo;
            _fileStorageService = fileStorageService;
            _notificationService = notificationService;
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

            //// 检查用户是否是照片的拥有者
            //if (photo.Booking.UserId != userId)
            //    throw new UnauthorizedAccessException("User is not the owner of the photo.");

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
                RetouchedPhotoId = null, // 初始为空
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

        // 新增 - 完成修图订单并保存修图后照片URL
        public async Task<bool> CompleteOrderAsync(int orderId, string retouchedPhotoUrl, string comment = null)
        {
            var order = await _retouchOrderRepo.GetByIdAsync(orderId);
            if (order == null)
                throw new KeyNotFoundException($"Order with ID {orderId} not found.");

            if (order.Status != "InProgress")
                throw new InvalidOperationException("Only orders in 'InProgress' status can be completed.");

            // 使用事务确保数据一致性
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 如果提供了URL，创建新的照片记录
                    if (!string.IsNullOrEmpty(retouchedPhotoUrl))
                    {
                        // 创建新的照片记录
                        var retouchedPhoto = new Photo
                        {
                            BookingId = null, // 修图后的照片不关联到预约
                            ImagePath = retouchedPhotoUrl,
                            Title = $"Retouched Photo for Order {orderId}",
                            Description = comment,
                            IsPublic = false, // 默认不公开
                            ClientApproved = false, // 需要客户确认
                            UploadedAt = DateTime.UtcNow
                        };

                        // 添加照片到数据库
                        var createdPhoto = await _photoRepo.CreateAsync(retouchedPhoto);

                        // 更新订单关联修图后照片
                        await _retouchOrderRepo.UpdateRetouchedPhotoIdAsync(orderId, createdPhoto.PhotoId);
                    }

                    // 更新订单状态
                    order.Status = "Completed";
                    order.CompletedAt = DateTime.UtcNow;
                    await _retouchOrderRepo.UpdateAsync(order);

                    // 发送通知给用户
                    var notificationRequest = new NotificationCreateRequest
                    {
                        UserId = order.UserId,
                        Title = "修图已完成",
                        Content = $"您的照片修图已完成，请查看结果。",
                        Type = "Booking"
                    };

                    await _notificationService.CreateNotificationAsync(notificationRequest);

                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        // 新增 - 完成修图订单并上传修图后照片
        public async Task<RetouchOrderDto> CompleteOrderWithPhotoAsync(int orderId, IFormFile photoFile, RetouchOrderCompleteRequest request)
        {
            if (photoFile == null)
                throw new ArgumentNullException(nameof(photoFile), "Retouched photo file is required.");

            var order = await _retouchOrderRepo.GetByIdAsync(orderId);
            if (order == null)
                throw new KeyNotFoundException($"Order with ID {orderId} not found.");

            if (order.Status != "InProgress")
                throw new InvalidOperationException("Only orders in 'InProgress' status can be completed.");

            // 使用事务确保数据一致性
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. 上传修图后的照片
                    string fileName = await _fileStorageService.SaveFileAsync(
                        photoFile,
                        "photos", // 使用photos目录与其他照片保持一致
                        Path.GetRandomFileName() + Path.GetExtension(photoFile.FileName)
                    );

                    // 2. 创建新的照片记录
                    var retouchedPhoto = new Photo
                    {
                        BookingId = null, // 修图后的照片不关联到预约
                        ImagePath = fileName,
                        Title = Path.GetFileNameWithoutExtension(photoFile.FileName),
                        Description = request.Comment,
                        IsPublic = false, // 默认不公开
                        ClientApproved = false, // 需要客户确认
                        UploadedAt = DateTime.UtcNow
                    };

                    // 3. 添加照片到数据库
                    var createdPhoto = await _photoRepo.CreateAsync(retouchedPhoto);

                    // 4. 更新订单关联修图后照片
                    await _retouchOrderRepo.UpdateRetouchedPhotoIdAsync(orderId, createdPhoto.PhotoId);

                    // 5. 尝试生成缩略图
                    try
                    {
                        await _fileStorageService.GenerateThumbnailAsync(fileName);
                    }
                    catch (Exception)
                    {
                        // 生成缩略图失败不影响主流程
                        // 如果有日志系统，可以记录错误
                    }

                    // 6. 发送通知给用户
                    var notificationRequest = new NotificationCreateRequest
                    {
                        UserId = order.UserId,
                        Title = "修图已完成",
                        Content = $"您的照片修图已完成，请查看结果。",
                        Type = "Booking"
                    };

                    await _notificationService.CreateNotificationAsync(notificationRequest);

                    await transaction.CommitAsync();

                    // 返回更新后的订单信息
                    return await GetOrderByIdAsync(orderId);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
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
                // 新增修图后照片的信息
                RetouchedPhotoId = order.RetouchedPhotoId,
                RetouchedPhotoTitle = order.RetouchedPhoto?.Title,
                RetouchedPhotoPath = order.RetouchedPhoto?.ImagePath,
                Status = order.Status,
                Requirements = order.Requirements,
                Price = order.Price,
                CreatedAt = order.CreatedAt,
                CompletedAt = order.CompletedAt
            };
        }
    }
}