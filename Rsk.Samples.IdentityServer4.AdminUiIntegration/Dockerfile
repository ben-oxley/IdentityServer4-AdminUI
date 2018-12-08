FROM microsoft/aspnetcore:2.0 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0 AS build
WORKDIR /src
COPY ["Rsk.Samples.IdentityServer4.AdminUiIntegration/Rsk.Samples.IdentityServer4.AdminUiIntegration.csproj", "Rsk.Samples.IdentityServer4.AdminUiIntegration/"]
RUN dotnet restore "Rsk.Samples.IdentityServer4.AdminUiIntegration/Rsk.Samples.IdentityServer4.AdminUiIntegration.csproj"
COPY . .
WORKDIR "/src/Rsk.Samples.IdentityServer4.AdminUiIntegration"
RUN dotnet build "Rsk.Samples.IdentityServer4.AdminUiIntegration.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Rsk.Samples.IdentityServer4.AdminUiIntegration.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .

ENV ASPNETCORE_ENVIRONMENT Production
ENV ASPNETCORE_URLS http://*:5003

ENTRYPOINT ["dotnet", "Rsk.Samples.IdentityServer4.AdminUiIntegration.dll"]