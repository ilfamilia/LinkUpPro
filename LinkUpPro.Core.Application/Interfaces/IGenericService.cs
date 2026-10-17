
namespace LinkUpPro.Core.Application.Interfaces
{
    public interface IGenericService<TReadDto, TCreateDto, TUpdateDto>
        where TReadDto : class
        where TCreateDto : class
        where TUpdateDto : class
    {
        Task<TReadDto> AddAsync(TCreateDto dto);
        Task<TReadDto?> UpdateAsync(TUpdateDto dto, params object[] keyValues);
        Task<bool> DeleteAsync(params object[] keyValues);
        Task<TReadDto?> GetByIdAsync(params object[] keyValues);
        Task<List<TReadDto>> GetAllAsync();
    }
}
