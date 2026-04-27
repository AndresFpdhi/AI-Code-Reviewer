FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CodeReviewer.Api/CodeReviewer.Api.csproj CodeReviewer.Api/
COPY CodeReviewer.Core/CodeReviewer.Core.csproj CodeReviewer.Core/
COPY CodeReviewer.Data/CodeReviewer.Data.csproj CodeReviewer.Data/
RUN dotnet restore CodeReviewer.Api/CodeReviewer.Api.csproj

COPY CodeReviewer.Api/ CodeReviewer.Api/
COPY CodeReviewer.Core/ CodeReviewer.Core/
COPY CodeReviewer.Data/ CodeReviewer.Data/
RUN dotnet publish CodeReviewer.Api/CodeReviewer.Api.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000

ENTRYPOINT ["dotnet", "CodeReviewer.Api.dll"]
