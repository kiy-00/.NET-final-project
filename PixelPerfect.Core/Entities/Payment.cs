using System;
using System.Collections.Generic;

namespace PixelPerfect.Core.Entities;

public partial class Payment
{
    public int PaymentId { get; set; }
    public int UserId { get; set; }
    public string OrderType { get; set; } = null!; // "Booking" 或 "RetouchOrder"
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = null!; // "Wechat", "Alipay", "BankTransfer", "CreditCard"
    public string? TransactionId { get; set; }
    public string Status { get; set; } = null!; // "Pending", "Completed", "Failed", "Refunded"
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}