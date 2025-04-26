##########################  BUILD  ##########################
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# копируем только csproj и восстанавливаем
COPY DnsProxy.csproj ./
RUN dotnet restore DnsProxy.csproj

# копируем всё остальное
COPY . .

# публикуем в /out  (не попадёт под игнор bin/obj)
RUN dotnet publish DnsProxy.csproj -c Release \
    -o /out \
    -p:UseAppHost=false

# DEBUG: выводим список файлов
RUN echo "=== publish output ===" && ls -R /out

##########################  RUNTIME ##########################
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# копируем готовую папку
COPY --from=build /out .

EXPOSE 8080/tcp
EXPOSE 53/udp

ENTRYPOINT ["dotnet","DnsProxy.dll"]
