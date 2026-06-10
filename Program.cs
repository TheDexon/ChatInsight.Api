using ChatInsight.Api.Configuration;
using ChatInsight.Api.Parsers;
using ChatInsight.Api.Services.Analytics;
using ChatInsight.Api.Services.Text;

var builder = WebApplication.CreateBuilder(args);

// --- Конфигурация ---
builder.Services.Configure<EmotionAnalysisOptions>(
    builder.Configuration.GetSection(EmotionAnalysisOptions.SectionName));

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
