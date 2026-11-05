FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/PersonalPortfolio.Api/PersonalPortfolio.Api.csproj src/PersonalPortfolio.Api/
RUN dotnet restore src/PersonalPortfolio.Api/PersonalPortfolio.Api.csproj

COPY src/PersonalPortfolio.Api/ src/PersonalPortfolio.Api/
RUN dotnet publish src/PersonalPortfolio.Api/PersonalPortfolio.Api.csproj --configuration Release --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PersonalPortfolio.Api.dll"]

