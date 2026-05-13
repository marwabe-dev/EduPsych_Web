using EduPsych_Web.Data;
using EduPsych_Web.Hubs; // تأكد من وجود هذا السطر ليتعرف على مجلد الـ Hubs
using Microsoft.EntityFrameworkCore;

// حل مشكلة توافق الوقت مع PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// 1️⃣ إعداد قاعدة البيانات PostgreSQL
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
    //options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2️⃣ إضافة خدمة الـ Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 🆕 إعدادات الـ HttpClient للاتصال بالـ APIs
builder.Services.AddHttpClient();


// 🟢 [إضافة جديدة] تفعيل خدمة SignalR للمحادثات الفورية
builder.Services.AddSignalR();

// 3️⃣ إضافة خدمات الـ MVC والوصول للسياق
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// إعدادات البيئة
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// الترتيب الصحيح لعمليات الـ Middleware
app.UseStaticFiles();
app.UseRouting();

// 4️⃣ تفعيل الـ Session والتحقق من الهوية
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ---------------------------------------------------------
// 5️⃣ المسارات (Routes)
// ---------------------------------------------------------

// 🟢 [إضافة جديدة] تحديد مسار الـ ChatHub
app.MapHub<ChatHub>("/chatHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();