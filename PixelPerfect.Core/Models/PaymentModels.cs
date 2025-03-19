using System;
using System.ComponentModel.DataAnnotations;

namespace PixelPerfect.Core.Models;

public class CreatePaymentRequest
{
    [Required]
    public string OrderType { get; set; } = null!; // "Booking" 或 "RetouchOrder"

    [Required]
    public int OrderId { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = null!;
}

public class PaymentResponse
{
    public int PaymentId { get; set; }
    public int UserId { get; set; }
    public string OrderType { get; set; } = null!;
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PaymentStatusUpdateRequest
{
    [Required]
    public string Status { get; set; } = null!;

    public string? TransactionId { get; set; }
}

public class PaymentSummaryResponse
{
    public int PaymentId { get; set; }
    public string OrderType { get; set; } = null!;
    public int OrderId { get; set; }
    public string OrderDescription { get; set; } = null!;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class PaymentStatusResponse
{
    public string Status { get; set; }
    public bool IsPaid { get; set; }
    public decimal Amount { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string PaymentMethod { get; set; }
}