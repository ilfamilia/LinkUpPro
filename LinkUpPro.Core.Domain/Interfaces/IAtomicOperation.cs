namespace LinkUpPro.Core.Domain.Interfaces;

/// <summary>Commits related repository changes together and rolls them back on failure.</summary>
public interface IAtomicOperation
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation);
}

