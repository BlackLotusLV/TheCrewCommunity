namespace TheCrewCommunity.Services;

public interface IPersistentMessageService
{
    void EnqueueMessageUpdate(ulong channelId);
}
