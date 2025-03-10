using PixelPerfect.Models;

namespace PixelPerfect.Services
{
    public interface IRetouchOrderService
    {
        // 获取修图订单
        Task<RetouchOrderDto> GetOrderByIdAsync(int orderId);
        Task<List<RetouchOrderDto>> GetOrdersByUserIdAsync(int userId);
        Task<List<RetouchOrderDto>> GetOrdersByRetoucherIdAsync(int retoucherId);
        Task<List<RetouchOrderDto>> GetOrdersByPhotoIdAsync(int photoId);
        Task<List<RetouchOrderDto>> SearchOrdersAsync(RetouchOrderSearchParams searchParams);

        // 创建修图订单
        Task<RetouchOrderDto> CreateOrderAsync(int userId, RetouchOrderCreateRequest request);

        // 更新修图订单
        Task<bool> UpdateOrderStatusAsync(int orderId, string status);
        Task<bool> CompleteOrderAsync(int orderId);

        // 权限检查
        Task<bool> IsUserOrderAsync(int orderId, int userId);
        Task<bool> IsRetoucherOrderAsync(int orderId, int retoucherId);
    }
}