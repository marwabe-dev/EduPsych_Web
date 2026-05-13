# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# نسخ ملفات المشروع أولاً
COPY . .

# تشغيل الـ Restore والتحقق من الإصدار
RUN dotnet restore

# بناء المشروع بالكامل
RUN dotnet publish -c Release -o /app/publish

# Run Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

# ضبط المنافذ لبيئة Render
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

# تأكدي أن اسم الملف يطابق مشروعك تماماً (EduPsych_Web.dll)
ENTRYPOINT ["dotnet", "EduPsych_Web.dll"]