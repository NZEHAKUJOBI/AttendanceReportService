# ------------------------------
# Stage 1: Build
# ------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy everything and restore dependencies
COPY . .
RUN dotnet restore

# Build and publish the app to /out
RUN dotnet publish -c Release -o out

# ------------------------------
# Stage 2: Runtime
# ------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published files
COPY --from=build /app/out .

# Expose port 8080
EXPOSE 8080

# Start the app
ENTRYPOINT ["dotnet", "AttendanceReportService.dll"]
