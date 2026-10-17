using AutoMapper;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Domain.Interfaces;

namespace LinkUpPro.Core.Application.Services
{
    public class GenericService<TEntity, TReadDto, TCreateDto, TUpdateDto>
        : IGenericService<TReadDto, TCreateDto, TUpdateDto>
        where TEntity : class
        where TReadDto : class
        where TCreateDto : class
        where TUpdateDto : class
    {
        protected readonly IGenericRepository<TEntity> _repository;
        protected readonly IMapper _mapper;

        public GenericService(IGenericRepository<TEntity> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public virtual async Task<TReadDto> AddAsync(TCreateDto dto)
        {
            TEntity entity = _mapper.Map<TEntity>(dto);

            TEntity createdEntity = await _repository.AddAsync(entity);

            return _mapper.Map<TReadDto>(createdEntity);
        }

        public virtual async Task<TReadDto?> UpdateAsync(TUpdateDto dto, params object[] keyValues)
        {
            TEntity? entity = await _repository.GetByIdAsync(keyValues);

            if (entity == null)
            {
                return null;
            }

            _mapper.Map(dto, entity);

            TEntity? updatedEntity = await _repository.UpdateAsync(entity, keyValues);

            return updatedEntity == null ? null : _mapper.Map<TReadDto>(updatedEntity);
        }

        public virtual async Task<bool> DeleteAsync(params object[] keyValues)
        {
            return await _repository.DeleteAsync(keyValues);
        }

        public virtual async Task<TReadDto?> GetByIdAsync(params object[] keyValues)
        {
            TEntity? entity = await _repository.GetByIdAsync(keyValues);

            return entity == null ? null : _mapper.Map<TReadDto>(entity);
        }

        public virtual async Task<List<TReadDto>> GetAllAsync()
        {
            List<TEntity> entities = await _repository.GetAllListAsync();

            return _mapper.Map<List<TReadDto>>(entities);
        }
    }
}
