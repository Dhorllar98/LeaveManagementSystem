# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and individual project files to restore dependencies correctly
COPY ["LeaveManagementSystem.sln", "./"]
COPY ["LeaveManagement.Domain/LeaveManagement.Domain.csproj", "LeaveManagement.Domain/"]
COPY ["LeaveManagement.Application/LeaveManagement.Application.csproj", "LeaveManagement.Application/"]
COPY ["LeaveManagement.Infrastructure/LeaveManagement.Infrastructure.csproj", "LeaveManagement.Infrastructure/"]
COPY ["LeaveManagement.Api./LeaveManagement.Api..csproj", "LeaveManagement.Api./"]

RUN dotnet restore

# Copy all source files and build the API project
COPY . .
WORKDIR "/src/LeaveManagement.Api"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime image for production execution
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "LeaveManagement.Api.dll"]