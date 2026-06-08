using FinLedgerApi.Data;
using FinLedgerApi.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LedgerDbContext>(options =>
    options.UseInMemoryDatabase("FinLedger"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
    db.Database.EnsureCreated();
}

app.MapAccountEndpoints();
app.MapTransactionEndpoints();

app.Run();

public partial class Program { }
