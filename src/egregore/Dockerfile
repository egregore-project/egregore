#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["/egregore/egregore.csproj", "egregore/"]
RUN dotnet restore "egregore/egregore.csproj"
COPY . .
WORKDIR "/src/egregore"
RUN dotnet build "egregore.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "egregore.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "egregore.dll"]