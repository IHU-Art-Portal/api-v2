using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Minio;
using Newtonsoft.Json;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using Theatrical.Api.Swagger;
using Theatrical.Data.Context;
using Theatrical.Dto.UsersDtos;
using Theatrical.Services;
using Theatrical.Services.Caching;
using Theatrical.Services.Curators;
using Theatrical.Services.Curators.DataCreationCurators;
using Theatrical.Services.Email;
using Theatrical.Services.Pagination;
using Theatrical.Services.PerformersService;
using Theatrical.Services.Repositories;
using Theatrical.Services.Security.AuthorizationFilters;
using Theatrical.Services.Security.Jwt;
using Theatrical.Services.Validation;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var jwtOptions = config.GetSection("JwtOptions").Get<JwtOptions>();

// Add services to the container.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(x =>
{
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = jwtOptions!.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
});
builder.Services.AddAuthorization();

builder.Services.AddControllers().AddNewtonsoftJson(o =>
{
    o.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
    o.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(swagger =>
{
    swagger.SwaggerDoc("v2", new OpenApiInfo { Title = "Theatrical.Api", Version = "v2" });
                    
});

//Swagger UI authorization support
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

//dbconnection
builder.Services.AddDbContext<TheatricalPlaysDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")!));

//services registering
//persons services
builder.Services.AddTransient<IPersonRepository, PersonRepository>();
builder.Services.AddTransient<IPersonService, PersonService>();
builder.Services.AddTransient<IPersonValidationService, PersonValidationService>();

//role services
builder.Services.AddTransient<IRoleRepository, RoleRepository>();
builder.Services.AddTransient<IRoleService, RoleService>();
builder.Services.AddTransient<IRoleValidationService, RoleValidationService>();

//organizer services
builder.Services.AddTransient<IOrganizerRepository, OrganizerRepository>();
builder.Services.AddTransient<IOrganizerService, OrganizerService>();
builder.Services.AddTransient<IOrganizerValidationService, OrganizerValidationService>();

//Venue services
builder.Services.AddTransient<IVenueRepository, VenueRepository>();
builder.Services.AddTransient<IVenueService, VenueService>();
builder.Services.AddTransient<IVenueValidationService, VenueValidationService>();

//User services
builder.Services.AddTransient<IUserRepository, UserRepository>();
builder.Services.AddTransient<IUserValidationService, UserValidationService>();
builder.Services.AddTransient<IUserService, UserService>();

//Production services
builder.Services.AddTransient<IProductionRepository, ProductionRepository>();
builder.Services.AddTransient<IProductionValidationService, ProductionValidationService>();
builder.Services.AddTransient<IProductionService, ProductionService>();

//Event services
builder.Services.AddTransient<IEventRepository, EventRepository>();
builder.Services.AddTransient<IEventValidationService, EventValidationService>();
builder.Services.AddTransient<IEventService, EventService>();

//Contribution services
builder.Services.AddTransient<IContributionRepository, ContributionRepository>();
builder.Services.AddTransient<IContributionValidationService, ContributionValidationService>();
builder.Services.AddTransient<IContributionService, ContributionService>();

//Log services
builder.Services.AddTransient<ILogRepository, LogRepository>();
builder.Services.AddTransient<ILogService, LogService>();

//Transaction services
builder.Services.AddTransient<ITransactionRepository, TransactionRepository>();
builder.Services.AddTransient<ITransactionValidationService, TransactionValidationService>();
builder.Services.AddTransient<ITransactionService, TransactionService>();

//Jwt Token service
builder.Services.AddTransient<ITokenService, TokenService>();

//Pagination service
builder.Services.AddTransient<IPaginationService, PaginationService>();

//Curator service
builder.Services.AddTransient<IRoleSimplifierCurator, RoleSimplifierCurator>();
builder.Services.AddTransient<ICuratorIncomingData, CuratorIncomingData>();

//Email service
builder.Services.AddTransient<IEmailService, EmailService>();

//Removed authorization filters, they can be used directly without registering.

//Memory caching and ICaching service
builder.Services.AddMemoryCache();
builder.Services.AddTransient<ICaching, Caching>();

//Account Requests Services
builder.Services.AddTransient<IAccountRequestRepository, AccountRequestRepository>();
builder.Services.AddTransient<IAccountRequestService, AccountRequestService>();
builder.Services.AddTransient<IAccountRequestValidationService, AccountRequestRequestValidationService>();

//Authorization Filters
builder.Services.AddTransient<AdminAuthorizationFilter>();
builder.Services.AddTransient<UserAuthorizationFilter>();
builder.Services.AddTransient<AnyRoleAuthorizationFilter>();
builder.Services.AddTransient<ClaimsManagerAuthorizationFilter>();

//Minio service
builder.Services.AddTransient<IMinioService, MinioService>();
builder.Services.AddMinio("TPLBhn60BoCIWuiyblFN", "uVKnOUdQoXfRvwbXUOyqMJrMBqwS6FrMa8fU5ORX");

//cors
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowOrigin",
        builderCors =>
        {
            builderCors.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

//Serilog Console log styling
var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v2/swagger.json", "Theatrical.Api v2"));
app.UseCors("AllowOrigin");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();