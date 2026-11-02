# Build context is the repository root (see docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so `dotnet restore` is cached until a dependency changes.
COPY Directory.Build.props ./
COPY src/LeaveManagement.Domain/LeaveManagement.Domain.csproj src/LeaveManagement.Domain/
COPY src/LeaveManagement.Application/LeaveManagement.Application.csproj src/LeaveManagement.Application/
COPY src/LeaveManagement.Infrastructure/LeaveManagement.Infrastructure.csproj src/LeaveManagement.Infrastructure/
COPY src/LeaveManagement.Api/LeaveManagement.Api.csproj src/LeaveManagement.Api/
RUN dotnet restore src/LeaveManagement.Api/LeaveManagement.Api.csproj

COPY src/ src/
RUN dotnet publish src/LeaveManagement.Api/LeaveManagement.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Run as the non-root "app" user that ships with the .NET images.
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "LeaveManagement.Api.dll"]
