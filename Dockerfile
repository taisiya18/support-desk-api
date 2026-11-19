# SDK и runtime закреплены по версии и digest официального реестра Microsoft.
# Digest нельзя переставить на другой образ, в отличие от тега.
FROM mcr.microsoft.com/dotnet/sdk:10.0.401@sha256:e70cdb7f80b0348f5cb85f19a8f670fca061f033d57eed12fa003d58b0e06317 AS build
WORKDIR /source

# Сначала только файлы, нужные для восстановления пакетов,
# чтобы слой с пакетами не пересобирался при правке кода.
COPY global.json Directory.Build.props ./
COPY src/SupportDesk.Api/SupportDesk.Api.csproj src/SupportDesk.Api/packages.lock.json src/SupportDesk.Api/
RUN dotnet restore src/SupportDesk.Api/SupportDesk.Api.csproj --locked-mode

COPY src/SupportDesk.Api/ src/SupportDesk.Api/
RUN dotnet publish src/SupportDesk.Api/SupportDesk.Api.csproj \
    --configuration Release --no-restore --output /app -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.12@sha256:222759b391a1aaf241166672c8f99b2d4ada452e7b5319f3c6e8f265a37b5ad4 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Каталог для файла базы. Рабочий каталог принадлежит root и на запись закрыт,
# поэтому база вынесена отдельно и передана пользователю приложения.
RUN mkdir -p /data && chown $APP_UID /data
ENV ConnectionStrings__AppDb="Data Source=/data/supportdesk.db"
ENV ASPNETCORE_HTTP_PORTS=8080

# Приложение работает не от имени root.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "SupportDesk.Api.dll"]
