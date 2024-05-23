using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Theatrical.Data.enums;
using Theatrical.Data.Models;
using Theatrical.Dto.Pagination;
using Theatrical.Dto.ProductionDtos;
using Theatrical.Dto.ResponseWrapperFolder;
using Theatrical.Dto.VenueDtos;
using Theatrical.Services;
using Theatrical.Services.ProductionService;
using Theatrical.Services.Security.AuthorizationFilters;
using Theatrical.Services.Validation;

namespace Theatrical.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors("AllowOrigin")]
public class VenuesController : ControllerBase
{
    private readonly IVenueService _service;
    private readonly IVenueValidationService _validation;
    private readonly IProductionService _productionService;
    private readonly IUserVenueService _userVenueService;

    public VenuesController(IVenueService service, IVenueValidationService validation, IProductionService productionService,
        IUserVenueService userVenueService)
    {
        _service = service;
        _validation = validation;
        _productionService = productionService;
        _userVenueService = userVenueService;
    }

    /// <summary>
    /// Endpoint to all venues.
    /// Customizable with page and size.
    /// Search by titles. Order by alphabetical order.
    /// </summary>
    /// <param name="page">page number</param>
    /// <param name="size">size capacity for a page</param>
    /// <param name="alphabeticalOrder"></param>
    /// <param name="addressSearch"></param>
    /// <param name="venueTitle"></param>
    /// <param name="availableForClaim"></param>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginationResult<VenueDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> GetVenues(int? page, int? size, bool? alphabeticalOrder, string? addressSearch,
         string? venueTitle, bool? availableForClaim)
    {
        try
        {
            var (validation, venues) = await _validation.ValidateAndFetch();

            if (!validation.Success)
            {
                var errorResponse = new ApiResponse(ErrorCode.NotFound, validation.Message!);
                return new NotFoundObjectResult(errorResponse);
            }

            var venuesDto = _service.ToDto(venues!);

            venuesDto = _service.Filtering(venuesDto, alphabeticalOrder, addressSearch, venueTitle, availableForClaim);

            var paginationResult = _service.Paginate(page, size, venuesDto);

            var response = new ApiResponse<PaginationResult<VenueDto>>(paginationResult);

            return new OkObjectResult(response);
        }
        catch (Exception e)
        {
            var unexpectedResponse = new ApiResponse(ErrorCode.ServerError, e.Message);

            return new ObjectResult(unexpectedResponse){StatusCode = StatusCodes.Status500InternalServerError};
        }
    }

    /// <summary>
    /// Endpoint to specific Venue.
    /// Search a Venue by its Id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(VenueDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> GetVenue(int id)
    {
        try
        {
            var (validation, venue) = await _validation.ValidateAndFetch(id);

            if (!validation.Success)
            {
                var errorResponse = new ApiResponse(ErrorCode.NotFound, validation.Message!);
                return new NotFoundObjectResult(errorResponse);
            }

            var venueDto = _service.ToDto(venue!);

            var response = new ApiResponse<VenueDto>(venueDto);

            return new OkObjectResult(response);
        }
        catch (Exception e)
        {
            var unexpectedResponse = new ApiResponse(ErrorCode.ServerError, e.Message);

            return new ObjectResult(unexpectedResponse){StatusCode = StatusCodes.Status500InternalServerError};
        }
    }

    [HttpGet]
    [Route("{venueId}/productions")]
    [ProducesResponseType(typeof(ProductionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> GetVenueProductions([FromRoute] int venueId, int? page, int? size)
    {
        try
        {
            var (validation, productions) = await _validation.ValidateAndFetchVenueProductions(venueId);

            if (!validation.Success)
            {
                var errorResponse = new ApiResponse(ErrorCode.NotFound, validation.Message!);
                return new NotFoundObjectResult(errorResponse);
            }
            
            productions = productions!
                .DistinctBy(p => p.Id)
                .ToList();              //removes double entries from results.
            var prodFilter = new ProductionSearchFilters
            {
                Title = null
            };
            // var productionsDto = _service.ProductionsToDto(productions);

            var paginationResult = _productionService.Paginate(page, size, prodFilter, productions);
            
            var response = new ApiResponse<PaginationResult<ProductionDto>>(paginationResult);

            return new OkObjectResult(response);
        }
        catch (Exception e)
        {
            var unexpectedResponse = new ApiResponse(ErrorCode.ServerError, e.Message);

            return new ObjectResult(unexpectedResponse){StatusCode = StatusCodes.Status500InternalServerError};
        }
    }

    /// <summary>
    /// Endpoint to creating a venue.
    /// </summary>
    /// <param name="venueCreateDto"></param>
    /// <returns></returns>
    [HttpPost]
    [TypeFilter(typeof(AdminAuthorizationFilter))]
    [ProducesResponseType(typeof(VenueDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> CreateVenue([FromBody] VenueCreateDto venueCreateDto)
    {
        try
        {
            var (validation, venue) = await _validation.ValidateAndFetch(venueCreateDto.Title);

            if (!validation.Success)
            {
                var errorResponse = new ApiResponse<Venue>(venue, (ErrorCode)validation.ErrorCode!, validation.Message!);
                return new BadRequestObjectResult(errorResponse);
            }
            
            var createdVenue = await _service.Create(venueCreateDto);

            var createdVenueDto = _service.ToDto(createdVenue);

            var response = new ApiResponse<VenueDto>(createdVenueDto, "Venue successfully added");

            return new OkObjectResult(response);
        }
        catch (Exception e)
        {
            var unexpectedResponse = new ApiResponse(ErrorCode.ServerError, e.Message);

            return new ObjectResult(unexpectedResponse){StatusCode = StatusCodes.Status500InternalServerError};
        }
    }

    [HttpPost]
    [Route("range")]
    [TypeFilter(typeof(AdminAuthorizationFilter))]
    [ProducesResponseType(typeof(VenuesCreationResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> CreateVenues([FromBody] List<VenueCreateDto> venueCreateDto)
    {
        try
        {
            var existingVenues = await _service.GetVenuesByTitles(venueCreateDto.Select(dto => dto.Title).ToList());

            var (venuesToUpdate,  venuesToCreate, responseList) = await _service.CreateUpdateList(existingVenues, venueCreateDto);

            var venuesCreationResponseDto = new VenuesCreationResponseDto
            {
                CreatedCount = venuesToCreate.Count,
                UpdatedCount = venuesToUpdate.Count,
                VenueDtos = responseList
            };

            return Ok(new ApiResponse<VenuesCreationResponseDto>(venuesCreationResponseDto));
        }
        catch (Exception e)
        {
            var unexpectedResponse = new ApiResponse(ErrorCode.ServerError, e.Message);

            return new ObjectResult(unexpectedResponse){StatusCode = StatusCodes.Status500InternalServerError};
        }
    }

    [HttpPost]
    [Route("claim-venue/{id}")]
    [TypeFilter(typeof(AnyRoleAuthorizationFilter))]
    public async Task<ActionResult<ApiResponse>> ClaimVenue([FromRoute] int id)
    {
        try
        {
            var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var (validation, user, venue) = await _validation.ValidateUserWithVenuesForClaim(email!, id);
            //fail case
            if (!validation.Success)
            {
                if (validation.ErrorCode == ErrorCode.NotFound)
                {
                    var errorResponse = new ApiResponse((ErrorCode)validation.ErrorCode!, validation.Message!);
                    return new ObjectResult(errorResponse) { StatusCode = (int)HttpStatusCode.NotFound };
                }

                var errorResponse1 = new ApiResponse((ErrorCode)validation.ErrorCode!, validation.Message!);
                return new ObjectResult(errorResponse1){StatusCode = (int)HttpStatusCode.BadRequest};
            }
            
            //success case
            await _userVenueService.CreateUserVenue(user!, venue!);

            return Ok(new ApiResponse("You have successfully claimed this place!"));
        }
        catch (Exception e)
        {
            var unexpectedResponse = new ApiResponse(ErrorCode.ServerError, e.Message);

            return new ObjectResult(unexpectedResponse){StatusCode = StatusCodes.Status500InternalServerError};
        }
    }

    /*/// <summary>
    /// Endpoint to deleting a venue.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete]
    [Route("{id}")]
    [TypeFilter(typeof(AdminAuthorizationFilter))]
    public async Task<ActionResult<ApiResponse>> DeleteVenue(int id)
    {
        try
        {
            var (validation, venue) = await _validation.ValidateForDelete(id);

            if (!validation.Success)
            {
                var errorResponse = new ApiResponse(ErrorCode.NotFound, validation.Message);
                return new ObjectResult(errorResponse) { StatusCode = 404 };
            }

            await _service.Delete(venue);

            ApiResponse response = new ApiResponse(message: $"Venue with ID: {id} has been deleted!");

            return new ObjectResult(response);
        }
        catch (Exception e)
        {
            var unexpectedResponse = new ApiResponse(ErrorCode.ServerError, e.Message);

            return new ObjectResult(unexpectedResponse){StatusCode = StatusCodes.Status500InternalServerError};
        }
    }*/

    /// <summary>
    /// Users can update their claimed venues only, through this endpoint.
    /// </summary>
    /// <param name="venueDto"></param>
    /// <returns></returns>
    [HttpPut("update")]
    [TypeFilter(typeof(AnyRoleAuthorizationFilter))]
    public async Task<ActionResult<ApiResponse>> UpdateVenue([FromBody] VenueUpdateDto venueDto)
    {
        try
        {
            var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            
            var userVenues = await _userVenueService.GetUserVenues(email!);
            
            var (validation, venue) = _validation.ValidateForUpdate(userVenues, venueDto);
            
            if (!validation.Success)
            {
                if (validation.ErrorCode == ErrorCode.NotFound)
                {
                    var errorResponse = new ApiResponse(ErrorCode.NotFound, validation.Message!);
                    return new NotFoundObjectResult(errorResponse);
                }

                var errorResponseBadRequest = new ApiResponse(ErrorCode.BadRequest, validation.Message!);
                return new BadRequestObjectResult(errorResponseBadRequest);
            }

            var updatedVenue = await _service.Update(venue!, venueDto);
            
            var response = new ApiResponse<VenueResponseDto>(updatedVenue, "You have successfully edited your venue!");

            return new ObjectResult(response);
        }
        catch (Exception e)
        {
            var unexpectedResponse = new ApiResponse(ErrorCode.ServerError, e.Message);

            return new ObjectResult(unexpectedResponse){StatusCode = StatusCodes.Status500InternalServerError};
        }
    }

}