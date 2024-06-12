using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Theatrical.Data.enums;
using Theatrical.Dto.Pagination;
using Theatrical.Dto.ProductionDtos;
using Theatrical.Dto.ResponseWrapperFolder;
using Theatrical.Services;
using Theatrical.Services.ProductionService;
using Theatrical.Services.Security.AuthorizationFilters;
using Theatrical.Services.Validation;

namespace Theatrical.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors("AllowOrigin")]
public class ProductionsController : ControllerBase
{
    private readonly IProductionValidationService _validation;
    private readonly IProductionService _service;

    public ProductionsController(IProductionService service, IProductionValidationService validation)
    {
        _service = service;
        _validation = validation;
    }
    
    /// <summary>
    /// Endpoint to creating a new Production.
    /// </summary>
    /// <param name="createProductionDto"></param>
    /// <returns></returns>
    [HttpPost]
    [TypeFilter(typeof(AdminAuthorizationFilter))]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProductionDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse>> CreateProduction([FromBody] CreateProductionDto createProductionDto)
    {
        try
        {
            var validation = await _validation.ValidateForCreate(createProductionDto);

            if (!validation.Success)
            {
                var errorResponse = new ApiResponse(ErrorCode.NotFound, validation.Message!);
                return new ObjectResult(errorResponse) { StatusCode = 404 };
            }

            var createdProduction = await _service.Create(createProductionDto);

            var response = new ApiResponse<ProductionDto>(createdProduction, "Successfully Created Production");

            return new ObjectResult(response) { StatusCode = 201 };
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
    [ProducesResponseType(typeof(ProductionsCreationResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> CreateProductions([FromBody] List<CreateProductionDto> createProductionDtos)
    {
        try
        {
            var existingProductions = await _service.GetProductionsByTitles(createProductionDtos.Select(dto => dto.Title).ToList());
            
            var (updatedProductions, createdProductions, responseList) = await _service.UpdateOrCreateAndProduceResponseList(existingProductions, createProductionDtos);

            var productionsCreationResponseDto = new ProductionsCreationResponseDto
            {
                CreatedCount = createdProductions.Count,
                UpdatedCount = updatedProductions.Count,
                ShortenedProductionDtos = responseList
            };

            return Ok(new ApiResponse<ProductionsCreationResponseDto>(productionsCreationResponseDto));
        }
        catch (Exception e)
        {
            var unexpectedResponse = new ApiResponse(ErrorCode.ServerError, e.Message);

            return new ObjectResult(unexpectedResponse){StatusCode = StatusCodes.Status500InternalServerError};
        }
    }

    /// <summary>
    /// Endpoint to fetching all productions
    /// Pagination Available.
    /// </summary>
    /// <param name="page"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(PaginationResult<ProductionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> GetProductions(int? page, int? size, string? title, string? producer)
    {
        try
        {
            var searchFilters = new ProductionSearchFilters
            {
                Title = title,
                Producer = producer
            };

            var (validation, productions) = await _validation.ValidateAndFetch();

            if (!validation.Success)
            {
                var errorResponse = new ApiResponse(ErrorCode.NotFound, validation.Message!);
                return new ObjectResult(errorResponse) { StatusCode = 404 };
            }

            // var productionsDto = _service.ConvertToDto(productions);

            var paginationResult = _service.Paginate(page, size, searchFilters, productions);

            var response = new ApiResponse<PaginationResult<ProductionDto>>(paginationResult);

            return new OkObjectResult(response);
        }
        catch (Exception e)
        {
            var unexpectedResponse = new ApiResponse(ErrorCode.ServerError, e.Message);

            return new ObjectResult(unexpectedResponse){StatusCode = StatusCodes.Status500InternalServerError};
        }
    }

    
    [HttpGet]
    [Route("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProductionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> GetProduction([FromRoute] int id)
    {
        try
        {
            var (validation, production) = await _validation.ValidateAndFetchProduction(id);

            if (!validation.Success)
            {
                var errorResponse = new ApiResponse(ErrorCode.NotFound, validation.Message!);
                return new NotFoundObjectResult(errorResponse);
            }

            var productionDto = _service.ToDto(production!);
            
            var response = new ApiResponse<ProductionDto>(productionDto);
            
            return new OkObjectResult(response);
        }
        catch (Exception e)
        {
            var unexpectedResponse = new ApiResponse(ErrorCode.ServerError, e.Message);

            return new ObjectResult(unexpectedResponse){StatusCode = StatusCodes.Status500InternalServerError};
        }
    }

    /*[HttpDelete]
    [TypeFilter(typeof(CustomAuthorizationFilter))]
    public async Task<ActionResult<ApiResponse>> DeleteProduction(int id)
    {
        var (validation, production) = await _validation.ValidateForDelete(id);

        if (!validation.Success)
        {
            var errorResponse = new ApiResponse(ErrorCode.NotFound, validation.Message);
            return new ObjectResult(errorResponse){StatusCode = 404};
        }

        await _service.Delete(production);
        var response = new ApiResponse("Successfully Delete Production");

        return new ObjectResult(response);
    }*/
}