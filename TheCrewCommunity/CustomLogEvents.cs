namespace TheCrewCommunity;

internal static class CustomLogEvents
{
    public static EventId LiveBot { get; } = new(200, "LiveBot");
    public static EventId CommandExecuted { get; } = new(201, "CMDExecuted");
    public static EventId CommandErrored { get; } = new(202, "CMDError");
    public static EventId ClientError { get; } = new(203, "ClientError");
    public static EventId SlashExecuted { get; } = new(204, "SlashExecuted");
    public static EventId SlashErrored { get; } = new(205, "SlashErrored");
    public static EventId ContextMenuExecuted { get; } = new(206, "ContextMenuExecuted");
    public static EventId ContextMenuErrored { get; } = new(207, "ContextMenuErrored");
    public static EventId AutoMod { get; } = new(208, "AutoMod");
    public static EventId DeleteLog { get; } = new(209, "DeleteLog");
    public static EventId ModMail { get; } = new(210, "ModMail");
    public static EventId PhotoCleanup { get; } = new(211, "PhotoCleanup");
    public static EventId LiveStream { get; } = new(212, "LiveStream");
    public static EventId AuditLogManager { get; } = new(213, "AuditLogManager");
    public static EventId ServiceError { get; } = new(214, "ServiceError");
    public static EventId ModLog { get; } = new(215, "ModLog");
    public static EventId TagCommand { get; } = new(216, "TagCommand");
    public static EventId InviteLinkFilter { get; } = new(217, "InviteLinkFilter");
    public static EventId ModMailCleanup { get; } = new(218, "MMCleanup");
    public static EventId StreamNotification { get; } = new(219, "StreamNotification");
    public static EventId WebAccount { get; } = new(220, "WebAccount");
    public static EventId PhotoUpload { get; } = new(221, "PhotoUpload");
    public static EventId PhotoBrowse { get; } = new(222, "PhotoBrowse");
    public static EventId DatabaseMethods { get; } = new(223, "DB Methods");
    public static EventId PhotoView { get; } = new(224, "PhotoView");
    public static EventId CloudFlare { get; } = new(225, "CloudFlare");
    public static EventId UserActivity { get; } = new(226, "User Activity");
}