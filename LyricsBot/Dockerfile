FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build // Завантаження на сервер інструменти програміста версії і назва етапу "build"
WORKDIR /src // Створення на сервері папку src
COPY . . //	Копіювання всіх файлів з поточної папки на сервері в папку src
RUN dotnet publish "LyricsBot.csproj" -c Release -o /app/publish // бере мій код у готову програму і кладе в папку /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime // Завантаження на сервер образу з готовим середовищем для запуску програм на dotnet і назва етапу "runtime"
WORKDIR /app // Створення на сервері папки app
COPY --from=build /app/publish . // Копіювання всіх файлів з папки /app/publish на сервері в папку app
ENTRYPOINT ["dotnet", "LyricsBot.dll"] // Запускає файл