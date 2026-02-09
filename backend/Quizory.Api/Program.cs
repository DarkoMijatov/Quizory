using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Quizory.Api.Data;
using Quizory.Api.Services;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("quizory"));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRequestContextAccessor, HeaderRequestContextAccessor>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IScoringService, ScoringService>();
builder.Services.AddScoped<ITextLocalizer, DictionaryTextLocalizer>();

builder.Services.AddLocalization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var supportedCultures = new[] { new CultureInfo("sr"), new CultureInfo("en") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("sr"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SeedData.InitializeAsync(db);
}

app.MapControllers();

app.Run();
