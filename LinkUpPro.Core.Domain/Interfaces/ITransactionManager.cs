namespace LinkUpPro.Core.Domain.Interfaces
{
    public interface ITransactionManager
    {
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
