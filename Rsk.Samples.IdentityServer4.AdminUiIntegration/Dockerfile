FROM mcr.microsoft.com/dotnet/core/aspnet:3.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

# Install .NET Core SDK
# https://github.com/aspnet/AspNetCore/issues/10833#issuecomment-499134467
ENV DOTNET_SDK_VERSION 2.2.401

RUN curl -SL --output dotnet.tar.gz https://dotnetcli.blob.core.windows.net/dotnet/Sdk/$DOTNET_SDK_VERSION/dotnet-sdk-$DOTNET_SDK_VERSION-linux-x64.tar.gz \
    && dotnet_sha512='08e1fcafa4f898c80ff5e88eeb40c7497b4f5651af3b8ec85f65a3daa2f1509a766d833477358d3ff83d179e014034ab0c48120847ef24736c8d1a5b67fec10b' \
    && echo "$dotnet_sha512 dotnet.tar.gz" | sha512sum -c - \
    && tar -zxf dotnet.tar.gz -C /usr/share/dotnet \
    && rm dotnet.tar.gz

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