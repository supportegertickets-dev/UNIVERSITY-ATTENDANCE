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

# Copy published FrontendServer to /app/frontend-server
COPY --from=build /app/publish-frontend ./frontend-server

# Copy frontend static files to /app/frontend (accessible from FrontendServer)
COPY --from=build /src/frontend ./frontend

# Copy published AttendanceAPI to /app/api
COPY --from=build /app/publish-api ./api

EXPOSE 5069 3000

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production

# Use startup script to run both services, or just run FrontendServer
# For Railway: run FrontendServer which serves static files
ENTRYPOINT ["dotnet", "frontend-server/FrontendServer.dll"]
