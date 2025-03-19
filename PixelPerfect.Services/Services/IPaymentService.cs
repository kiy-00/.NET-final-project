using PixelPerfect.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PixelPerfect.Services.Services;

public interface IPaymentService
{
    Task<PaymentResponse> CreatePayment(int userId, CreatePaymentRequest request);
    Task<PaymentResponse?> GetPaymentById(int paymentId);
    Task<List<PaymentResponse>> GetPaymentsByUserId(int userId);
    Task<List<PaymentResponse>> GetPaymentsByOrder(string orderType, int orderId);
    Task<PaymentResponse> UpdatePaymentStatus(int paymentId, PaymentStatusUpdateRequest request);

    // 在 IPaymentService 接口中添加：
    Task<PaymentStatusResponse> GetOrderPaymentStatus(string orderType, int orderId);
    Task<List<PaymentResponse>> GetPaymentsByUserIdAndStatus(int userId, string status, string orderType = null);
}