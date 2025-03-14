using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HybridCacheSample.Goblins;

public class GoblinMode(int minMilliseconds = 100, int maxMilliseconds = 1000) : DbCommandInterceptor
{
    private readonly Random _random = new();

    public static bool IsEnabled { get; set; } = false;

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        if (IsEnabled) await SlowDownQuery(cancellationToken);
        return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override async ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result,
        CancellationToken cancellationToken = new CancellationToken())
    {
        if (IsEnabled) await SlowDownQuery(cancellationToken);
        
        return await base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        if (IsEnabled) await SlowDownQuery(cancellationToken);
        
        return await base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }
    
    private async ValueTask SlowDownQuery(CancellationToken cancellationToken = default)
    {
        await Task.Delay(_random.Next(minMilliseconds, maxMilliseconds), cancellationToken);
    }
}