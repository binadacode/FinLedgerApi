using System;
using System.ComponentModel.DataAnnotations;

namespace FinLedgerApi.Models;

// 1. Define the Type of Transaction
public enum TransactionType
{
    Debit,  // Increases assets/expenses, decreases liabilities/equity
    Credit  // Decreases assets/expenses, increases liabilities/equity
}

// 2. The Account Model
public class Account
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string AccountType { get; set; } = "Asset"; // e.g., Asset, Liability, Revenue, Expense
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// 3. The Transaction Model
public class Transaction
{
    public int Id { get; set; }

    [Required]
    public int AccountId { get; set; } // Foreign key linking to the Account

    [Required]
    public decimal Amount { get; set; } // Financial amounts always use 'decimal', never 'float' or 'double'

    [Required]
    public TransactionType Type { get; set; } // Debit or Credit

    [StringLength(255)]
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}