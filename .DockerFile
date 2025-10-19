
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["AttendanceReportService/AttendanceReportService.csproj", "AttendanceReportService/"]
RUN dotnet restore "AttendanceReportService/AttendanceReportService.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/AttendanceReportService"
RUN dotnet publish -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AttendanceReportService.dll"]
