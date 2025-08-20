# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["FileBasedBlog.csproj", "./"]
RUN dotnet restore "./FileBasedBlog.csproj"

# Copy the rest of the source code and publish
COPY . .
RUN dotnet publish "./FileBasedBlog.csproj" -c Release -o /app/publish

# Stage 2: Create runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Set environment variable for ASP.NET Core to listen on port 8080
ENV ASPNETCORE_URLS=http://+:8080

# Copy published output from build stage
COPY --from=build /app/publish .

# Copy your file-based content folder
COPY content /app/content

# Expose port 8080
EXPOSE 8080

# Start the application
ENTRYPOINT ["dotnet", "FileBasedBlog.dll"]
