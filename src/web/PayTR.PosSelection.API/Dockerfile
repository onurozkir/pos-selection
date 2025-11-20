FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/web/PayTR.PosSelection.API/PayTR.PosSelection.API.csproj", "src/web/PayTR.PosSelection.API/"]
RUN dotnet restore "src/web/PayTR.PosSelection.API/PayTR.PosSelection.API.csproj"
COPY . .
WORKDIR "/src/src/web/PayTR.PosSelection.API"
RUN dotnet build "./PayTR.PosSelection.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PayTR.PosSelection.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PayTR.PosSelection.API.dll"]
