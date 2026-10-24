
using LinkUpPro.Core.Application.Results;

namespace LinkUpPro.Core.Application.Interfaces
{
    public interface IGenericService<TReadDto, TCreateDto, TUpdateDto>
        where TReadDto : class
        where TCreateDto : class
        where TUpdateDto : class
    {
        Task<ServiceResultWithData<TReadDto>> AddAsync(TCreateDto dto);

        Task<ServiceResultWithData<TReadDto>> UpdateAsync(TUpdateDto dto, params object[] keyValues);

        Task<ServiceResult> DeleteAsync(params object[] keyValues);

        Task<ServiceResultWithData<TReadDto>> GetByIdAsync(params object[] keyValues);

        Task<ServiceResultWithData<List<TReadDto>>> GetAllAsync();
    }
}
