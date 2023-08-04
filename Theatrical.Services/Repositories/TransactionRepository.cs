﻿using Microsoft.EntityFrameworkCore;
using Theatrical.Data.Context;
using Theatrical.Data.Models;

namespace Theatrical.Services.Repositories;

public interface ITransactionRepository
{
    Task<Transaction> PostTransaction(Transaction transaction);
    Task<List<Transaction>> GetTransactions(int userId);
    Task<Transaction?> GetTransaction(int transactionId);
}

public class TransactionRepository : ITransactionRepository
{
    private readonly TheatricalPlaysDbContext _context;

    public TransactionRepository(TheatricalPlaysDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction> PostTransaction(Transaction transaction)
    {
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }
    
    public async Task<List<Transaction>> GetTransactions(int userId)
    {
        var transactions = await _context.Transactions
            .Where(t => t.UserId == userId)
            .ToListAsync();

        return transactions;
    }

    public async Task<Transaction?> GetTransaction(int transactionId)
    {
        var transaction = await _context.Transactions.FindAsync(transactionId);
        return transaction;
    }
}