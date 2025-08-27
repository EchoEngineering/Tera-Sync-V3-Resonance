namespace TeraSyncV2.Interop.Ipc;

public interface IIpcCaller : IDisposable
{
    bool APIAvailable { get; }
    void CheckAPI();
}
