using ChatInsight.Api.Analysis.Ai;
using ChatInsight.Api.Analysis.Emotion;
using ChatInsight.Api.Analysis.Personality;
using ChatInsight.Api.Analysis.Statistics;
using ChatInsight.Api.Analysis.Text;
using ChatInsight.Api.Analysis.Timeline;
using ChatInsight.Api.Domain;
using ChatInsight.Api.Services.Analytics;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ChatInsight.Api.Reports;

/// <summary>
/// Рендерит аналитику чата в PDF. Опционально добавляет AI-анализ и
/// портреты участников (если переданы).
/// </summary>
public class PdfReportService
{
    private readonly StatisticsService _statistics;
    private readonly TextAnalyticsService _text;
    private readonly EmotionService _emotion;
    private readonly TimelineService _timeline;

    public PdfReportService(
        StatisticsService statistics,
        TextAnalyticsService text,
        EmotionService emotion,
        TimelineService timeline)
    {
        _statistics = statistics;
        _text = text;
        _emotion = emotion;
        _timeline = timeline;
    }


    public byte[] Build(
        ChatAnalysisContext context,
        AiInsight? insight = null,
        List<PersonalityProfile>? personalities = null)
    {
        var stats = _statistics.Analyze(context);
        var text = _text.Analyze(context);
        var emotion = _emotion.Analyze(context);
        var timeline = _timeline.Analyze(context);

        var title = string.IsNullOrWhiteSpace(context.Export.Name)
            ? "Без названия"
            : context.Export.Name;

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Calibri));

                page.Header().Element(c => Header(c, title, context));
                page.Content().Element(c =>
                    Content(c, stats, text, emotion, timeline, insight, personalities));
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("ChatInsight • ");
                    t.Span($"сформировано {DateTime.Now:dd.MM.yyyy HH:mm}");
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void Header(IContainer container, string title, ChatAnalysisContext context)
    {
        container.Column(col =>
        {
            col.Item().Text("Аналитический отчёт")
                .FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
            col.Item().Text(title).FontSize(14).SemiBold();
            col.Item().Text(
                $"Период: {context.FirstMessageDate:dd.MM.yyyy} — " +
                $"{context.LastMessageDate:dd.MM.yyyy}   •   " +
                $"Сообщений: {context.TotalMessages}   •   " +
                $"Участников: {context.Participants.Count}")
                .FontSize(10).FontColor(Colors.Grey.Darken1);
            col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
    }

    private void Content(
        IContainer container,
        ChatStatistics stats,
        TextStatistics text,
        EmotionStatistics emotion,
        List<TimelineEvent> timeline,
        AiInsight? insight,
        List<PersonalityProfile>? personalities)
    {
        container.PaddingVertical(12).Column(col =>
        {
            col.Spacing(16);

            if (insight is not null && !string.IsNullOrWhiteSpace(insight.Summary))
                AiSection(col, insight);

            if (personalities is not null && personalities.Count > 0)
                PersonalitySection(col, personalities);

            Section(col, "Активность");
            col.Item().Text(
                $"Средняя длина сообщения: {stats.AverageMessageLength:F0} симв.   •   " +
                $"Самый активный час: {stats.MostActiveHour}:00");

            if (stats.MessagesByAuthor.Count > 0)
            {
                col.Item().Text("Сообщений по авторам:").SemiBold();
                foreach (var a in stats.MessagesByAuthor.OrderByDescending(x => x.Value))
                    col.Item().Text($"  • {a.Key}: {a.Value}");
            }

            Section(col, "Эмоции");
            col.Item().Text(
                $"Позитивных: {emotion.PositiveMessages}   •   " +
                $"Негативных: {emotion.NegativeMessages}   •   " +
                $"С матом: {emotion.ProfanityMessages}");
            col.Item().Text($"Индекс токсичности: {emotion.ToxicityScore}%");

            Section(col, "Частые слова");
            if (text.TopWords.Count == 0)
                col.Item().Text("Недостаточно данных.").Italic();
            else
                col.Item().Text(string.Join(",  ", text.TopWords.Take(15)));

            Section(col, "Ключевые события");
            if (timeline.Count == 0)
                col.Item().Text("Событий не выделено.").Italic();
            else
                foreach (var e in timeline)
                    col.Item().Text($"  • {e.Date:dd.MM.yyyy} — {e.Title}: {e.Description}");
        });
    }

    private static void AiSection(ColumnDescriptor col, AiInsight insight)
    {
        col.Item().Background(Colors.Blue.Lighten5).Padding(12).Column(box =>
        {
            box.Spacing(8);
            box.Item().Text("AI-анализ").FontSize(15).Bold().FontColor(Colors.Blue.Darken2);

            if (!string.IsNullOrWhiteSpace(insight.Summary))
            {
                box.Item().Text("Резюме").SemiBold();
                box.Item().Text(insight.Summary);
            }
            if (!string.IsNullOrWhiteSpace(insight.EmotionalTone))
            {
                box.Item().Text("Эмоциональный фон").SemiBold();
                box.Item().Text(insight.EmotionalTone);
            }
            if (insight.Topics.Count > 0)
            {
                box.Item().Text("Темы").SemiBold();
                box.Item().Text(string.Join(",  ", insight.Topics));
            }
            if (insight.Dynamics.Count > 0)
            {
                box.Item().Text("Динамика общения").SemiBold();
                foreach (var d in insight.Dynamics)
                    box.Item().Text($"  • {d}");
            }
            if (!string.IsNullOrWhiteSpace(insight.Model))
                box.Item().Text($"модель: {insight.Model}")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
        });
    }

    private static void PersonalitySection(
        ColumnDescriptor col, List<PersonalityProfile> people)
    {
        Section(col, "Портреты участников");

        foreach (var p in people)
        {
            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2)
                .Padding(10).Column(box =>
            {
                box.Spacing(4);
                box.Item().Text(p.Participant).FontSize(13).Bold();

                if (!string.IsNullOrWhiteSpace(p.Summary))
                    box.Item().Text(p.Summary);

                if (!string.IsNullOrWhiteSpace(p.CommunicationStyle))
                    box.Item().Text(t =>
                    {
                        t.Span("Стиль: ").SemiBold();
                        t.Span(p.CommunicationStyle);
                    });

                if (p.Traits.Count > 0)
                    box.Item().Text(t =>
                    {
                        t.Span("Черты: ").SemiBold();
                        t.Span(string.Join(", ", p.Traits));
                    });
            });
        }
    }

    private static void Section(ColumnDescriptor col, string title)
    {
        col.Item().PaddingTop(4).Text(title)
            .FontSize(14).Bold().FontColor(Colors.Blue.Darken1);
    }
}
