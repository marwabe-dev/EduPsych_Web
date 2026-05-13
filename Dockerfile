FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
# الانتقال للمجلد الذي يحتوي على ملف الـ .csproj
WORKDIR /src/EduPsych_Web/EduPsych_Web
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EduPsych_Web.dll"]