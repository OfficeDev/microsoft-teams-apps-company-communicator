# Stage 1 : Define the base image
# This will setup the image that will be used for production(aliased as "base").
FROM mcr.microsoft.com/azure-functions/dotnet:3.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 80

# Stage 2: Build and publish the code
# Uses an sdk image(aliased as "build"), copies our project code into a working directory,
# restores Nuget packages, builds the code and publishes it to a directory names publish.
FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func/Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.csproj", "Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func/"]
COPY ["Microsoft.Teams.Apps.CompanyCommunicator.Common/Microsoft.Teams.Apps.CompanyCommunicator.Common.csproj", "Microsoft.Teams.Apps.CompanyCommunicator.Common/"]
RUN dotnet restore "Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func/Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.csproj"
COPY . .
WORKDIR "/src/Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func"
RUN dotnet build "Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.csproj" -c Release -o /app/publish

# Stage 3: Build and publish the code
# This copies the publish directory into production image's working directory.
FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true