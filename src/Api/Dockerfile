FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY src/Shared/BuildingBlocks/OrderFlow.BuildingBlocks.csproj src/Shared/BuildingBlocks/
COPY src/Modules/Ordering/Domain/OrderFlow.Ordering.Domain.csproj src/Modules/Ordering/Domain/
COPY src/Modules/Ordering/Application/OrderFlow.Ordering.Application.csproj src/Modules/Ordering/Application/
COPY src/Modules/Ordering/Infrastructure/OrderFlow.Ordering.Infrastructure.csproj src/Modules/Ordering/Infrastructure/
COPY src/Modules/Inventory/Domain/OrderFlow.Inventory.Domain.csproj src/Modules/Inventory/Domain/
COPY src/Modules/Inventory/Application/OrderFlow.Inventory.Application.csproj src/Modules/Inventory/Application/
COPY src/Modules/Inventory/Infrastructure/OrderFlow.Inventory.Infrastructure.csproj src/Modules/Inventory/Infrastructure/
COPY src/Api/OrderFlow.Api.csproj src/Api/
RUN dotnet restore src/Api/OrderFlow.Api.csproj
COPY src/ src/
RUN dotnet publish src/Api/OrderFlow.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderFlow.Api.dll"]