using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Theatrical.Data.enums;
using Theatrical.Data.Models;
using Theatrical.Dto.Pagination;
using Theatrical.Dto.PersonDtos;
using Theatrical.Dto.ResponseWrapperFolder;
using Theatrical.Services.PersonService;
using Theatrical.Services.Security.AuthorizationFilters;
using Theatrical.Services.Validation;

namespace Theatrical.Api.Controllers;

[ApiController]
[Route("/api/[controller]")]
[EnableCors("AllowOrigin")]
public class PeopleController : ControllerBase
{
    private readonly IPersonService _service;
    private readonly IPersonValidationService _validation;

    public PeopleController(IPersonService service, IPersonValidationService validation)
    {
        _service = service;
        _validation = validation;
    }

    /// <summary>
    /// Retrieves performer information by their Id
    /// </summary>
    /// <param name="id">int</param>
    /// <returns>TheatricalResponse&lt;PerformerDto&gt; object containing performer data.</returns>
    /// ToDo update person if they exist.
    [HttpGet]
    [Route("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(PersonDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PersonDto>>> GetPerson(int id)
    {
        try
        {
            var (validation, person) = await _validation.ValidateAndFetch(id);

            if (!validation.Success)
            {
                ApiResponse errorResponse = new ApiResponse((ErrorCode)validation.ErrorCode!, validation.Message!);
                return new ObjectResult(errorResponse) { StatusCode = 404 };
            }

            var performerDto = _service.ToDto(person!);

            ApiResponse response = new ApiResponse<PersonDto>(performerDto);

            return new OkObjectResult(response);
        }
        catch (Exception e)
        {
            var unexpectedResponse = new ApiResponse(ErrorCode.ServerError, e.Message);

            return new ObjectResult(unexpectedResponse){StatusCode = StatusCodes.Status500InternalServerError};
        }
    }

    /// <summary>
    /// Retrieves all performers if pagination parameters are not specified
    /// </summary>
    /// <param name="page">Optional. The page number for pagination</param>
    /// <param name="size">Optional. THe page size for pagination</param>
    /// <param name="searchFilters"></param>
    /// <param name="showAvailable"></param>
    /// <param name="alphabeticalOrder"></param>
    /// <param name="role"></param>
    /// <param name="age"></param>
    /// <param name="height"></param>
    /// <param name="weight"></param>
    /// <param name="eyeColor"></param>
    /// <param name="hairColor"></param>
    /// <param name="languageKnowledge"></param>
    /// <param name="name"></param>
    /// <returns>TheatricalResponse&lt;PerformersPaginationDto&gt; object containing paginated items.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginationResult<PersonDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> GetPeople(int? page, int? size, bool? showAvailable,
        bool? alphabeticalOrder, string? role, int? age, string? height, string? weight, string? eyeColor,
        string? hairColor, string? languageKnowledge, string? name)
    {
        try
        {
            var searchFilters = new SearchFilters
            {
                ShowAvailableAccounts = showAvailable,
                AlphabeticalOrder = alphabeticalOrder,
                Role = role,
                Age = age,
                Height = height,
                Weight = weight,
                EyeColor = eyeColor,
                HairColor = hairColor,
                LanguageKnowledge = languageKnowledge,
                Name = name
            };
            var peopleDto = await _service.GetAndPaginate(page, size, searchFilters);
            
            ApiResponse response = new ApiResponse<PaginationResult<PersonDto>>(peopleDto);

            return new ObjectResult(response);
        }
        catch (Exception e)
        {
            var unexpectedResponse = new ApiResponse(ErrorCode.ServerError, e.Message);

            return new ObjectResult(unexpectedResponse){StatusCode = StatusCodes.Status500InternalServerError};
        }
    }

    /// <summary>
    /// Endpoint to creating a new Person.
    /// Adding data requires you to specify your system/application.
    /// System 2 for Python, 3 for C#, 4 for C, 5 for C++, 10 for Java, 11 for Android, 12 for iOS, 13 for Javascript, 14 for Testing
    /// </summary>
    /// <param name="createPersonDto">Fullname, ImageLinks (if any), System ID</param>
    /// <returns></returns>
    [HttpPost]
    [TypeFilter(typeof(AdminAuthorizationFilter))]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(PersonDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> CreatePerson([FromBody] CreatePersonDto createPersonDto)
    {
        try
        {
            if (string.IsNullOrEmpty(createPersonDto.Fullname))
            {
                return new BadRequestObjectResult(new ApiResponse(ErrorCode.BadRequest, "Fullname value must not be empty or null"));
            }
            
            var (validation, correctedCreatePersonDto) = await _validation.ValidateForCreate(createPersonDto);

            if (!validation.Success)
            {
                if (validation.ErrorCode == ErrorCode.BadRequest)
                {
                    return new BadRequestObjectResult(new ApiResponse(ErrorCode.BadRequest, validation.Message!));
                }
                var errorResponse = new ApiResponse((ErrorCode)validation.ErrorCode!, validation.Message!);
                
                return new ObjectResult(errorResponse) { StatusCode = StatusCodes.Status400BadRequest};
            }

            var createdPerson = await _service.Create(correctedCreatePersonDto!);
            var createdPersonDto = _service.ToDto(createdPerson);

            var response = new ApiResponse<PersonDto>(createdPersonDto,"Successfully Created Person");

            return new OkObjectResult(response);
        }
        catch (Exception e)
        {
            var unexpectedResponse = new ApiResponse(ErrorCode.ServerError, e.Message);

            return new ObjectResult(unexpectedResponse){StatusCode = StatusCodes.Status500InternalServerError};
        }
    }

    [HttpPost("Addrange")]
    [TypeFilter(typeof(AdminAuthorizationFilter))]
    [ProducesResponseType(typeof(CreatePeopleStatusReport), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> CreatePeople([FromBody] List<CreatePersonDto> createPersonDto)
    {
        try
        {
            var validPeople = new List<CreatePersonDto>();
            var nullFullNamePeople = new List<CreatePersonDto>();
            foreach (var person in createPersonDto)
            {
                if (!string.IsNullOrEmpty(person.Fullname) && !string.IsNullOrWhiteSpace(person.Fullname))
                { 
                    validPeople.Add(person);
                    continue;
                }
                nullFullNamePeople.Add(person);
            }

            var peopleUpdated = new List<Person>();
            if (validPeople.Count > 0)
            {
                var (alreadyExistingPeople, addingPeople) = await _validation.ValidateForCreateList(validPeople);
                if (alreadyExistingPeople is not null && alreadyExistingPeople.Count > 0)
                {
                    peopleUpdated = await _service.UpdateList(alreadyExistingPeople, validPeople);
                }
                
                var peopleCreated = await _service.CreateList(addingPeople!);
                var peopleCreatedShortenedDto = _service.ToDtoRange(peopleCreated);
                var peopleUpdatedShortenedDto = _service.ToDtoRange(peopleUpdated);
                
                var statusReport = new CreatePeopleStatusReport
                {
                    AddedPeople = addingPeople!.Count,
                    AlreadyExistingButUpdatedPeople = alreadyExistingPeople?.Count ?? 0,
                    NullNameNotAddedPeople = nullFullNamePeople.Count,
                    PeopleUpdated = peopleUpdatedShortenedDto,
                    PeopleCreated = peopleCreatedShortenedDto
                };
                
                var apiResponse = new ApiResponse<CreatePeopleStatusReport>(statusReport);
                return new OkObjectResult(apiResponse);
            }
            
            var statusReport1 = new CreatePeopleStatusReport
            {
                AddedPeople = 0,
                AlreadyExistingButUpdatedPeople = 0,
                NullNameNotAddedPeople = nullFullNamePeople.Count,
                PeopleUpdated = null,
                PeopleCreated = null
            };
                
            var apiResponse1 = new ApiResponse<CreatePeopleStatusReport>(statusReport1);
            return new OkObjectResult(apiResponse1);
        }
        catch (Exception e)
        {
            var unexpectedResponse = new ApiResponse(ErrorCode.ServerError, e.Message);

            return new ObjectResult(unexpectedResponse) { StatusCode = StatusCodes.Status500InternalServerError };
        }
    }

    /// <summary>
    /// Endpoint to fetching all Person(s) by a specific role.
    /// Pagination Available.
    /// </summary>
    /// <param name="role"></param>
    /// <param name="page"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("role/{role}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(PaginationResult<PersonDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> GetPeopleByRole(string role, int? page, int? size)
    {
        try
        {
            var (validation, people) = await _validation.ValidateForFetchRole(role);

            if (!validation.Success)
            {
                var errorResponse = new ApiResponse((ErrorCode)validation.ErrorCode!, validation.Message!);
                return new ObjectResult(errorResponse) { StatusCode = 404 };
            }

            var paginationResult = _service.PaginateAndProduceDtos(people!, page, size);

            var response = new ApiResponse<PaginationResult<PersonDto>>(paginationResult);
            return new OkObjectResult(response);
        }
        catch (Exception e)
        {
            var unexpectedResponse = new ApiResponse(ErrorCode.ServerError, e.Message);

            return new ObjectResult(unexpectedResponse){StatusCode = StatusCodes.Status500InternalServerError};
        }
    }

    /// <summary>
    /// Endpoint to fetching a Person by their initials.
    /// </summary>
    /// <param name="letters"></param>
    /// <param name="page"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("initials/{letters}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(PaginationResult<PersonDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> GetPeopleByInitialLetter(string letters, int? page, int? size)
    {
        try
        {
            var (validation, persons) = await _validation.ValidateForInitials(letters);

            if (!validation.Success)
            {
                var errorResponse = new ApiResponse(ErrorCode.NotFound, validation.Message!);
                return new ObjectResult(errorResponse){StatusCode = 404};
            }

            var paginatedResult = _service.PaginateAndProduceDtos(persons!, page, size);

            var response = new ApiResponse<PaginationResult<PersonDto>>(paginatedResult);

            return new OkObjectResult(response);
        }
        catch (Exception e)
        {
            var unexpectedResponse = new ApiResponse(ErrorCode.ServerError, e.Message);

            return new ObjectResult(unexpectedResponse){StatusCode = StatusCodes.Status500InternalServerError};
        }
    }

    /// <summary>
    /// Endpoint to fetching all Productions a Person participates in.
    /// Pagination Available.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="page"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("{id}/productions")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(PaginationResult<PersonProductionsRoleInfo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> GetPersonProductions(int id, int? page, int? size)
    {
        try
        {
            var (validation, productions) = await _validation.ValidatePersonsProductions(id);

            if (!validation.Success)
            {
                var errorResponse = new ApiResponse(ErrorCode.NotFound, validation.Message!);
                return new ObjectResult(errorResponse) { StatusCode = 404 };
            }

            var paginationResult = _service.PaginateContributionsOfPerson(productions!, page, size);
            
            var response = new ApiResponse<PaginationResult<PersonProductionsRoleInfo>>(paginationResult);
            
            return new OkObjectResult(response);
        }
        catch (Exception e)
        {
            var unexpectedResponse = new ApiResponse(ErrorCode.ServerError, e.Message);

            return new ObjectResult(unexpectedResponse){StatusCode = StatusCodes.Status500InternalServerError};
        }
    }

    /// <summary>
    /// Endpoint to fetching all photos by Person's Id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("{id}/photos")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ImageDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> GetPersonsPhotos(int id)
    {
        try
        {
            var (validation, images) = await _validation.ValidatePersonsPhotos(id);

            if (!validation.Success)
            {
                var errorResponse = new ApiResponse(ErrorCode.NotFound, validation.Message!);
                return new ObjectResult(errorResponse) { StatusCode = 404 };
            }
            
            var imagesDto = _service.ImagesToDto(images);
            
            var response = new ApiResponse<List<ImageDto>>(imagesDto);
            
            return new OkObjectResult(response);
        }
        catch (Exception e)
        {
            var unexpectedResponse = new ApiResponse(ErrorCode.ServerError, e.Message);

            return new ObjectResult(unexpectedResponse){StatusCode = StatusCodes.Status500InternalServerError};
        }
    }
    
    /// <summary>
    /// Endpoint to deleting a Person by id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete]
    [Route("{id}")]
    [TypeFilter(typeof(AdminAuthorizationFilter))]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeletePerson(int id)
    {
        try
        {
            var (validation, performer) = await _validation.ValidateForDelete(id);

            if (!validation.Success)
            {
                var errorResponse = new ApiResponse((ErrorCode)validation.ErrorCode!, validation.Message!);
                return new ObjectResult(errorResponse) { StatusCode = 404 };
            }

            await _service.Delete(performer!);
            ApiResponse response = new ApiResponse(message: $"Person with ID: {id} has been deleted!");

            return new OkObjectResult(response);
        }
        catch (Exception e)
        {
            if (e.HResult.Equals(-2146233088))
            {
                return new ObjectResult(new ApiResponse(ErrorCode.ServerError, "Object issue due to caching, or foreign key on person's images.")) { StatusCode = 500 };
            }

            return new ObjectResult(new ApiResponse(ErrorCode.ServerError, e.Message));
        }
    }

    [HttpGet]
    [Route("photos")]
    [ProducesResponseType(typeof(Image), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> GetPhotos()
    {
        try
        {
            var images = await _service.GetImages();

            var apiResponse = new ApiResponse<List<Image>>(images);
            
            return new OkObjectResult(apiResponse);
        }
        catch (Exception e)
        {
            return new ObjectResult(new ApiResponse(ErrorCode.ServerError, e.Message));
        }
    }

    [HttpDelete]
    [Route("DeleteTestData")]
    [ProducesResponseType(typeof(NoContentResult), StatusCodes.Status204NoContent)]
    public async Task<ActionResult> DeleteTestData(string? Test_Data_Are_Considered_Data_Added_Or_Modified_By_SystemID_14)
    {
        await _service.DeleteTestData();
        return NoContent();
    }

    /*/// <summary>
    /// Caution, long running operation.
    /// This Method takes every contribution and maps the role to the new person column for roles.
    /// For each person's Contribution.Role => it maps the role to Person.Roles
    /// </summary>
    /// <param name="DontRun_LongOperation"></param>
    /// <param name="DebuggingPurposesOnly"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("MapContributionPersonRolesToPersonRoles")]
    [TypeFilter(typeof(AdminAuthorizationFilter))]
    public async Task<IActionResult> MapContrToPersonRoles([FromQuery] string? DontRun_LongOperation, string? DebuggingPurposesOnly)
    {
        var personsUpdated = await _service.MapContributionRolesToPerson();

        var apiResponse = new ApiResponse<List<PersonDto>>(personsUpdated);
        
        return new OkObjectResult(apiResponse);
    }*/

}