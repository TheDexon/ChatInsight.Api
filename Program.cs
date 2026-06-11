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

// QuestPDF — бесплатная Community-лицензия
QuestPDF.Settings.License = LicenseType.Community;

// --- Конфигурация ---
builder.Services.Configure<EmotionAnalysisOptions>(
    builder.Configuration.GetSection(EmotionAnalysisOptions.SectionName));
builder.Services.Configure<OllamaOptions>(
    builder.Configuration.GetSection(OllamaOptions.SectionName));

// --- База данных (PostgreSQL) ---
builder.Services.AddDbContext<ChatInsightDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres")));

// --- MVC / Swagger ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- CORS (под будущий React-фронт) ---
const string FrontendCors = "frontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCors, policy =>
        policy.WithOrigins(
                "http://localhost:5173",  // Vite
                "http://localhost:3000")  // CRA / Next
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// --- Парсер ---
builder.Services.AddScoped<TelegramParser>();

// --- Текстовые сервисы ---
builder.Services.AddScoped<TelegramTextExtractor>();
builder.Services.AddScoped<TextCleaner>();

// --- Импорт / загрузка из БД ---
builder.Services.AddScoped<ChatImportService>();
builder.Services.AddScoped<ChatContextLoader>();

// --- Аналитика ---
builder.Services.AddScoped<StatisticsService>();
builder.Services.AddScoped<TextAnalyticsService>();
builder.Services.AddScoped<TopicService>();
builder.Services.AddScoped<EmotionService>();
builder.Services.AddScoped<ResponseService>();
builder.Services.AddScoped<InitiativeService>();
builder.Services.AddScoped<TimelineService>();
builder.Services.AddScoped<RelationshipService>();
builder.Services.AddScoped<ReportService>();

// --- Отчёты ---
builder.Services.AddScoped<PdfReportService>();

// --- AI (Ollama) ---
builder.Services.AddHttpClient<OllamaClient>();
builder.Services.AddScoped<AiInsightService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(FrontendCors);
app.UseAuthorization();
app.MapControllers();

app.Run();
