FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Ecom.Workflow.API/Ecom.Workflow.API.csproj", "Ecom.Workflow.API/"]
COPY ["Ecom.Workflow.Application/Ecom.Workflow.Application.csproj", "Ecom.Workflow.Application/"]
COPY ["Ecom.Workflow.Domain/Ecom.Workflow.Domain.csproj", "Ecom.Workflow.Domain/"]
COPY ["Ecom.Workflow.Infrastructure/Ecom.Workflow.Infrastructure.csproj", "Ecom.Workflow.Infrastructure/"]
RUN dotnet restore "Ecom.Workflow.API/Ecom.Workflow.API.csproj"
COPY . .
RUN dotnet build "Ecom.Workflow.API/Ecom.Workflow.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Ecom.Workflow.API/Ecom.Workflow.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ecom.Workflow.API.dll"]
