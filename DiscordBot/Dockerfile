FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim AS build-env
WORKDIR /App

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0-bullseye-slim
WORKDIR /App
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "DiscordBot.dll"]