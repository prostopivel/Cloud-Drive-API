FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5001
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Services/Auth/Auth.API/Auth.API.csproj", "Services/Auth/Auth.API/"]
COPY ["Services/Auth/Auth.Core/Auth.Core.csproj", "Services/Auth/Auth.Core/"]
COPY ["Services/Auth/Auth.Infrastructure/Auth.Infrastructure.csproj", "Services/Auth/Auth.Infrastructure/"]
COPY ["Services/Auth/Auth.Grpc/Auth.Grpc.csproj", "Services/Auth/Auth.Grpc/"]
COPY ["Shared/Common/Shared.Common.csproj", "Shared/Common/"]
COPY ["Shared/Grpc/Shared.Grpc.csproj", "Shared/Grpc/"]
RUN dotnet restore "Services/Auth/Auth.API/Auth.API.csproj"

COPY . .
WORKDIR "/src/Services/Auth/Auth.API"
RUN dotnet build "Auth.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Auth.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Auth.API.dll"]