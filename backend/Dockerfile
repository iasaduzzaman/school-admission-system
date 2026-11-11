FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files for layer caching
COPY SchoolAdmission.slnx .
COPY SchoolAdmission.Domain/SchoolAdmission.Domain.csproj SchoolAdmission.Domain/
COPY SchoolAdmission.Application/SchoolAdmission.Application.csproj SchoolAdmission.Application/
COPY SchoolAdmission.Infrastructure/SchoolAdmission.Infrastructure.csproj SchoolAdmission.Infrastructure/
COPY SchoolAdmission.Api/SchoolAdmission.Api.csproj SchoolAdmission.Api/
COPY SchoolAdmission.Tests/SchoolAdmission.Tests.csproj SchoolAdmission.Tests/

RUN dotnet restore SchoolAdmission.slnx

# Copy full source and publish
COPY . .
WORKDIR /src/SchoolAdmission.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && \
    apt-get install -y --no-install-recommends libgssapi-krb5-2 && \
    rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SchoolAdmission.Api.dll"]
