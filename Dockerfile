FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY Practica2.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish Practica2.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

COPY --from=build /app/publish ./

ENTRYPOINT ["sh", "-c", "dotnet Practica2.dll --urls http://0.0.0.0:${PORT:-8080}"]