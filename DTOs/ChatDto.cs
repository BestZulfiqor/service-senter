namespace ServiceCenter.DTOs;

public class ChatMessageDto
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public int ReceiverId { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
}

public class SendMessageDto
{
    public int ReceiverId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ChatUserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int UnreadCount { get; set; }
    public ChatMessageDto? LastMessage { get; set; }
    public DateTime? LastMessageTime { get; set; }
    public bool IsOnline { get; set; }
}

public class ChatConversationDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public int UnreadCount { get; set; }
    public ChatMessageDto? LastMessage { get; set; }
    public bool IsOnline { get; set; }
}