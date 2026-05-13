FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .

# البحث عن المشروع وعمل الـ Restore والـ Publish تلقائياً
RUN dotnet restore $(find . -name "*.csproj")
RUN dotnet publish $(find . -name "*.csproj") -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

# تأكدي أن هذا الاسم يطابق الـ DLL النهائي الخاص بكِ
ENTRYPOINT ["dotnet", "EduPsych_Web.dll"]