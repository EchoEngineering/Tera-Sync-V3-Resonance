using TeraSyncV2.API.Data.Enum;

namespace TeraSyncV2Shared.Utils;
public record ClientMessage(MessageSeverity Severity, string Message, string UID);
