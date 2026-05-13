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

# Copy frontend files to AttendanceAPI wwwroot BEFORE publish
RUN mkdir -p AttendanceAPI/wwwroot && cp -r frontend/* AttendanceAPI/wwwroot/

# Build
RUN dotnet build "AttendanceAPI/AttendanceAPI.csproj" -c Release -o /app/build
RUN dotnet build "FrontendServer/FrontendServer.csproj" -c Release -o /app/build-frontend

# Publish
RUN dotnet publish "AttendanceAPI/AttendanceAPI.csproj" -c Release -o /app/publish-api
RUN dotnet publish "FrontendServer/FrontendServer.csproj" -c Release -o /app/publish-frontend

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copy published AttendanceAPI (includes wwwroot with frontend files)
COPY --from=build /app/publish-api ./

EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080

# Run AttendanceAPI
ENTRYPOINT ["dotnet", "AttendanceAPI.dll"]
