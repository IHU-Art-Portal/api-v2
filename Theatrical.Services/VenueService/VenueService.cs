using Theatrical.Data.Models;
using Theatrical.Dto.Pagination;
using Theatrical.Dto.ProductionDtos;
using Theatrical.Dto.VenueDtos;
using Theatrical.Services.Curators;
using Theatrical.Services.Pagination;
using Theatrical.Services.Repositories;

namespace Theatrical.Services.VenueService;

public interface IVenueService
{
    Task<Venue> Create(VenueCreateDto venueCreateDto);
    Task Delete(Venue venue);
    Task<VenueResponseDto> Update(Venue venue, VenueUpdateDto venueUpdateDto);
    List<VenueDto> ToDto(List<Venue> venue);
    VenueDto ToDto(Venue venue);
    PaginationResult<VenueDto> Paginate(int? page, int? size, List<VenueDto> venuesDto);
    List<ProductionDto> ProductionsToDto(List<Production> productions);
    Task<List<Venue>> GetVenuesByTitles(List<string> titles);

    Task<(List<Venue> VenuesToUpdate, List<Venue> VenuesToCreate, List<VenueDto> responseList)> CreateUpdateList(
        List<Venue> existingVenues, List<VenueCreateDto> venueCreateDto);

    List<VenueDto> Filtering(List<VenueDto> venuesDto, bool? alphabeticalOrder, string? addressSearch, string? venueTitle, bool? availableForClaim);
}

public class VenueService : IVenueService
{
    private readonly IVenueRepository _repository;
    private readonly IPaginationService _pagination;

    public VenueService(IVenueRepository repository, IPaginationService paginationService)
    {
        _repository = repository;
        _pagination = paginationService;
    }
    
    public async Task<Venue> Create(VenueCreateDto venueCreateDto)
    {
        Venue venue = new Venue
        {
            Title = venueCreateDto.Address,
            Address = venueCreateDto.Address,
            Timestamp = DateTime.UtcNow,
            SystemId = venueCreateDto.SystemId
        }; 
        
        var createdVenue = await _repository.Create(venue);
        return createdVenue;
    }

    public async Task Delete(Venue venue)
    {
        await _repository.Delete(venue);
    }

    public async Task<VenueResponseDto> Update(Venue venue, VenueUpdateDto venueUpdateDto)
    {
        var updatedVenue = await _repository.Update(venue, venueUpdateDto);
        var venueResponse = new VenueResponseDto
        {
            Id = updatedVenue.Id,
            Title = updatedVenue.Title,
            Address = updatedVenue.Address
        };
        return venueResponse;
    }

    public List<VenueDto> ToDto(List<Venue> venues)
    {
        var venuesDtos = new List<VenueDto>();

        foreach (var venue in venues)
        {
            VenueDto venueDto = new VenueDto
            {
                Id = venue.Id,
                Title = venue.Title,
                Address = venue.Address,
                IsClaimed = venue.isClaimed
            };
            venuesDtos.Add(venueDto);
        }
        
        
        return venuesDtos;
    }

    public VenueDto ToDto(Venue venue)
    {
        var venueDto = new VenueDto
        {
            Id = venue.Id,
            Title = venue.Title,
            Address = venue.Address,
            IsClaimed = venue.isClaimed
        };
        
        return venueDto;
    }

    public PaginationResult<VenueDto> Paginate(int? page, int? size, List<VenueDto> venuesDto)
    {
        var result = _pagination.GetPaginated(page, size, venuesDto, items =>
        {
            return items.Select(venue => new VenueDto
            {
                Address = venue.Address,
                Id = venue.Id,
                Title = venue.Title,
                IsClaimed = venue.IsClaimed
            });
        });
        
        return result;
    }

    public List<ProductionDto> ProductionsToDto(List<Production> productions)
    {
        var productionsDto = productions.Select(prod => new ProductionDto
        {
            Id = prod.Id,
            OrganizerId = prod.OrganizerId,
            Title = prod.Title,
            Description = prod.Description,
            Url = prod.Url,
            Producer = prod.Producer,
            MediaUrl = prod.MediaUrl,
            Duration = prod.Duration
        }).ToList();
        
        return productionsDto;
    }
    
    public async Task<List<Venue>> GetVenuesByTitles(List<string> titles)
    {
        return await _repository.GetVenuesByTitles(titles);
    }

    public async Task<(List<Venue> VenuesToUpdate, List<Venue> VenuesToCreate, List<VenueDto> responseList)> CreateUpdateList(List<Venue> existingVenues, List<VenueCreateDto> venueCreateDto)
    {
        var venuesToUpdate = new List<Venue>();
        var venuesToCreate = new List<Venue>();
        var responseList = new List<VenueDto>();

        foreach (var dto in venueCreateDto)
        {
            var existingVenue = existingVenues.FirstOrDefault(v => v.Title == dto.Title);

            if (existingVenue is not null)
            {
                existingVenue.Address = dto.Address;
                existingVenue.Title = dto.Title;
                existingVenue.SystemId = dto.SystemId;
                    
                venuesToUpdate.Add(existingVenue);
            }
            else
            {
                var newVenue = new Venue
                {
                    Title = dto.Title,
                    Address = dto.Address,
                    SystemId = dto.SystemId
                };
                venuesToCreate.Add(newVenue);
            }
        }
        
        //Saves changes for all updated venues
        if (venuesToUpdate.Any())
        {
            var updatedVenues = await _repository.UpdateRange(venuesToUpdate);
            responseList.AddRange(ToDto(updatedVenues));
        }
        
        //Creates venues
        if (venuesToCreate.Any())
        {
            var createdVenues = await _repository.CreateRange(venuesToCreate);
            responseList.AddRange(ToDto(createdVenues));
        }

        return (venuesToUpdate, venuesToCreate, responseList);
    }

    public List<VenueDto> Filtering(List<VenueDto> venuesDto, bool? alphabeticalOrder, string? addressSearch, string? venueTitle,
        bool? availableForClaim)
    {
        if (addressSearch is not null)
        {
            var normalizedAddress = StringUtilities.RemoveDiacritics(addressSearch).ToLowerInvariant();

            venuesDto = venuesDto
                .Where(v => v.Address != null 
                && StringUtilities.RemoveDiacritics(v.Address).ToLowerInvariant().Contains(normalizedAddress))
                .ToList();
        }
            
        if (venueTitle is not null)
        {
           var normalizedTitle = StringUtilities.RemoveDiacritics(venueTitle).ToLowerInvariant();

            venuesDto = venuesDto
                .Where(v => v.Title != null 
                && StringUtilities.RemoveDiacritics(v.Title).ToLowerInvariant().Contains(normalizedTitle))
                .ToList();
        }
            
        if (alphabeticalOrder == true)
        {
            venuesDto = venuesDto
                .OrderBy(v => v.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (availableForClaim == true)
        {
            venuesDto = venuesDto
                .Where(v => v.IsClaimed == true)
                .ToList();
        }

        return venuesDto;
    }
}