using SmartBank.Data.Repositories;
using SmartBank.Models.DTOs.Support;
using SmartBank.Models.Entities;

namespace SmartBank.API.Services;

public interface ISupportService
{
    Task<TicketResultDto> CreateTicketAsync(int userId, CreateTicketDto request);
    Task<TicketListDto> GetMyTicketsAsync(int userId);
    Task<TicketListDto> GetAllTicketsAsync();
    Task<TicketResultDto> ResolveTicketAsync(int adminUserId, ResolveTicketDto request);
    Task<TicketResponseDto?> GetTicketByIdAsync(int ticketId, int userId, bool isAdmin);
}

public class SupportService : ISupportService
{
    private readonly ISupportRepository _repo;
    private readonly INotificationRepository _notif;
    private readonly ILogger<SupportService> _logger;

    public SupportService(ISupportRepository repo, INotificationRepository notif, ILogger<SupportService> logger)
    {
        _repo   = repo;
        _notif  = notif;
        _logger = logger;
    }

    public async Task<TicketResultDto> CreateTicketAsync(int userId, CreateTicketDto request)
    {
        var ticket = new SupportTicket
        {
            CreatedByUserId = userId,
            Subject         = request.Subject.Trim(),
            Description     = request.Description.Trim(),
            Category        = string.IsNullOrWhiteSpace(request.Category) ? "Other" : request.Category,
            Priority        = string.IsNullOrWhiteSpace(request.Priority) ? "Medium" : request.Priority,
            Status          = "Open",
            CreatedAt       = DateTime.UtcNow
        };

        await _repo.AddAsync(ticket);

        await _notif.AddAsync(new Notification
        {
            UserId            = userId,
            Title             = "Support Ticket Created",
            Message           = $"Your support ticket #{ticket.TicketId} '{ticket.Subject}' has been created. We will respond shortly.",
            Type              = "Support",
            RelatedEntityId   = ticket.TicketId,
            RelatedEntityType = "Ticket",
            CreatedAt         = DateTime.UtcNow
        });

        _logger.LogInformation("Support ticket {TicketId} created by user {UserId}", ticket.TicketId, userId);

        return new TicketResultDto
        {
            Success = true,
            Message = "Support ticket created. We will get back to you soon.",
            Ticket  = MapTicket(ticket)
        };
    }

    public async Task<TicketListDto> GetMyTicketsAsync(int userId)
    {
        var tickets = await _repo.GetByUserAsync(userId);
        return BuildList(tickets);
    }

    public async Task<TicketListDto> GetAllTicketsAsync()
    {
        var tickets = await _repo.GetAllAsync();
        return BuildList(tickets);
    }

    public async Task<TicketResultDto> ResolveTicketAsync(int adminUserId, ResolveTicketDto request)
    {
        var ticket = await _repo.GetByIdAsync(request.TicketId);
        if (ticket is null)
            return new TicketResultDto { Success = false, Message = "Ticket not found." };

        if (ticket.Status == "Resolved" || ticket.Status == "Closed")
            return new TicketResultDto { Success = false, Message = "Ticket is already resolved." };

        ticket.Resolution       = request.Resolution.Trim();
        ticket.Status           = "Resolved";
        ticket.AssignedToUserId = adminUserId;
        ticket.ResolvedAt       = DateTime.UtcNow;
        await _repo.UpdateAsync(ticket);

        await _notif.AddAsync(new Notification
        {
            UserId            = ticket.CreatedByUserId,
            Title             = "Ticket Resolved",
            Message           = $"Your ticket #{ticket.TicketId} '{ticket.Subject}' has been resolved.",
            Type              = "Support",
            RelatedEntityId   = ticket.TicketId,
            RelatedEntityType = "Ticket",
            CreatedAt         = DateTime.UtcNow
        });

        return new TicketResultDto
        {
            Success = true,
            Message = "Ticket resolved.",
            Ticket  = MapTicket(ticket)
        };
    }

    public async Task<TicketResponseDto?> GetTicketByIdAsync(int ticketId, int userId, bool isAdmin)
    {
        var t = await _repo.GetByIdAsync(ticketId);
        if (t is null) return null;
        if (!isAdmin && t.CreatedByUserId != userId) return null;
        return MapTicket(t);
    }

    private static TicketListDto BuildList(List<SupportTicket> tickets) => new()
    {
        Tickets       = tickets.Select(MapTicket).ToList(),
        TotalCount    = tickets.Count,
        OpenCount     = tickets.Count(t => t.Status != "Resolved" && t.Status != "Closed"),
        ResolvedCount = tickets.Count(t => t.Status == "Resolved" || t.Status == "Closed")
    };

    private static TicketResponseDto MapTicket(SupportTicket t) => new()
    {
        TicketId    = t.TicketId,
        Subject     = t.Subject,
        Description = t.Description,
        Category    = t.Category,
        Priority    = t.Priority,
        Status      = t.Status ?? "Open",
        Resolution  = t.Resolution,
        CreatedAt   = t.CreatedAt ?? DateTime.UtcNow,
        ResolvedAt  = t.ResolvedAt
    };
}
