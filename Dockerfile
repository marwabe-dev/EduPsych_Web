# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# نسخ كل الملفات (بما في ذلك .sln و .csproj)
COPY . .

# تشغيل الـ Restore (سيقوم بالبحث عن ملفات المشروع تلقائياً)
RUN dotnet restore

# بناء المشروع
RUN dotnet publish -c Release -o /app/publish

# Run Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# ضبط المنفذ ليتوافق مع Render
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

# تأكدي أن اسم الـ DLL هنا يطابق اسم مشروعك تماماً
ENTRYPOINT ["dotnet", "EduPsych_web.dll"]