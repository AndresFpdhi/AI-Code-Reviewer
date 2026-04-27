using CodeReviewer.Api.BackgroundJobs;
using CodeReviewer.Api.Middleware;
using CodeReviewer.Core.Options;
using CodeReviewer.Core.Services;
using CodeReviewer.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<GroqOptions>(builder.Configuration.GetSection(GroqOptions.SectionName));
builder.Services.Configure<GitHubAppOptions>(builder.Configuration.GetSection(GitHubAppOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=reviews.db";
builder.Services.AddDbContext<AppDbContext>(opts => opts.UseSqlite(connectionString));

builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IPromptBuilder, PromptBuilder>();
builder.Services.AddScoped<IReviewService, ReviewService>();

builder.Services.AddHttpClient<IGroqClient, GroqClient>();
builder.Services.AddHttpClient<IGitHubAppClient, GitHubAppClient>();

builder.Services.AddSingleton<IReviewQueue, ReviewQueue>();
builder.Services.AddHostedService<ReviewQueueProcessor>();

var corsOrigins = builder.Configuration.GetValue<string>("Cors:AllowedOrigins")?.Split(',')
    ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api/github/webhook"),
    branch => branch.UseMiddleware<GitHubSignatureMiddleware>());

app.MapControllers();

app.Run();

public partial class Program { }
