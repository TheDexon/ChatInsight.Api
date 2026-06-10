using ChatInsight.Api.Parsers;
using ChatInsight.Api.Services.Analytics;
using ChatInsight.Api.Services.Text;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<EmotionService>();
builder.Services.AddScoped<TopicService>();
builder.Services.AddScoped<TextCleaner>();
builder.Services.AddScoped<TextAnalyticsService>();
builder.Services.AddScoped<TelegramTextExtractor>();
builder.Services.AddScoped<RelationshipService>();
builder.Services.AddScoped<InitiativeService>();
builder.Services.AddScoped<ResponseService>();
builder.Services.AddScoped<TimelineService>();
builder.Services.AddControllers();
builder.Services.AddScoped<StatisticsService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<TelegramParser>();
builder.Services.AddScoped<StatisticsService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();