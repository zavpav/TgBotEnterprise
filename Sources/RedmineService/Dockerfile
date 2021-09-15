#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["RedmineService/RedmineService.csproj", "RedmineService/"]
RUN dotnet restore "RedmineService/RedmineService.csproj"
COPY . .
WORKDIR "/src/RedmineService"
RUN dotnet build "RedmineService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RedmineService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RedmineService.dll"]