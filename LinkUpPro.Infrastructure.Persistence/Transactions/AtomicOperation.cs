using System.Data;
using LinkUpPro.Core.Domain.Exceptions;
using LinkUpPro.Core.Domain.Interfaces;
using LinkUpPro.Infrastructure.Persistence.Contexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Infrastructure.Persistence.Transactions;

/// <summary>Protects multi-record transitions with serializable isolation and safe conflict reporting.</summary>
public sealed class AtomicOperation(LinkUpProAppContext context) : IAtomicOperation
{
    /// <summary>Commits all tracked changes together, rolling back the entire operation on any failure.</summary>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        if (context.Database.CurrentTransaction is not null) return await operation();
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var result = await operation();
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return result;
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            context.ChangeTracker.Clear();
            if (exception is DbUpdateConcurrencyException || IsDatabaseConflict(exception))
                throw new DomainRuleException("Los datos cambiaron durante la operación. Refresque la pantalla e inténtelo nuevamente.");
            throw;
        }
    }

    /// <summary>Recognizes duplicate-key and deadlock conflicts without leaking SQL messages to users.</summary>
    private static bool IsDatabaseConflict(Exception exception)
        => exception is SqlException sql && sql.Number is 1205 or 2601 or 2627
            || exception.InnerException is not null && IsDatabaseConflict(exception.InnerException);
}
