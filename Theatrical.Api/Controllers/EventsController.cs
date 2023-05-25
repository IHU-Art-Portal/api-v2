﻿using Microsoft.AspNetCore.Mvc;
using Theatrical.Data.Models;
using Theatrical.Dto.EventDtos;
using Theatrical.Dto.ResponseWrapperFolder;
using Theatrical.Services;
using Theatrical.Services.Validation;

namespace Theatrical.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly IEventValidationService _validation;
    private readonly IEventService _service;

    public EventsController(IEventService service, IEventValidationService validation)
    {
        _service = service;
        _validation = validation;
    }

    [HttpGet]
    public async Task<ActionResult<TheatricalResponse>> GetEvents()
    {
        var (validation, events) = await _validation.FetchAndValidate();

        if (!validation.Success)
        {
            var errorResponse = new TheatricalResponse(ErrorCode.NotFound, validation.Message!);
            return new ObjectResult(errorResponse) { StatusCode = 404 };
        }
        
        var response = new TheatricalResponse<List<Event>>(events);
        return new OkObjectResult(response);
    }
    
    [HttpPost]
    public async Task<ActionResult<TheatricalResponse>> CreateEvent([FromBody] CreateEventDto createEventDto)
    {
        var validation = await _validation.ValidateForCreate(createEventDto);

        if (!validation.Success)
        {
            var errorResponse = new TheatricalResponse(ErrorCode.NotFound, validation.Message);
            return new ObjectResult(errorResponse) { StatusCode = 404 };
        }

        await _service.Create(createEventDto);
        var response = new TheatricalResponse("Successfully Created Event");
        
        return new ObjectResult(response);
    }
}