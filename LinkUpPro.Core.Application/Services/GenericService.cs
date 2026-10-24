using AutoMapper;
using LinkUpPro.Core.Application.Enums;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.Results;
using LinkUpPro.Core.Domain.Interfaces;

namespace LinkUpPro.Core.Application.Services
{
    public class GenericService<TEntity, TReadDto, TCreateDto, TUpdateDto>
        : BaseService, IGenericService<TReadDto, TCreateDto, TUpdateDto>
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

        public virtual async Task<ServiceResultWithData<TReadDto>> AddAsync(TCreateDto dto)
        {
            return await ExecuteAsync<TReadDto>(async () =>
            {
                TEntity entity = _mapper.Map<TEntity>(dto);

                TEntity createdEntity = await _repository.AddAsync(entity);

                TReadDto readDto = _mapper.Map<TReadDto>(createdEntity);

                return ServiceResultWithData<TReadDto>.Success(readDto);
            });
        }

        public virtual async Task<ServiceResultWithData<TReadDto>> UpdateAsync(TUpdateDto dto, params object[] keyValues)
        {
            return await ExecuteAsync<TReadDto>(async () =>
            {
                TEntity? entity = await _repository.GetByIdAsync(keyValues);

                if (entity == null)
                {
                    return ServiceResultWithData<TReadDto>.Failure(OperationStatus.NotFound, "The requested record was not found.");
                }

                _mapper.Map(dto, entity);

                TEntity? updatedEntity = await _repository.UpdateAsync(entity, keyValues);

                if (updatedEntity == null)
                {
                    return ServiceResultWithData<TReadDto>.Failure(OperationStatus.NotFound, "The requested record was not found.");
                }

                TReadDto readDto = _mapper.Map<TReadDto>(updatedEntity);

                return ServiceResultWithData<TReadDto>.Success(readDto);
            });
        }

        public virtual async Task<ServiceResult> DeleteAsync(params object[] keyValues)
        {
            return await ExecuteAsync(async () =>
            {
                bool deleted = await _repository.DeleteAsync(keyValues);

                if (!deleted)
                {
                    return ServiceResult.Failure(OperationStatus.NotFound, "The requested record was not found.");
                }

                return ServiceResult.Success();
            });
        }

        public virtual async Task<ServiceResultWithData<TReadDto>> GetByIdAsync(params object[] keyValues)
        {
            return await ExecuteAsync<TReadDto>(async () =>
            {
                TEntity? entity = await _repository.GetByIdAsync(keyValues);

                if (entity == null)
                {
                    return ServiceResultWithData<TReadDto>.Failure(OperationStatus.NotFound, "The requested record was not found.");
                }

                TReadDto readDto = _mapper.Map<TReadDto>(entity);

                return ServiceResultWithData<TReadDto>.Success(readDto);
            });
        }

        public virtual async Task<ServiceResultWithData<List<TReadDto>>> GetAllAsync()
        {
            return await ExecuteAsync<List<TReadDto>>(async () =>
            {
                List<TEntity> entities = await _repository.GetAllListAsync();

                List<TReadDto> readDtos = _mapper.Map<List<TReadDto>>(entities);

                return ServiceResultWithData<List<TReadDto>>.Success(readDtos);
            });
        }
    }
}
