using FinLedgerApi.Data;
using FinLedgerApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FinLedgerApi.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/accounts");

        group.MapPost("/", async (CreateAccountRequest request, LedgerDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest("Account name is required.");

            var account = new Account
            {
                Name = request.Name.Trim(),
                AccountType = string.IsNullOrWhiteSpace(request.AccountType)
                    ? "Asset"
                    : request.AccountType.Trim()
            };

            db.Accounts.Add(account);
            await db.SaveChangesAsync();

            return Results.Created($"/accounts/{account.Id}", new AccountResponse(
                account.Id, account.Name, account.AccountType, account.CreatedAt));
        });

        group.MapGet("/", async (LedgerDbContext db) =>
        {
            var summaries = await db.Accounts
                .Select(a => new AccountSummary(
                    a.Id,
                    a.Name,
                    a.AccountType,
                    a.CreatedAt,
                    db.Transactions
                        .Where(t => t.AccountId == a.Id)
                        .Sum(t => t.Type == TransactionType.Credit ? t.Amount : -t.Amount)))
                .ToListAsync();

            return Results.Ok(summaries);
        });

        group.MapGet("/{id:int}/statement", async (int id, LedgerDbContext db) =>
        {
            var account = await db.Accounts.FindAsync(id);
            if (account is null)
                return Results.NotFound($"Account {id} not found.");

            var transactions = await db.Transactions
                .Where(t => t.AccountId == id)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();

            decimal runningBalance = 0;
            var entries = transactions.Select(t =>
            {
                runningBalance += t.Type == TransactionType.Credit ? t.Amount : -t.Amount;
                return new StatementEntry(
                    t.Id, t.Amount, t.Type, t.Description, t.CreatedAt, runningBalance);
            }).ToList();

            return Results.Ok(new AccountStatement(
                account.Id, account.Name, account.AccountType, entries));
        });
    }
}

public record CreateAccountRequest(string Name, string? AccountType);
public record AccountResponse(int Id, string Name, string AccountType, DateTime CreatedAt);
public record AccountSummary(int Id, string Name, string AccountType, DateTime CreatedAt, decimal NetBalance);
public record StatementEntry(int Id, decimal Amount, TransactionType Type, string Description, DateTime CreatedAt, decimal RunningBalance);
public record AccountStatement(int Id, string Name, string AccountType, List<StatementEntry> Entries);
