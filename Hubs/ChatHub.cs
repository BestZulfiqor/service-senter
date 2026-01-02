using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using ServiceCenter.Data;
using ServiceCenter.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceCenter.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ServiceCenterDbContext _context;
    private readonly ILogger<ChatHub> _logger;
    private readonly UserManager<User> _userManager;
    private static readonly Dictionary<int, string> _onlineUsers = new();

    public ChatHub(ServiceCenterDbContext context, ILogger<ChatHub> logger, UserManager<User> userManager)
    {
        _context = context;
        _logger = logger;
        _userManager = userManager;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int uid))
        {
            _onlineUsers[uid] = Context.ConnectionId;
            
            var user = await _context.Users.FindAsync(uid);
            if (user != null)
            {
                _logger.LogInformation($"User {user.UserName} ({uid}) connected to chat");
                
                // Notify admins about user coming online
                if (!Context.User.IsInRole(UserRoles.Admin))
                {
                    await NotifyAdminsAboutUserStatus(uid, true, user.UserName ?? user.Email);
                }
            }
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int uid))
        {
            _onlineUsers.Remove(uid);
            
            var user = await _context.Users.FindAsync(uid);
            if (user != null)
            {
                _logger.LogInformation($"User {user.UserName} ({uid}) disconnected from chat");
                
                // Notify admins about user going offline
                if (!Context.User.IsInRole(UserRoles.Admin))
                {
                    await NotifyAdminsAboutUserStatus(uid, false, user.UserName ?? user.Email);
                }
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(int receiverId, string message)
    {
        var senderIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(senderIdClaim) || !int.TryParse(senderIdClaim, out int senderId))
        {
            throw new HubException("Unauthorized");
        }

        var sender = await _context.Users.FindAsync(senderId);
        var receiver = await _context.Users.FindAsync(receiverId);
        
        if (sender == null || receiver == null)
        {
            throw new HubException("User not found");
        }

        // Validate that non-admin users can only send messages to admins
        if (!Context.User.IsInRole(UserRoles.Admin) && !await _userManager.IsInRoleAsync(receiver, UserRoles.Admin))
        {
            throw new HubException("You can only send messages to administrators");
        }

        var chatMessage = new ChatMessage
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Message = message,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.ChatMessages.Add(chatMessage);
        await _context.SaveChangesAsync();

        var messageDto = new
        {
            id = chatMessage.Id,
            senderId = chatMessage.SenderId,
            senderName = sender.UserName ?? sender.Email ?? "Unknown",
            receiverId = chatMessage.ReceiverId,
            receiverName = receiver.UserName ?? receiver.Email ?? "Unknown",
            message = chatMessage.Message,
            sentAt = chatMessage.SentAt,
            isRead = chatMessage.IsRead
        };

        // Send to receiver if they're online
        if (_onlineUsers.TryGetValue(receiverId, out string? connectionId))
        {
            await Clients.Client(connectionId).SendAsync("ReceiveMessage", messageDto);
        }

        // Send confirmation to sender
        await Clients.Caller.SendAsync("MessageSent", messageDto);

        // Update user list for admins
        await UpdateAdminUserList();
    }

    public async Task MarkAsRead(int messageId)
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new HubException("Unauthorized");
        }

        var message = await _context.ChatMessages.FindAsync(messageId);
        if (message != null && message.ReceiverId == userId)
        {
            message.IsRead = true;
            await _context.SaveChangesAsync();
            
            // Notify sender that message was read
            if (_onlineUsers.TryGetValue(message.SenderId, out string? connectionId))
            {
                await Clients.Client(connectionId).SendAsync("MessageRead", new { messageId = message.Id });
            }
            
            // Update user list for admins
            await UpdateAdminUserList();
        }
    }

    private async Task NotifyAdminsAboutUserStatus(int userId, bool isOnline, string userName)
    {
        var adminUsers = await _context.UserRoles
            .Where(ur => _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == UserRoles.Admin))
            .Select(ur => ur.UserId)
            .ToListAsync();

        foreach (var adminId in adminUsers)
        {
            if (_onlineUsers.TryGetValue(adminId, out string? connectionId))
            {
                await Clients.Client(connectionId).SendAsync("UserStatusChanged", new
                {
                    userId = userId,
                    userName = userName,
                    isOnline = isOnline
                });
            }
        }
    }

    private async Task UpdateAdminUserList()
    {
        // Get all users who have sent messages to admins or received messages from admins
        var usersWithChats = await _context.ChatMessages
            .Where(m => m.SenderId != m.ReceiverId)
            .Select(m => new { m.SenderId, m.ReceiverId })
            .ToListAsync();

        var userIds = usersWithChats.SelectMany(u => new[] { u.SenderId, u.ReceiverId }).Distinct().ToList();

        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.Email,
                Role = _context.UserRoles.Any(ur => ur.UserId == u.Id && _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == UserRoles.Admin)) ? "Admin" :
                       _context.UserRoles.Any(ur => ur.UserId == u.Id && _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == UserRoles.Technician)) ? "Technician" : "Client"
            })
            .ToListAsync();

        foreach (var adminId in _onlineUsers.Keys)
        {
            var adminUser = await _context.Users.FindAsync(adminId);
            if (adminUser != null && await _userManager.IsInRoleAsync(adminUser, UserRoles.Admin))
            {
                if (_onlineUsers.TryGetValue(adminId, out string? connectionId))
                {
                    var userDtos = users.Select(u => new
                    {
                        u.Id,
                        u.UserName,
                        u.Email,
                        u.Role,
                        UnreadCount = _context.ChatMessages.Count(m => m.SenderId == u.Id && m.ReceiverId == adminId && !m.IsRead),
                        LastMessage = _context.ChatMessages
                            .Where(m => (m.SenderId == u.Id && m.ReceiverId == adminId) || (m.SenderId == adminId && m.ReceiverId == u.Id))
                            .OrderByDescending(m => m.SentAt)
                            .Select(m => new
                            {
                                m.Id,
                                m.SenderId,
                                SenderName = m.Sender.UserName ?? m.Sender.Email ?? "Unknown",
                                m.ReceiverId,
                                ReceiverName = m.Receiver.UserName ?? m.Receiver.Email ?? "Unknown",
                                m.Message,
                                m.SentAt,
                                m.IsRead
                            })
                            .FirstOrDefault(),
                        IsOnline = _onlineUsers.ContainsKey(u.Id)
                    }).ToList();

                    await Clients.Client(connectionId).SendAsync("UpdateUserList", userDtos);
                }
            }
        }
    }
}