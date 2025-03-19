using Microsoft.EntityFrameworkCore;
using PixelPerfect.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PixelPerfect.DataAccess.Repo;

public interface IPaymentRepo
{
    Task<Payment> CreatePayment(Payment payment);
    Task<Payment?> GetPaymentById(int paymentId);
    Task<List<Payment>> GetPaymentsByUserId(int userId);
    Task<List<Payment>> GetPaymentsByOrder(string orderType, int orderId);
    Task<Payment> UpdatePaymentStatus(int paymentId, string status, string? transactionId = null);
}

public class PaymentRepo : IPaymentRepo
{
    private readonly PhotoBookingDbContext _context;

    public PaymentRepo(PhotoBookingDbContext context)
    {
        _context = context;
    }

    public async Task<Payment> CreatePayment(Payment payment)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task<Payment?> GetPaymentById(int paymentId)
    {
        return await _context.Payments
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
    }

    public async Task<List<Payment>> GetPaymentsByUserId(int userId)
    {
        return await _context.Payments
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Payment>> GetPaymentsByOrder(string orderType, int orderId)
    {
        return await _context.Payments
            .Where(p => p.OrderType == orderType && p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Payment> UpdatePaymentStatus(int paymentId, string status, string? transactionId = null)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment == null)
        {
            throw new Exception($"Payment with ID {paymentId} not found");
        }

        payment.Status = status;
        if (transactionId != null)
        {
            payment.TransactionId = transactionId;
        }
        payment.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return payment;
    }
}