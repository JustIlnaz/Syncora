FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Syncora.Api/Syncora.Api.csproj", "Syncora.Api/"]
RUN dotnet restore "Syncora.Api/Syncora.Api.csproj"
COPY src/Syncora.Api/ Syncora.Api/
RUN dotnet publish "Syncora.Api/Syncora.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Syncora.Api.dll"]
