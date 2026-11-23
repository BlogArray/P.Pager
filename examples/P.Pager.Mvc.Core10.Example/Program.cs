using P.Pager.Mvc.Core10.Example.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<MemberStore>();

// Pagers use the neutral theme (styled by _content/P.Pager.Mvc.Core/pager.css) unless their options say otherwise.
// To use Bootstrap classes everywhere instead:
// P.Pager.PagerSettings.DefaultTheme = P.Pager.PagerTheme.Bootstrap;

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
