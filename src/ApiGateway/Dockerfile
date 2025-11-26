FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://0.0.0.0:5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Shared/Common/Shared.Common.csproj", "src/Shared/Common/"]
COPY ["src/ApiGateway/ApiGateway.csproj", "src/ApiGateway/"]
COPY ["src/Shared/Grpc/Shared.Grpc.csproj", "src/Shared/Grpc/"]
COPY ["src/Shared/API/Shared.API.csproj", "src/Shared/API/"]
RUN dotnet restore "src/ApiGateway/ApiGateway.csproj"

COPY . .
WORKDIR "/src/src/ApiGateway"
RUN dotnet build "ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ApiGateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ApiGateway.dll"]