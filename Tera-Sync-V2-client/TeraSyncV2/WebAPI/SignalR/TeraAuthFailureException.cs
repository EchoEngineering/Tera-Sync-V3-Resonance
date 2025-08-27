namespace TeraSyncV2.WebAPI.SignalR;

public class TeraAuthFailureException : Exception
{
    public TeraAuthFailureException(string reason)
    {
        Reason = reason;
    }

    public string Reason { get; }
}