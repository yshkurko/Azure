# Один Dockerfile для всех .NET-сервисов. Сборка из папки src:
#   docker build -f Dockerfile --build-arg PROJECT=Shop.Catalog.Api -t shop-catalog-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT
WORKDIR /src

# Сначала только csproj — слой restore кэшируется, пока не меняются зависимости.
COPY Shop.Shared/Shop.Shared.csproj Shop.Shared/
COPY Shop.Orders.Data/Shop.Orders.Data.csproj Shop.Orders.Data/
COPY Shop.Catalog.Api/Shop.Catalog.Api.csproj Shop.Catalog.Api/
COPY Shop.Orders.Api/Shop.Orders.Api.csproj Shop.Orders.Api/
COPY Shop.OrderProcessor/Shop.OrderProcessor.csproj Shop.OrderProcessor/
RUN dotnet restore ${PROJECT}/${PROJECT}.csproj

COPY . .
RUN dotnet publish ${PROJECT}/${PROJECT}.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
ARG PROJECT
ENV APP_DLL=${PROJECT}.dll
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet $APP_DLL"]
