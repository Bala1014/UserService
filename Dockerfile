# Restore is a separate layer keyed only on the project files, so a code-only change reuses the
# cached package restore instead of re-downloading every dependency.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props UserService.slnx ./
COPY src/Racinglazing.User.Api/Racinglazing.User.Api.csproj src/Racinglazing.User.Api/
COPY src/Racinglazing.User.Application/Racinglazing.User.Application.csproj src/Racinglazing.User.Application/
COPY src/Racinglazing.User.Domain/Racinglazing.User.Domain.csproj src/Racinglazing.User.Domain/
COPY src/Racinglazing.User.Infrastructure/Racinglazing.User.Infrastructure.csproj src/Racinglazing.User.Infrastructure/
RUN dotnet restore src/Racinglazing.User.Api/Racinglazing.User.Api.csproj

COPY . .
RUN dotnet publish src/Racinglazing.User.Api/Racinglazing.User.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# The base image ships a built-in non-root user exposed via $APP_UID.
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .
USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "Racinglazing.User.Api.dll"]
