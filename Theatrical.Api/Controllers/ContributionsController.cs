﻿using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Theatrical.Data.Models;
using Theatrical.Dto.ContributionDtos;
using Theatrical.Dto.Pagination;
using Theatrical.Dto.ResponseWrapperFolder;
using Theatrical.Services;
using Theatrical.Services.Validation;

namespace Theatrical.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors("AllowOrigin")]
public class ContributionsController : ControllerBase
{
    private readonly IContributionValidationService _validation;
    private readonly IContributionService _service;

    public ContributionsController(IContributionValidationService validation, IContributionService service)
    {
        _service = service;
        _validation = validation;
    }
    
    [HttpGet]
    public async Task<ActionResult<ApiResponse>> GetContributions(int? page, int? size)
    {
        try
        {
            var (validation, contributions) = await _validation.ValidateForFetch();

            if (!validation.Success)
            {
                var errorResponse = new ApiResponse(ErrorCode.NotFound, validation.Message!);
                return new ObjectResult(errorResponse) { StatusCode = StatusCodes.Status404NotFound };
            }

            var contributionDtos = _service.ToDto(contributions);

            var paginationResult = _service.Paginate(page, size, contributionDtos);
            
            var response = new ApiResponse<PaginationResult<ContributionDto>>(paginationResult);

            return new OkObjectResult(response);
        }
        catch (Exception e)
        {
            var unexpectedResponse = new ApiResponse(ErrorCode.ServerError, e.Message.ToString());

            return new ObjectResult(unexpectedResponse){StatusCode = StatusCodes.Status500InternalServerError};
        }
    }

    [HttpPost]
    [TypeFilter(typeof(AdminAuthorizationFilter))]
    public async Task<ActionResult<ApiResponse>> CreateContribution([FromBody] CreateContributionDto contributionDto)
    {
        try
        {
            var validation = await _validation.ValidateForCreate(contributionDto);

            if (!validation.Success)
            {
                var errorResponse = new ApiResponse(ErrorCode.NotFound, validation.Message!);
                return new ObjectResult(errorResponse) { StatusCode = StatusCodes.Status404NotFound };
            }

            await _service.Create(contributionDto);

            var response = new ApiResponse("Successfully Created Contribution");

            return new OkObjectResult(response);
        }
        catch (Exception e)
        {
            var unexpectedResponse = new ApiResponse(ErrorCode.ServerError, e.Message.ToString());

            return new ObjectResult(unexpectedResponse){StatusCode = StatusCodes.Status500InternalServerError};
        }
    }
}