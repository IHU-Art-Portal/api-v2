# Use the official ASP.NET Core runtime image as a base image
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

# Use the official ASP.NET Core SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Theatrical.Api/Theatrical.Api.csproj", "Theatrical.Api/"]
COPY ["Theatrical.Data/Theatrical.Data.csproj", "Theatrical.Data/"]
COPY ["Theatrical.Services/Theatrical.Services.csproj", "Theatrical.Services/"]
COPY ["Theatrical.Dto/Theatrical.Dto.csproj", "Theatrical.Dto/"]
RUN dotnet restore "Theatrical.Api/Theatrical.Api.csproj"
COPY . .
WORKDIR "/src/Theatrical.Api"
RUN dotnet build "Theatrical.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Theatrical.Api.csproj" -c Release -o /app/publish

# Create the final image with the published application
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Theatrical.Api.dll"]
