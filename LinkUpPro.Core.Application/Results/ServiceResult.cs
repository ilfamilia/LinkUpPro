
using LinkUpPro.Core.Application.Enums;

namespace LinkUpPro.Core.Application.Results
{
    public class ServiceResult
    {
        public OperationStatus Status { get; }

        public bool IsSuccess => Status == OperationStatus.Success;

        public IReadOnlyList<string> Errors { get; }

        protected ServiceResult(OperationStatus status, IReadOnlyList<string> errors)
        {
            Status = status;
            Errors = errors;
        }

        public static ServiceResult Success()
        {
            return new ServiceResult(OperationStatus.Success, []);
        }

        public static ServiceResult Failure(OperationStatus status, params string[] errors)
        {
            if (status == OperationStatus.Success)
            {
                throw new ArgumentException("A failure cannot have Success status.");
            }

            return new ServiceResult(status, errors);
        }
    }
}
