using System.Net;
using System.Net.Http.Json;
using FinLedgerApi.Endpoints;
using FinLedgerApi.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FinLedgerApi.Tests;

public class LedgerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public LedgerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateAccount_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/accounts", new { name = "Cash", accountType = "Asset" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var account = await response.Content.ReadFromJsonAsync<AccountResponse>();
        Assert.NotNull(account);
        Assert.Equal("Cash", account.Name);
        Assert.Equal("Asset", account.AccountType);
    }

    [Fact]
    public async Task CreateAccount_EmptyName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/accounts", new { name = "", accountType = "Asset" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostTransaction_Returns201()
    {
        var accountResp = await _client.PostAsJsonAsync("/accounts", new { name = "Savings", accountType = "Asset" });
        var account = await accountResp.Content.ReadFromJsonAsync<AccountResponse>();

        var txResp = await _client.PostAsJsonAsync("/transactions", new
        {
            accountId = account!.Id,
            amount = 250.00m,
            type = TransactionType.Credit,
            description = "Initial deposit"
        });

        Assert.Equal(HttpStatusCode.Created, txResp.StatusCode);
        var tx = await txResp.Content.ReadFromJsonAsync<TransactionResponse>();
        Assert.NotNull(tx);
        Assert.Equal(250.00m, tx.Amount);
        Assert.Equal(TransactionType.Credit, tx.Type);
    }

    [Fact]
    public async Task PostTransaction_NonexistentAccount_Returns404()
    {
        var resp = await _client.PostAsJsonAsync("/transactions", new
        {
            accountId = 9999,
            amount = 100m,
            type = TransactionType.Debit,
            description = "Ghost"
        });

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task PostTransaction_ZeroAmount_Returns400()
    {
        var accountResp = await _client.PostAsJsonAsync("/accounts", new { name = "Test", accountType = "Asset" });
        var account = await accountResp.Content.ReadFromJsonAsync<AccountResponse>();

        var resp = await _client.PostAsJsonAsync("/transactions", new
        {
            accountId = account!.Id,
            amount = 0m,
            type = TransactionType.Credit,
            description = "Zero"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetAccounts_ReturnsNetBalance()
    {
        var accResp = await _client.PostAsJsonAsync("/accounts", new { name = "LedgerTest", accountType = "Asset" });
        var account = await accResp.Content.ReadFromJsonAsync<AccountResponse>();

        await _client.PostAsJsonAsync("/transactions", new
        {
            accountId = account!.Id, amount = 500m, type = TransactionType.Credit, description = "Deposit"
        });
        await _client.PostAsJsonAsync("/transactions", new
        {
            accountId = account.Id, amount = 150m, type = TransactionType.Debit, description = "Withdrawal"
        });

        var accounts = await _client.GetFromJsonAsync<List<AccountSummary>>("/accounts");
        Assert.NotNull(accounts);

        var match = accounts.FirstOrDefault(a => a.Id == account.Id);
        Assert.NotNull(match);
        Assert.Equal(350m, match.NetBalance);
    }

    [Fact]
    public async Task GetStatement_ReturnsChronologicalOrder()
    {
        var accResp = await _client.PostAsJsonAsync("/accounts", new { name = "StatementTest", accountType = "Revenue" });
        var account = await accResp.Content.ReadFromJsonAsync<AccountResponse>();

        await _client.PostAsJsonAsync("/transactions", new
        {
            accountId = account!.Id, amount = 100m, type = TransactionType.Credit, description = "First"
        });
        await _client.PostAsJsonAsync("/transactions", new
        {
            accountId = account.Id, amount = 30m, type = TransactionType.Debit, description = "Second"
        });

        var statement = await _client.GetFromJsonAsync<AccountStatement>($"/accounts/{account.Id}/statement");
        Assert.NotNull(statement);
        Assert.Equal(2, statement.Entries.Count);
        Assert.Equal("First", statement.Entries[0].Description);
        Assert.Equal(100m, statement.Entries[0].RunningBalance);
        Assert.Equal("Second", statement.Entries[1].Description);
        Assert.Equal(70m, statement.Entries[1].RunningBalance);
    }

    [Fact]
    public async Task GetStatement_NonexistentAccount_Returns404()
    {
        var resp = await _client.GetAsync("/accounts/9999/statement");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
