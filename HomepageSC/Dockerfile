FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["HomepageSC/HomepageSC.csproj", "HomepageSC/"]
RUN dotnet restore "HomepageSC/HomepageSC.csproj"
COPY . .
RUN dotnet test
WORKDIR "/src/HomepageSC"
RUN dotnet build "HomepageSC.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HomepageSC.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HomepageSC.dll"]
