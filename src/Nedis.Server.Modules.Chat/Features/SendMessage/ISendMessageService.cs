using Nedis.Server.Common;
using Nedis.Server.Modules.Chat.Shared;

namespace Nedis.Server.Modules.Chat.Features.SendMessage;

public interface ISendMessageService
{
    Task<ServiceResult<MessageSummary, SendMessageError>> SendAsync(SendMessageRequest request);
}