FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Ecom.Auth.API/Ecom.Auth.API.csproj", "Ecom.Auth.API/"]
COPY ["Ecom.Auth.Application/Ecom.Auth.Application.csproj", "Ecom.Auth.Application/"]
COPY ["Ecom.Auth.Domain/Ecom.Auth.Domain.csproj", "Ecom.Auth.Domain/"]
COPY ["Ecom.Auth.Infrastructure/Ecom.Auth.Infrastructure.csproj", "Ecom.Auth.Infrastructure/"]
RUN dotnet restore "Ecom.Auth.API/Ecom.Auth.API.csproj"
COPY . .
RUN dotnet build "Ecom.Auth.API/Ecom.Auth.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Ecom.Auth.API/Ecom.Auth.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ecom.Auth.API.dll"]
