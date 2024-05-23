using Theatrical.Data.Models;
using Theatrical.Dto.Pagination;
using Theatrical.Dto.ProductionDtos;
using Theatrical.Services.Pagination;
using Theatrical.Services.Repositories;

namespace Theatrical.Services.ProductionService;

public interface IProductionService
{
    Task<ProductionDto> Create(CreateProductionDto createProductionDto);
    List<ProductionDto> ConvertToDto(List<Production> productions);
    ProductionDto ToDto(Production production);
    Task Delete(Production production);
    PaginationResult<ProductionDto> Paginate(int? page, int? size, ProductionSearchFilters searchFilters, List<Production> productions);
    Task<List<Production>> GetProductionsByTitles(List<string> productionsTitles);
    Task<(List<Production> updatedProductions, List<Production> createdProductions, List<ShortenedProductionDto> responseList)> UpdateOrCreateAndProduceResponseList(List<Production> existingProductions, List<CreateProductionDto> createProductionDtos);
}
public class ProductionService : IProductionService
{
    private readonly IProductionRepository _repo;
    private readonly IPaginationService _pagination;

    private readonly IProductionFilteringMethods _filtering;

    public ProductionService(IProductionRepository repo, IPaginationService paginationService, IProductionFilteringMethods filteringMethods)
    {
        _repo = repo;
        _pagination = paginationService;
        _filtering = filteringMethods;
    }

    public ProductionDto ToDto(Production production)
    {
        var productionDto = new ProductionDto
        {
            Id = production.Id,
            OrganizerId = (int)production.OrganizerId,
            Title = production.Title,
            Description = production.Description,
            Url = production.Url,
            Producer = production.Producer,
            MediaUrl = production.MediaUrl,
            Duration = production.Duration
        };

        return productionDto;
    }
    
    public async Task<ProductionDto> Create(CreateProductionDto createProductionDto)
    {
        var production = new Production
        {
            OrganizerId = createProductionDto.OrganizerId,
            Title = createProductionDto.Title,
            Description = createProductionDto.Description,
            Url = createProductionDto.Url,
            Producer = createProductionDto.Producer,
            MediaUrl = createProductionDto.MediaUrl,
            Duration = createProductionDto.Duration,
            Timestamp = DateTime.UtcNow,
            SystemId = createProductionDto.SystemId
        };

        await _repo.Create(production);
        
        var productionDto = new ProductionDto
        {
            Id = production.Id,
            Description = production.Description,
            Duration = production.Duration,
            MediaUrl = production.MediaUrl,
            OrganizerId = (int)production.OrganizerId,
            Producer = production.Producer,
            Title = production.Title,
            Url = production.Url
        };
        
        return productionDto;
    }

    public async Task Delete(Production production)
    {
        await _repo.Delete(production);
    }
    
    public List<ProductionDto> ConvertToDto(List<Production> productions)
    {
        var productionsDto = new List<ProductionDto>();

        foreach (var production in productions)
        {
            var productionDto = new ProductionDto
            {
                Id = production.Id,
                Description = production.Description,
                Duration = production.Duration,
                MediaUrl = production.MediaUrl,
                OrganizerId = (int)production.OrganizerId,
                Producer = production.Producer,
                Title = production.Title,
                Url = production.Url
            };
            productionsDto.Add(productionDto);
        }

        return productionsDto;
    }

    public PaginationResult<ProductionDto> Paginate(int? page, int? size, ProductionSearchFilters searchFilters, List<Production> productions)
    {
        var titleFiltered = _filtering.TitleFiltering(productions, searchFilters.Title);
        var producerFiltered = _filtering.ProducerFiltering(productions, searchFilters.Producer);

        var productionDtos = ConvertToDto(producerFiltered);

        var paginationResult = _pagination.GetPaginated(page, size, productionDtos, items =>
        {
            return items.Select(prod => new ProductionDto
            {
                Id = prod.Id,
                OrganizerId = prod.OrganizerId,
                Title = prod.Title,
                Description = prod.Description,
                Url = prod.Url,
                Producer = prod.Producer,
                MediaUrl = prod.MediaUrl,
                Duration = prod.Duration
            });
        });

        return paginationResult;
    }

    public async Task<List<Production>> GetProductionsByTitles(List<string> productionsTitles)
    {
        return await _repo.GetProductionsByTitles(productionsTitles);
    }

    public async Task<(List<Production> updatedProductions, List<Production> createdProductions, List<ShortenedProductionDto> responseList)> UpdateOrCreateAndProduceResponseList(List<Production> existingProductions, List<CreateProductionDto> createProductionDtos)
    {
        var productionsToUpdate = new List<Production>();
        var productionsToCreate = new List<Production>();
        var responseList = new List<ShortenedProductionDto>();

        foreach (var dto in createProductionDtos)
        {
            var existingProduction = existingProductions.FirstOrDefault(p => p.Title == dto.Title);

            if (existingProduction is not null)
            {
                existingProduction.OrganizerId = dto.OrganizerId;
                existingProduction.Title = dto.Title;
                existingProduction.Description = dto.Description;
                existingProduction.Url = dto.Url;
                existingProduction.Producer = dto.Producer;
                existingProduction.MediaUrl = dto.MediaUrl;
                existingProduction.Duration = dto.Duration;
                existingProduction.SystemId = dto.SystemId;
                
                productionsToUpdate.Add(existingProduction);
            }
            else
            {
                var newProduction = new Production
                {
                    OrganizerId = dto.OrganizerId,
                    Title = dto.Title,
                    Description = dto.Description,
                    Url = dto.Url,
                    Producer = dto.Producer,
                    MediaUrl = dto.MediaUrl,
                    Duration = dto.Duration,
                    SystemId = dto.SystemId,
                };
                productionsToCreate.Add(newProduction);
            }
        }

        if (productionsToUpdate.Any())
        {
            var updatedProductions = await _repo.UpdateRange(productionsToUpdate);
            responseList.AddRange(ToShortenedDto(updatedProductions));
        }

        if (productionsToCreate.Any())
        {
            var createdProductions = await _repo.CreateRange(productionsToCreate);
            responseList.AddRange(ToShortenedDto(createdProductions));
        }
        
        return (productionsToUpdate, productionsToCreate, responseList);
    }

    private List<ShortenedProductionDto> ToShortenedDto(List<Production> productions)
    {
        return productions.Select(prod => new ShortenedProductionDto
        {
            Id = prod.Id,
            Title = prod.Title,
            OrganizerId = prod.OrganizerId
        }).ToList();
    }
}

