# SPEC — FinLedgerApi

## Overview
A lightweight double-entry-inspired micro-ledger REST API built on .NET 9 Minimal APIs with EF Core In-Memory provider. Manages accounts, posts debit/credit transactions, computes net balances, and generates chronological account statements.

## Tech Stack
- Language: C# 13 / .NET 9
- Framework: ASP.NET Core Minimal APIs
- Database: EF Core InMemory provider
- Testing: xUnit + WebApplicationFactory
- Other: System.ComponentModel.DataAnnotations for validation

## Architecture
```
D:\projects\net\
├── FinLedgerApi.csproj          # Project file, .NET 9, package refs
├── Program.cs                   # DI config, endpoint mapping, app bootstrap
├── Models.cs                    # Account, Transaction, TransactionType (exists)
├── Data/
│   └── LedgerDbContext.cs       # EF Core DbContext
├── Endpoints/
│   ├── AccountEndpoints.cs      # POST /accounts, GET /accounts, GET /accounts/{id}/statement
│   └── TransactionEndpoints.cs  # POST /transactions
└── Tests/
    └── FinLedgerApi.Tests/
        ├── FinLedgerApi.Tests.csproj
        └── LedgerTests.cs       # Integration tests via WebApplicationFactory
```

## Data Models (existing Models.cs — no changes)
- **Account**: Id, Name (required, max 100), AccountType (default "Asset"), CreatedAt
- **Transaction**: Id, AccountId (FK), Amount (decimal), Type (Debit/Credit), Description (max 255), CreatedAt
- **TransactionType**: enum { Debit, Credit }

## API Endpoints

### POST /accounts
- Body: `{ "name": "Cash", "accountType": "Asset" }`
- Returns: 201 Created + Account object
- Validation: name required, non-empty

### GET /accounts
- Returns: 200 + array of AccountSummary objects
- Each summary includes: Account fields + NetBalance (sum of credits minus debits)

### POST /transactions
- Body: `{ "accountId": 1, "amount": 100.00, "type": "Debit", "description": "..." }`
- Returns: 201 Created + Transaction object
- Validation: accountId must exist, amount > 0, type required

### GET /accounts/{id}/statement
- Returns: 200 + AccountStatement object
- Contains: account info, transactions ordered by CreatedAt ascending, running balance

## Feature Checklist
- [ ] Project file (.csproj) targeting net9.0
- [ ] LedgerDbContext with Account/Transaction DbSets
- [ ] POST /accounts endpoint
- [ ] GET /accounts endpoint with net balance aggregation
- [ ] POST /transactions endpoint with validation
- [ ] GET /accounts/{id}/statement endpoint
- [ ] Integration test suite (xUnit + WebApplicationFactory)

## Test Plan
- Create account → verify 201 + returned fields
- Create duplicate/invalid account → verify 400
- Post transaction to valid account → verify 201
- Post transaction to nonexistent account → verify 404
- Post transaction with amount <= 0 → verify 400
- GET /accounts → verify net balance computation (credit - debit)
- GET /accounts/{id}/statement → verify chronological order + running balance
