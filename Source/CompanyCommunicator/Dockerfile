# Stage 1 : Define the base image
# This will setup the image that will be used for production(aliased as "base") and
# install node js.
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
RUN apt-get update -yq \
    && apt-get install curl gnupg -yq \
    && curl -sL https://deb.nodesource.com/setup_10.x | bash \
    && apt-get install nodejs -yq

# Stage 2: Build and publish the code
# Uses an sdk image(aliased as "build"), installs node js, copies our project code into a working
# directory, restores Nuget packages, builds the code and publishes it to a directory names publish.
FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
RUN apt-get update -yq \
    && apt-get install curl gnupg -yq \
    && curl -sL https://deb.nodesource.com/setup_10.x | bash \
    && apt-get install nodejs -yq
WORKDIR /src
COPY ["Microsoft.Teams.Apps.CompanyCommunicator/Microsoft.Teams.Apps.CompanyCommunicator.csproj", "Microsoft.Teams.Apps.CompanyCommunicator/"]
COPY ["Microsoft.Teams.Apps.CompanyCommunicator.Common/Microsoft.Teams.Apps.CompanyCommunicator.Common.csproj", "Microsoft.Teams.Apps.CompanyCommunicator.Common/"]
RUN dotnet restore "Microsoft.Teams.Apps.CompanyCommunicator/Microsoft.Teams.Apps.CompanyCommunicator.csproj"
COPY . .
WORKDIR "/src/Microsoft.Teams.Apps.CompanyCommunicator"
RUN dotnet build "Microsoft.Teams.Apps.CompanyCommunicator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Microsoft.Teams.Apps.CompanyCommunicator.csproj" -c Release -o /app/publish

# Stage 3: Build and publish the code
# This copies the publish directory into production image's working directory and 
# defines the dotnet command to run once container is running.
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Microsoft.Teams.Apps.CompanyCommunicator.dll"]