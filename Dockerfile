# Multi-stage build for AttendanceAPI and FrontendServer
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["AttendanceAPI/AttendanceAPI.csproj", "AttendanceAPI/"]
COPY ["FrontendServer/FrontendServer.csproj", "FrontendServer/"]
RUN dotnet restore "AttendanceAPI/AttendanceAPI.csproj"
RUN dotnet restore "FrontendServer/FrontendServer.csproj"

# Copy source code
COPY . .

# Build
RUN dotnet build "AttendanceAPI/AttendanceAPI.csproj" -c Release -o /app/build
RUN dotnet build "FrontendServer/FrontendServer.csproj" -c Release -o /app/build-frontend

# Publish
RUN dotnet publish "AttendanceAPI/AttendanceAPI.csproj" -c Release -o /app/publish-api
RUN dotnet publish "FrontendServer/FrontendServer.csproj" -c Release -o /app/publish-frontend

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copy published AttendanceAPI to /app
COPY --from=build /app/publish-api ./

# Copy published FrontendServer to /app/frontend-server
COPY --from=build /app/publish-frontend ./frontend-server

# Copy frontend static files to /app/frontend-server/frontend (served by FrontendServer)
COPY --from=build /src/frontend ./frontend-server/frontend

# Copy frontend static files to /app/wwwroot (fallback)
COPY --from=build /src/frontend ./wwwroot

EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080

# Run AttendanceAPI (serves both API and static frontend files)
ENTRYPOINT ["dotnet", "AttendanceAPI.dll"]
