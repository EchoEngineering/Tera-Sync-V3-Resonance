using TeraSyncV2Server.Hubs;
using System.Runtime.CompilerServices;

namespace TeraSyncV2Server.Utils;

public class TeraHubLogger
{
    private readonly TeraHub _hub;
    private readonly ILogger<TeraHub> _logger;

    public TeraHubLogger(TeraHub hub, ILogger<TeraHub> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public static object[] Args(params object[] args)
    {
        return args;
    }

    public void LogCallInfo(object[] args = null, [CallerMemberName] string methodName = "")
    {
        string formattedArgs = args != null && args.Length != 0 ? "|" + string.Join(":", args) : string.Empty;
        _logger.LogInformation("{uid}:{method}{args}", _hub.UserUID, methodName, formattedArgs);
    }

    public void LogCallWarning(object[] args = null, [CallerMemberName] string methodName = "")
    {
        string formattedArgs = args != null && args.Length != 0 ? "|" + string.Join(":", args) : string.Empty;
        _logger.LogWarning("{uid}:{method}{args}", _hub.UserUID, methodName, formattedArgs);
    }
}
