using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCenter.Data;
using ServiceCenter.DTOs;
using ServiceCenter.Models;
using System.Security.Claims;

namespace ServiceCenter.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly ServiceCenterDbContext _context;
    private readonly UserManager<User> _userManager;

    public ChatController(ServiceCenterDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet("messages/{userId}")]
    public async Task<ActionResult<IEnumerable<ChatMessageDto>>> GetMessagesWithUser(int userId)
    {
        var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
        {
            return Unauthorized();
        }

        var isAdmin = User.IsInRole(UserRoles.Admin);

        // Validate that non-admin users can only get messages with admins
        if (!isAdmin)
        {
            var otherUser = await _context.Users.FindAsync(userId);
            if (otherUser == null || !await _userManager.IsInRoleAsync(otherUser, UserRoles.Admin))
            {
                return Forbid();
            }
        }

        var messages = await _context.ChatMessages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => (m.SenderId == currentUserId && m.ReceiverId == userId) || 
                       (m.SenderId == userId && m.ReceiverId == currentUserId))
            .OrderBy(m => m.SentAt)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = m.Sender.UserName ?? m.Sender.Email ?? "Unknown",
                ReceiverId = m.ReceiverId,
                ReceiverName = m.Receiver.UserName ?? m.Receiver.Email ?? "Unknown",
                Message = m.Message,
                SentAt = m.SentAt,
                IsRead = m.IsRead
            })
            .ToListAsync();

        // Mark messages as read
        var unreadMessages = messages.Where(m => m.ReceiverId == currentUserId && !m.IsRead).ToList();
        foreach (var message in unreadMessages)
        {
            var dbMessage = await _context.ChatMessages.FindAsync(message.Id);
            if (dbMessage != null)
            {
                dbMessage.IsRead = true;
            }
        }
        await _context.SaveChangesAsync();

        return Ok(messages);
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<ChatUserDto>>> GetChatUsers()
    {
        var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
        {
            return Unauthorized();
        }

        var isAdmin = User.IsInRole(UserRoles.Admin);

        if (!isAdmin)
        {
            // Non-admin users can only see admins
            var adminUsers = await _context.UserRoles
                .Where(ur => _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == UserRoles.Admin))
                .Select(ur => ur.UserId)
                .ToListAsync();

            var admins = await _context.Users
                .Where(u => adminUsers.Contains(u.Id))
                .Select(u => new ChatUserDto
                {
                    Id = u.Id,
                    Name = u.UserName ?? u.Email ?? "Unknown",
                    Email = u.Email ?? "",
                    Role = "Admin",
                    UnreadCount = _context.ChatMessages.Count(m => m.SenderId == u.Id && m.ReceiverId == currentUserId && !m.IsRead),
                    LastMessage = _context.ChatMessages
                        .Where(m => (m.SenderId == u.Id && m.ReceiverId == currentUserId) || (m.SenderId == currentUserId && m.ReceiverId == u.Id))
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => new ChatMessageDto
                        {
                            Id = m.Id,
                            SenderId = m.SenderId,
                            SenderName = m.Sender.UserName ?? m.Sender.Email ?? "Unknown",
                            ReceiverId = m.ReceiverId,
                            ReceiverName = m.Receiver.UserName ?? m.Receiver.Email ?? "Unknown",
                            Message = m.Message,
                            SentAt = m.SentAt,
                            IsRead = m.IsRead
                        })
                        .FirstOrDefault(),
                    IsOnline = false // Will be updated by SignalR
                })
                .ToListAsync();

            return Ok(admins);
        }
        else
        {
            // Admin users can see all users who have chatted with them
            var usersWithChats = await _context.ChatMessages
                .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                .Select(m => new { m.SenderId, m.ReceiverId })
                .ToListAsync();

            var userIds = usersWithChats.SelectMany(u => new[] { u.SenderId, u.ReceiverId })
                                       .Where(id => id != currentUserId)
                                       .Distinct()
                                       .ToList();

            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new ChatUserDto
                {
                    Id = u.Id,
                    Name = u.UserName ?? u.Email ?? "Unknown",
                    Email = u.Email ?? "",
                    Role = _context.UserRoles.Any(ur => ur.UserId == u.Id && _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == UserRoles.Admin)) ? "Admin" :
                           _context.UserRoles.Any(ur => ur.UserId == u.Id && _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == UserRoles.Technician)) ? "Technician" : "Client",
                    UnreadCount = _context.ChatMessages.Count(m => m.SenderId == u.Id && m.ReceiverId == currentUserId && !m.IsRead),
                    LastMessage = _context.ChatMessages
                        .Where(m => (m.SenderId == u.Id && m.ReceiverId == currentUserId) || (m.SenderId == currentUserId && m.ReceiverId == u.Id))
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => new ChatMessageDto
                        {
                            Id = m.Id,
                            SenderId = m.SenderId,
                            SenderName = m.Sender.UserName ?? m.Sender.Email ?? "Unknown",
                            ReceiverId = m.ReceiverId,
                            ReceiverName = m.Receiver.UserName ?? m.Receiver.Email ?? "Unknown",
                            Message = m.Message,
                            SentAt = m.SentAt,
                            IsRead = m.IsRead
                        })
                        .FirstOrDefault(),
                    IsOnline = false // Will be updated by SignalR
                })
                .ToListAsync();

            return Ok(users.OrderByDescending(u => u.LastMessage?.SentAt));
        }
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        try
        {
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserIdClaim == null)
                return Unauthorized();

            var currentUserId = int.Parse(currentUserIdClaim);
            var currentUser = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
            
            if (currentUser == null)
                return NotFound();

            // Check if user is admin
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            if (!isAdmin)
                return Forbid();

            // Get all messages for admin
            var messages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.ReceiverId == currentUserId || m.SenderId == currentUserId)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            // Group by other user
            var conversations = new List<ChatConversationDto>();
            var processedUsers = new HashSet<int>();

            foreach (var message in messages)
            {
                var otherUserId = message.SenderId == currentUserId ? message.ReceiverId : message.SenderId;
                
                if (processedUsers.Contains(otherUserId))
                    continue;
                    
                processedUsers.Add(otherUserId);
                
                var otherUser = message.SenderId == currentUserId ? message.Receiver : message.Sender;
                var userMessages = messages.Where(m => 
                    (m.SenderId == currentUserId && m.ReceiverId == otherUserId) || 
                    (m.SenderId == otherUserId && m.ReceiverId == currentUserId)).ToList();
                    
                var unreadCount = userMessages.Count(m => m.ReceiverId == currentUserId && !m.IsRead);
                var lastMessage = userMessages.OrderByDescending(m => m.SentAt).FirstOrDefault();

                conversations.Add(new ChatConversationDto
                {
                    UserId = otherUserId,
                    UserName = otherUser?.UserName ?? otherUser?.Email ?? "Unknown",
                    UserEmail = otherUser?.Email ?? "",
                    UserRole = "Client",
                    UnreadCount = unreadCount,
                    LastMessage = lastMessage != null ? new ChatMessageDto
                    {
                        Id = lastMessage.Id,
                        SenderId = lastMessage.SenderId,
                        SenderName = lastMessage.Sender?.UserName ?? lastMessage.Sender?.Email ?? "Unknown",
                        ReceiverId = lastMessage.ReceiverId,
                        ReceiverName = lastMessage.Receiver?.UserName ?? lastMessage.Receiver?.Email ?? "Unknown",
                        Message = lastMessage.Message,
                        SentAt = lastMessage.SentAt,
                        IsRead = lastMessage.IsRead
                    } : null,
                    IsOnline = false
                });
            }

            return Ok(conversations.OrderByDescending(c => c.LastMessage?.SentAt));
        }
        catch (Exception ex)
        {
            // Return empty list if there's an error
            return Ok(new List<ChatConversationDto>());
        }
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto model)
    {
        var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
        {
            return Unauthorized();
        }

        var sender = await _context.Users.FindAsync(currentUserId);
        var receiver = await _context.Users.FindAsync(model.ReceiverId);
        
        if (sender == null || receiver == null)
        {
            return NotFound("User not found");
        }

        // Validate that non-admin users can only send messages to admins
        if (!User.IsInRole(UserRoles.Admin) && !await _userManager.IsInRoleAsync(receiver, UserRoles.Admin))
        {
            return Forbid("You can only send messages to administrators");
        }

        var chatMessage = new ChatMessage
        {
            SenderId = currentUserId,
            ReceiverId = model.ReceiverId,
            Message = model.Message,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.ChatMessages.Add(chatMessage);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Message sent successfully", messageId = chatMessage.Id });
    }

    [HttpPost("mark-read/{messageId}")]
    public async Task<IActionResult> MarkAsRead(int messageId)
    {
        var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
        {
            return Unauthorized();
        }

        var message = await _context.ChatMessages.FindAsync(messageId);
        if (message == null)
        {
            return NotFound();
        }

        if (message.ReceiverId != currentUserId)
        {
            return Forbid();
        }

        message.IsRead = true;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
        {
            return Unauthorized();
        }

        var count = await _context.ChatMessages
            .CountAsync(m => m.ReceiverId == currentUserId && !m.IsRead);

        return Ok(count);
    }
}