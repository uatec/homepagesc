FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["HomepageSidecar/HomepageSidecar.csproj", "HomepageSidecar/"]
RUN dotnet restore "HomepageSidecar/HomepageSidecar.csproj"
RUN dotnet test
COPY . .
WORKDIR "/src/HomepageSidecar"
RUN dotnet build "HomepageSidecar.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HomepageSidecar.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HomepageSidecar.dll"]
