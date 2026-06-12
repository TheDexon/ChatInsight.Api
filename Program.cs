using ChatInsight.Api.Configuration;
using ChatInsight.Api.Data;
using ChatInsight.Api.Parsers;
using ChatInsight.Api.Reports;
using ChatInsight.Api.Services.Ai;
using ChatInsight.Api.Services.Analytics;
using ChatInsight.Api.Services.Import;
using ChatInsight.Api.Services.Text;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.Configure<EmotionAnalysisOptions>(
    builder.Configuration.GetSection(EmotionAnalysisOptions.SectionName));
builder.Services.Configure<OllamaOptions>(
    builder.Configuration.GetSection(OllamaOptions.SectionName));

builder.Services.AddDbContext<ChatInsightDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

const string FrontendCors = "frontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCors, policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
            .AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddScoped<TelegramParser>();
builder.Services.AddScoped<TelegramTextExtractor>();
builder.Services.AddScoped<TextCleaner>();

builder.Services.AddScoped<ChatImportService>();
builder.Services.AddScoped<ChatContextLoader>();

builder.Services.AddScoped<StatisticsService>();
builder.Services.AddScoped<TextAnalyticsService>();
builder.Services.AddScoped<TopicService>();
builder.Services.AddScoped<EmotionService>();
builder.Services.AddScoped<ResponseService>();
builder.Services.AddScoped<InitiativeService>();
builder.Services.AddScoped<TimelineService>();
builder.Services.AddScoped<RelationshipService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<ComparisonService>();

builder.Services.AddScoped<PdfReportService>();

builder.Services.AddHttpClient<OllamaClient>();
builder.Services.AddScoped<AiInsightService>();
builder.Services.AddScoped<AiInsightCacheService>();
builder.Services.AddScoped<PersonalityService>();
builder.Services.AddScoped<PersonalityCacheService>();
builder.Services.AddScoped<LifeTimelineService>();
builder.Services.AddScoped<LifeTimelineCacheService>();

builder.Services.AddSingleton<AiJobQueue>();
builder.Services.AddScoped<AiJobService>();
builder.Services.AddHostedService<AiJobWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(FrontendCors);
app.UseAuthorization();
app.MapControllers();

app.Run();
