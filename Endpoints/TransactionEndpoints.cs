using FinLedgerApi.Data;
using FinLedgerApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FinLedgerApi.Endpoints;

public static class TransactionEndpoints
{
    public static void MapTransactionEndpoints(this WebApplication app)
    {
        app.MapPost("/transactions", async (CreateTransactionRequest request, LedgerDbContext db) =>
        {
            var account = await db.Accounts.FindAsync(request.AccountId);
            if (account is null)
                return Results.NotFound($"Account {request.AccountId} not found.");

            if (request.Amount <= 0)
                return Results.BadRequest("Amount must be greater than zero.");

            if (!Enum.IsDefined(request.Type))
                return Results.BadRequest("Transaction type must be Debit or Credit.");

            var transaction = new Transaction
            {
                AccountId = request.AccountId,
                Amount = request.Amount,
                Type = request.Type,
                Description = request.Description?.Trim() ?? string.Empty
            };

            db.Transactions.Add(transaction);
            await db.SaveChangesAsync();

            return Results.Created($"/transactions/{transaction.Id}", new TransactionResponse(
                transaction.Id,
                transaction.AccountId,
                transaction.Amount,
                transaction.Type,
                transaction.Description,
                transaction.CreatedAt));
        });
    }
}

public record CreateTransactionRequest(int AccountId, decimal Amount, TransactionType Type, string? Description);
public record TransactionResponse(int Id, int AccountId, decimal Amount, TransactionType Type, string Description, DateTime CreatedAt);
