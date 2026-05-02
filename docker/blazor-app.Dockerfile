FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Mms.sln .
COPY Directory.Build.props .
COPY src/Mms.Domain/Mms.Domain.csproj src/Mms.Domain/
COPY src/Mms.Application/Mms.Application.csproj src/Mms.Application/
COPY src/Mms.Infrastructure/Mms.Infrastructure.csproj src/Mms.Infrastructure/
COPY src/Mms.Web/Mms.Web.csproj src/Mms.Web/
COPY src/Mms.PrintAgent/Mms.PrintAgent.csproj src/Mms.PrintAgent/
COPY tests/Mms.UnitTests/Mms.UnitTests.csproj tests/Mms.UnitTests/
COPY tests/Mms.IntegrationTests/Mms.IntegrationTests.csproj tests/Mms.IntegrationTests/
COPY tests/Mms.E2ETests/Mms.E2ETests.csproj tests/Mms.E2ETests/

RUN dotnet restore Mms.sln

COPY . .
RUN dotnet publish src/Mms.Web/Mms.Web.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Install LibreOffice for PDF conversion
RUN apt-get update \
    && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
        libreoffice-writer \
        libreoffice-common \
        default-jre \
        fonts-dejavu-core \
        fonts-freefont-ttf \
    && rm -rf /var/lib/apt/lists/*

# Create writable dirs BEFORE switching to non-root user
RUN mkdir -p /app/keys /app/wwwroot/uploads \
    && chown -R app:app /app/keys /app/wwwroot/uploads

# Run as non-root user
USER app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Mms.Web.dll"]
