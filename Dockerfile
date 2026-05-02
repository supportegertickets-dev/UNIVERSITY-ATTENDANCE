# Multi-stage build for AttendanceAPI
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
RUN dotnet publish "AttendanceAPI/AttendanceAPI.csproj" -c Release -o /app/publish
RUN dotnet publish "FrontendServer/FrontendServer.csproj" -c Release -o /app/publish-frontend

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 5069 3000

# Copy published AttendanceAPI
COPY --from=build /app/publish ./api

# Copy published FrontendServer
COPY --from=build /app/publish-frontend ./frontend-app

# Copy frontend static files
COPY --from=build /src/frontend ./frontend

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5069
ENV ASPNETCORE_ENVIRONMENT=Production

# Start AttendanceAPI
ENTRYPOINT ["dotnet", "api/AttendanceAPI.dll"]
