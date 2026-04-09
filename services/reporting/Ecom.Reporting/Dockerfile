FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Ecom.Reporting.API/Ecom.Reporting.API.csproj", "Ecom.Reporting.API/"]
COPY ["Ecom.Reporting.Application/Ecom.Reporting.Application.csproj", "Ecom.Reporting.Application/"]
COPY ["Ecom.Reporting.Domain/Ecom.Reporting.Domain.csproj", "Ecom.Reporting.Domain/"]
COPY ["Ecom.Reporting.Infrastructure/Ecom.Reporting.Infrastructure.csproj", "Ecom.Reporting.Infrastructure/"]
RUN dotnet restore "Ecom.Reporting.API/Ecom.Reporting.API.csproj"
COPY . .
RUN dotnet build "Ecom.Reporting.API/Ecom.Reporting.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Ecom.Reporting.API/Ecom.Reporting.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ecom.Reporting.API.dll"]
