# One image for the whole application: the API serves the built front from wwwroot.
# Three stages, so the final image carries neither the SDK nor Node nor the sources.

# 1. Front: type check, then production build.
FROM node:24-alpine AS web
WORKDIR /web

# Dependencies first: this layer is reused as long as the lock file does not change.
COPY web/package.json web/package-lock.json ./
RUN npm ci

COPY web/ ./

# Social networks need absolute links: the public address is known at build time only.
ARG SITE_URL=""
ENV VITE_SITE_URL=$SITE_URL
RUN npm run build

# 2. API: restore, then publish in Release.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Kairos.Domain/Kairos.Domain.csproj src/Kairos.Domain/
COPY src/Kairos.Application/Kairos.Application.csproj src/Kairos.Application/
COPY src/Kairos.Infrastructure/Kairos.Infrastructure.csproj src/Kairos.Infrastructure/
COPY src/Kairos.Api/Kairos.Api.csproj src/Kairos.Api/
RUN dotnet restore src/Kairos.Api/Kairos.Api.csproj

COPY src/ src/
RUN dotnet publish src/Kairos.Api/Kairos.Api.csproj --configuration Release --no-restore --output /app

# 3. Runtime: the ASP.NET image only, run by its unprivileged user.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=api /app ./
COPY --from=web /web/dist ./wwwroot

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "Kairos.Api.dll"]
