#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["JenkinsService/JenkinsService.csproj", "JenkinsService/"]
RUN dotnet restore "JenkinsService/JenkinsService.csproj"
COPY . .
WORKDIR "/src/JenkinsService"
RUN dotnet build "JenkinsService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "JenkinsService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JenkinsService.dll"]