FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/jobs/PayTR.PosSelection.Jobs/PayTR.PosSelection.Jobs.csproj", "src/jobs/PayTR.PosSelection.Jobs/"]
COPY ["src/infra/PayTR.PosSelection.Infrastructure/PayTR.PosSelection.Infrastructure.csproj", "src/infra/PayTR.PosSelection.Infrastructure/"]
RUN dotnet restore "src/jobs/PayTR.PosSelection.Jobs/PayTR.PosSelection.Jobs.csproj"
COPY . .
WORKDIR "/src/src/jobs/PayTR.PosSelection.Jobs"
RUN dotnet build "./PayTR.PosSelection.Jobs.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PayTR.PosSelection.Jobs.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PayTR.PosSelection.Jobs.dll"]
