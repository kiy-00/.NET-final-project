using Microsoft.EntityFrameworkCore;
using PixelPerfect.Core.Entities;
using PixelPerfect.Core.Models;
using PixelPerfect.DataAccess.Repo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PixelPerfect.Services.Services.Impl;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepo _paymentRepo;
    private readonly PhotoBookingDbContext _context;
    private readonly INotificationService _notificationService;

    public PaymentService(IPaymentRepo paymentRepo, PhotoBookingDbContext context, INotificationService notificationService)
    {
        _paymentRepo = paymentRepo;
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<PaymentResponse> CreatePayment(int userId, CreatePaymentRequest request)
    {
        // 获取订单信息和金额
        decimal amount = 0;
        if (request.OrderType == "Booking")
        {
            var booking = await _context.Bookings.FindAsync(request.OrderId);
            if (booking == null)
            {
                throw new Exception("Booking not found");
            }
            if (booking.PaymentStatus != "Unpaid")
            {
                throw new Exception("This booking has already been paid");
            }
            amount = booking.InitialAmount; // 一次性付清全部金额
        }
        else if (request.OrderType == "RetouchOrder")
        {
            var order = await _context.Retouchorders.FindAsync(request.OrderId);
            if (order == null)
            {
                throw new Exception("Retouch order not found");
            }
            if (order.PaymentStatus != "Unpaid")
            {
                throw new Exception("This retouch order has already been paid");
            }
            amount = order.Price;
        }
        else
        {
            throw new Exception("Invalid order type");
        }

        // 创建支付记录
        var payment = new Payment
        {
            UserId = userId,
            OrderType = request.OrderType,
            OrderId = request.OrderId,
            Amount = amount,
            PaymentMethod = request.PaymentMethod,
            Status = "Pending",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        var createdPayment = await _paymentRepo.CreatePayment(payment);

        return MapToPaymentResponse(createdPayment);
    }

    public async Task<PaymentResponse?> GetPaymentById(int paymentId)
    {
        var payment = await _paymentRepo.GetPaymentById(paymentId);
        return payment != null ? MapToPaymentResponse(payment) : null;
    }

    public async Task<List<PaymentResponse>> GetPaymentsByUserId(int userId)
    {
        var payments = await _paymentRepo.GetPaymentsByUserId(userId);
        return payments.Select(MapToPaymentResponse).ToList();
    }

    public async Task<List<PaymentResponse>> GetPaymentsByOrder(string orderType, int orderId)
    {
        var payments = await _paymentRepo.GetPaymentsByOrder(orderType, orderId);
        return payments.Select(MapToPaymentResponse).ToList();
    }

    public async Task<PaymentResponse> UpdatePaymentStatus(int paymentId, PaymentStatusUpdateRequest request)
    {
        var payment = await _paymentRepo.GetPaymentById(paymentId);
        if (payment == null)
        {
            throw new Exception("Payment not found");
        }

        // 更新支付状态
        var updatedPayment = await _paymentRepo.UpdatePaymentStatus(paymentId, request.Status, request.TransactionId);

        // 如果支付完成，更新订单状态
        if (request.Status == "Completed")
        {
            await UpdateOrderPaymentStatus(payment);

            // 发送通知
            if (payment.OrderType == "Booking")
            {
                var booking = await _context.Bookings.FindAsync(payment.OrderId);
                if (booking != null)
                {
                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        UserId = booking.PhotographerId,
                        Title = "收到新的支付",
                        Content = $"您收到了预约订单 #{payment.OrderId} 的付款，金额：¥{payment.Amount}",
                        Type = "Payment"
                    });
                }
            }
            else if (payment.OrderType == "RetouchOrder")
            {
                var order = await _context.Retouchorders.FindAsync(payment.OrderId);
                if (order != null)
                {
                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        UserId = order.RetoucherId,
                        Title = "收到新的支付",
                        Content = $"您收到了修图订单 #{payment.OrderId} 的付款，金额：¥{payment.Amount}",
                        Type = "Payment"
                    });
                }
            }
        }
        else if (request.Status == "Refunded")
        {
            await UpdateOrderRefundStatus(payment);
        }

        return MapToPaymentResponse(updatedPayment);
    }

    private async Task UpdateOrderPaymentStatus(Payment payment)
    {
        if (payment.OrderType == "Booking")
        {
            var booking = await _context.Bookings.FindAsync(payment.OrderId);
            if (booking != null)
            {
                booking.PaymentStatus = "Paid";
                await _context.SaveChangesAsync();
            }
        }
        else if (payment.OrderType == "RetouchOrder")
        {
            var order = await _context.Retouchorders.FindAsync(payment.OrderId);
            if (order != null)
            {
                order.PaymentStatus = "Paid";
                await _context.SaveChangesAsync();
            }
        }
    }

    private async Task UpdateOrderRefundStatus(Payment payment)
    {
        if (payment.OrderType == "Booking")
        {
            var booking = await _context.Bookings.FindAsync(payment.OrderId);
            if (booking != null)
            {
                booking.PaymentStatus = "Refunded";
                await _context.SaveChangesAsync();
            }
        }
        else if (payment.OrderType == "RetouchOrder")
        {
            var order = await _context.Retouchorders.FindAsync(payment.OrderId);
            if (order != null)
            {
                order.PaymentStatus = "Refunded";
                await _context.SaveChangesAsync();
            }
        }
    }

    public async Task<PaymentStatusResponse> GetOrderPaymentStatus(string orderType, int orderId)
    {
        // 获取订单的所有支付记录，按创建时间降序排列（最新的排在前面）
        var payments = await _paymentRepo.GetPaymentsByOrder(orderType, orderId);

        if (payments == null || !payments.Any())
        {
            return new PaymentStatusResponse
            {
                Status = "None",
                IsPaid = false,
                Amount = 0,
                PaymentDate = null,
                PaymentMethod = null
            };
        }

        // 获取最新的支付记录
        var latestPayment = payments.OrderByDescending(p => p.CreatedAt).First();

        // 检查支付状态
        bool isPaid = latestPayment.Status == "Completed";

        // 获取支付时间（如果已支付）
        DateTime? paymentDate = latestPayment.Status == "Completed" ?
            latestPayment.UpdatedAt : null;

        return new PaymentStatusResponse
        {
            Status = latestPayment.Status,
            IsPaid = isPaid,
            Amount = latestPayment.Amount,
            PaymentDate = paymentDate,
            PaymentMethod = latestPayment.PaymentMethod
        };
    }

    public async Task<List<PaymentResponse>> GetPaymentsByUserIdAndStatus(int userId, string status, string orderType = null)
    {
        // 首先获取用户的所有支付记录
        var allUserPayments = await _paymentRepo.GetPaymentsByUserId(userId);

        // 过滤符合条件的支付记录
        var filteredPayments = allUserPayments
            .Where(p => p.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
            .Where(p => orderType == null || p.OrderType.Equals(orderType, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // 将实体转换为响应模型
        return filteredPayments.Select(MapToPaymentResponse).ToList();
    }

    private PaymentResponse MapToPaymentResponse(Payment payment)
    {
        return new PaymentResponse
        {
            PaymentId = payment.PaymentId,
            UserId = payment.UserId,
            OrderType = payment.OrderType,
            OrderId = payment.OrderId,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            Status = payment.Status,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };
    }
}