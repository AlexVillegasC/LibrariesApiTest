# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/HackerRank1.API/HackerRank1.API.csproj", "src/HackerRank1.API/"]
COPY ["src/LibraryService.SharedKernel/LibraryService.SharedKernel.csproj", "src/LibraryService.SharedKernel/"]
COPY ["src/Modules/LibraryService.Modules.Auth/LibraryService.Modules.Auth.csproj", "src/Modules/LibraryService.Modules.Auth/"]
COPY ["src/Modules/LibraryService.Modules.Books/LibraryService.Modules.Books.csproj", "src/Modules/LibraryService.Modules.Books/"]
COPY ["src/Modules/LibraryService.Modules.Libraries/LibraryService.Modules.Libraries.csproj", "src/Modules/LibraryService.Modules.Libraries/"]
RUN dotnet restore "./src/HackerRank1.API/HackerRank1.API.csproj"
COPY . .
WORKDIR "/src/src/HackerRank1.API"
RUN dotnet build "./HackerRank1.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./HackerRank1.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HackerRank1.API.dll"]