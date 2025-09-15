FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["FileBasedBlog.csproj", "./"]
RUN dotnet restore "./FileBasedBlog.csproj"
COPY . .
RUN dotnet publish "./FileBasedBlog.csproj" -c Release -o /app/publish


FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY content /app/content
VOLUME [ "/app/content/posts", "/app/content/users", "/app/content/categories", "/app/content/tags", "/app/content/role-requests" ]
ENTRYPOINT ["dotnet", "FileBasedBlog.dll"]
