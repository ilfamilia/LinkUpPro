using LinkUpPro.Core.Application.Enums;

namespace LinkUpPro.Core.Application.Results
{
    public class ServiceResultWithData<T> : ServiceResult
    {
        public T? Data { get; }

        private ServiceResultWithData(OperationStatus status, T? data, IReadOnlyList<string> errors)
            : base(status, errors)
        {
            Data = data;
        }

        public static ServiceResultWithData<T> Success(T data)
        {
            ArgumentNullException.ThrowIfNull(data);

            return new ServiceResultWithData<T>(OperationStatus.Success, data, []);
        }

        public new static ServiceResultWithData<T> Failure(OperationStatus status, params string[] errors)
        {
            if (status == OperationStatus.Success)
            {
                throw new ArgumentException("A failure cannot have Success status.");
            }

            return new ServiceResultWithData<T>(status, default, errors);
        }
    }
}