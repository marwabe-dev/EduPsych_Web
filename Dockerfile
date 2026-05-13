# المرحلة الأولى: بناء المشروع
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore EduPsych_Web/EduPsych_Web/EduPsych_Web.csproj
RUN dotnet publish EduPsych_Web/EduPsych_Web/EduPsych_Web.csproj -c Release -o /app/publish

# المرحلة الثانية: التشغيل
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EduPsych_Web.dll"]