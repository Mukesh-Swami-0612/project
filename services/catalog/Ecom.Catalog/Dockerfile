FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Ecom.Catalog.API/Ecom.Catalog.API.csproj", "Ecom.Catalog.API/"]
COPY ["Ecom.Catalog.Application/Ecom.Catalog.Application.csproj", "Ecom.Catalog.Application/"]
COPY ["Ecom.Catalog.Domain/Ecom.Catalog.Domain.csproj", "Ecom.Catalog.Domain/"]
COPY ["Ecom.Catalog.Infrastructure/Ecom.Catalog.Infrastructure.csproj", "Ecom.Catalog.Infrastructure/"]
RUN dotnet restore "Ecom.Catalog.API/Ecom.Catalog.API.csproj"
COPY . .
RUN dotnet build "Ecom.Catalog.API/Ecom.Catalog.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Ecom.Catalog.API/Ecom.Catalog.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ecom.Catalog.API.dll"]
