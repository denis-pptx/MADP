﻿
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using NuGet.Packaging;
using System.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

//builder.Services.AddScoped<IToolCategoryService, MemoryToolCategoryService>();
//builder.Services.AddScoped<IToolService, MemoryToolService>();

UriData uriData = builder.Configuration.GetSection("UriData").Get<UriData>()!;

builder.Services.AddHttpClient<IToolService, ApiToolService>(opt => opt.BaseAddress = new Uri(uriData.ApiUri));
builder.Services.AddHttpClient<IToolCategoryService, ApiToolCategoryService>(opt => opt.BaseAddress = new Uri(uriData.ApiUri));

builder.Services.AddAuthentication(opt =>
{
    opt.DefaultScheme = "cookie";
    opt.DefaultChallengeScheme = "oidc";
})
    .AddCookie("cookie")
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = builder.Configuration["InteractiveServiceSettings:AuthorityUrl"];
        options.ClientId = builder.Configuration["InteractiveServiceSettings:ClientId"];
        options.ClientSecret = builder.Configuration["InteractiveServiceSettings:ClientSecret"];

        // Получить Claims пользователя
        options.GetClaimsFromUserInfoEndpoint = true;

        options.ResponseType = "code";
        options.ResponseMode = "query";
        options.SaveTokens = true;

        options.Scope.AddRange(builder.Configuration.GetSection("InteractiveServiceSettings:Scopes").Get<string[]>());
        options.ClaimActions.MapUniqueJsonKey("role", "role");
    });


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireClaim("role", "admin"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages().RequireAuthorization();

app.Run();
