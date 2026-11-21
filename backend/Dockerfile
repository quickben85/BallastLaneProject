FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG API_PROJECT
WORKDIR /src
COPY src/ ./src/
RUN dotnet publish "src/${API_PROJECT}/${API_PROJECT}.csproj" -c Release -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ARG API_PROJECT
ENV API_DLL="${API_PROJECT}.dll"
WORKDIR /app
COPY --from=build /publish/ ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet \"$API_DLL\""]
