using Microsoft.EntityFrameworkCore;
using PredictiveMaintenance.Application.Assistant;
using PredictiveMaintenance.Application.Auth;
using PredictiveMaintenance.Application.Interfaces;
using PredictiveMaintenance.Domain.Entities;

namespace PredictiveMaintenance.Application.Services;

public interface IAssistantService
{
    IReadOnlyList<FaqPromptDto> GetFaqPrompts();
    Task<IReadOnlyList<AssistantSessionDto>> GetSessionsAsync(UserContext user, CancellationToken cancellationToken);
    Task<AssistantSessionDto> CreateSessionAsync(UserContext user, CreateAssistantSessionRequest request, CancellationToken cancellationToken);
    Task<AssistantSessionDto?> GetSessionAsync(UserContext user, Guid sessionId, CancellationToken cancellationToken);
    Task<AssistantSessionDto?> SendMessageAsync(UserContext user, Guid sessionId, SendAssistantMessageRequest request, CancellationToken cancellationToken);
}

public sealed class AssistantService(
    IPredictiveMaintenanceDbContext db,
    IDeepSeekClient deepSeekClient) : IAssistantService
{
    private const string SystemPrompt = """
    You are the Predictive Maintenance assistant inside a corporate SaaS dashboard.
    The application tracks resellers, machines, PLC/PCB controllers, motors, belts, relays and sensors.
    Explain maintenance risk using historic technician replacement records, install dates, movement cycles,
    rotations or activations, operating hours, temperature profile, open alerts, due dates and health scores.
    Keep answers practical for operations teams: identify what to inspect, what evidence matters,
    and when to dispatch a technician. Never ask for API keys or secrets.
    """;

    public IReadOnlyList<FaqPromptDto> GetFaqPrompts()
        =>
        [
            new("Why is this part critical?", "Explain what makes a predictive maintenance alert critical and what a technician should inspect first."),
            new("Prepare a dispatch note", "Draft a short technician dispatch note for a machine with motor heat and movement-cycle risk."),
            new("How is health calculated?", "Explain how movement history, age, temperature and technician replacement history affect the health score."),
            new("Reduce false positives", "What data should we collect to reduce predictive maintenance false positives?")
        ];

    public async Task<IReadOnlyList<AssistantSessionDto>> GetSessionsAsync(UserContext user, CancellationToken cancellationToken)
    {
        var sessions = await QueryUserSessions(user)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(cancellationToken);

        return sessions.Select(ToDto).ToList();
    }

    public async Task<AssistantSessionDto> CreateSessionAsync(UserContext user, CreateAssistantSessionRequest request, CancellationToken cancellationToken)
    {
        var title = string.IsNullOrWhiteSpace(request.Title) ? "Maintenance assistant" : request.Title.Trim();
        var session = new AssistantChatSession
        {
            AppUserId = user.UserId,
            Title = title.Length > 140 ? title[..140] : title
        };
        session.Messages.Add(new AssistantChatMessage
        {
            Role = "assistant",
            Content = "Select an FAQ or ask about machine health, alerts, replacement timing, or technician dispatch."
        });

        db.AssistantChatSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(session);
    }

    public async Task<AssistantSessionDto?> GetSessionAsync(UserContext user, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await QueryUserSessions(user).SingleOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        return session is null ? null : ToDto(session);
    }

    public async Task<AssistantSessionDto?> SendMessageAsync(UserContext user, Guid sessionId, SendAssistantMessageRequest request, CancellationToken cancellationToken)
    {
        var content = request.Content.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            return await GetSessionAsync(user, sessionId, cancellationToken);
        }

        var session = await QueryUserSessions(user).SingleOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        var messages = new List<AssistantMessage> { new("system", SystemPrompt) };
        messages.AddRange(session.Messages
            .OrderBy(m => m.CreatedAt)
            .TakeLast(16)
            .Select(m => new AssistantMessage(m.Role, m.Content)));
        messages.Add(new AssistantMessage("user", content));

        string reply;
        if (deepSeekClient.IsConfigured)
        {
            try
            {
                reply = await deepSeekClient.GetAssistantReplyAsync(messages, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                reply = $"The DeepSeek API could not be reached ({ex.Message}). Verify network access from the API container, then try again.";
            }
        }
        else
        {
            reply = LocalFallback(content);
        }

        db.AssistantChatMessages.Add(new AssistantChatMessage
        {
            AssistantChatSessionId = session.Id,
            Role = "user",
            Content = content
        });
        db.AssistantChatMessages.Add(new AssistantChatMessage
        {
            AssistantChatSessionId = session.Id,
            Role = "assistant",
            Content = reply
        });

        session.Title = session.Messages.FirstOrDefault(m => m.Role == "user")?.Content.Truncate(64)
            ?? content.Truncate(64);
        session.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return await GetSessionAsync(user, sessionId, cancellationToken);
    }

    private IQueryable<AssistantChatSession> QueryUserSessions(UserContext user)
        => db.AssistantChatSessions
            .Include(s => s.Messages)
            .Where(s => s.AppUserId == user.UserId);

    private static AssistantSessionDto ToDto(AssistantChatSession session)
        => new(
            session.Id,
            session.Title,
            session.CreatedAt,
            session.UpdatedAt,
            session.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new AssistantChatMessageDto(m.Id, m.Role, m.Content, m.CreatedAt))
                .ToList());

    private static string LocalFallback(string prompt)
        => $"""
        I can answer from the local maintenance playbook right now. For this question: "{prompt}"

        Check the machine-level alert list first, then inspect the part with the lowest health score. Prioritize parts with high movement ratio, high installed age ratio, and elevated average temperature. If the alert is critical or due within 30 days, dispatch a technician and include the first-alert date plus the latest health score in the work order.
        """;
}

internal static class StringExtensions
{
    public static string Truncate(this string value, int length)
        => value.Length <= length ? value : value[..length];
}
