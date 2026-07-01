# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["UserRolePortal/UserRolePortal.csproj", "UserRolePortal/"]
RUN dotnet restore "UserRolePortal/UserRolePortal.csproj"

# Copy the rest of the code and build
COPY . .
WORKDIR "/src/UserRolePortal"
RUN dotnet publish "UserRolePortal.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Run the application
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose port 8080 (Render's default port for web services)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "UserRolePortal.dll"]