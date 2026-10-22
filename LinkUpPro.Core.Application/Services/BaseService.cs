using LinkUpPro.Core.Application.Enums;
using LinkUpPro.Core.Application.Results;

namespace LinkUpPro.Core.Application.Services
{
    public abstract class BaseService
    {
        protected async Task<ServiceResult> ExecuteAsync(Func<Task<ServiceResult>> operation)
        {
            try
            {
                return await operation();
            }
            catch (Exception)
            {
                return ServiceResult.Failure(OperationStatus.Failed);
            }
        }

        protected async Task<ServiceResultWithData<T>> ExecuteAsync<T>(Func<Task<ServiceResultWithData<T>>> operation)
        {
            try
            {
                return await operation();
            }
            catch (Exception)
            {
                return ServiceResultWithData<T>.Failure(OperationStatus.Failed);
            }
        }
    }
}