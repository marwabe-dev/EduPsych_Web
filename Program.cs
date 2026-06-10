using EduPsych_Web.Data;
using EduPsych_Web.Hubs;
using Microsoft.EntityFrameworkCore;

// حل مشكلة توافق الوقت مع PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------------------------
// 1️⃣ [تعديل جوهري] جلب سلسلة الاتصال المتوافقة مع جهازكِ المحلي ومع سيرفر Render أونلاين
// ----------------------------------------------------------------------------------
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings.DefaultConnection")
                      ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
// ----------------------------------------------------------------------------------

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

// 🟢 تفعيل خدمة SignalR للمحادثات الفورية
builder.Services.AddSignalR();

// 3️⃣ إضافة خدمات الـ MVC والوصول للسياق
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ----------------------------------------------------------------------------------
// ⚙️ [إضافة سحرية للـ MVP] تطبيق الهجرات تلقائياً وإنشاء الجداول على Neon عند إقلاع السيرفر
// ----------------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        // يتجاوز الخطأ إذا كانت الجداول موجودة مسبقاً لمنع انهيار التطبيق
    }
}
// ----------------------------------------------------------------------------------

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

// 5️⃣ المسارات (Routes)
app.MapHub<ChatHub>("/chatHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();