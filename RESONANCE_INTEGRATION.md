# Resonance Cross-Fork Integration Guide

This guide shows Mare fork developers how to integrate with Resonance for cross-client synchronization.

## What is Resonance?

Resonance bridges different Mare forks using AT Protocol, allowing users on different clients (TeraSync, Neko Net, etc.) to discover and sync with each other.

## 2-Step Integration Pattern

### Step 1: Client Discovery Registration
Add this code to your Plugin constructor, **after** your host/services are initialized:

```csharp
// Register with Resonance for cross-client sync discovery
try
{
    var resonanceRegister = pluginInterface.GetIpcSubscriber<string, string, bool>("Resonance.RegisterClient");
    resonanceRegister?.InvokeFunc("YourForkName", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0");
}
catch
{
    // Resonance not installed, ignore
}
```

### Step 2: Character Data Publishing
Add this code wherever your fork successfully updates/publishes character data:

```csharp
// Publish to Resonance for cross-fork sync
try
{
    var resonancePublish = dalamudUtilService.PluginInterface.GetIpcSubscriber<Dictionary<string, object>, bool>("Resonance.PublishData");
    var characterData = new Dictionary<string, object>
    {
        ["Id"] = yourCharacterDataId,
        ["FileGamePaths"] = yourFileGamePaths, // List of mod file paths
        ["Source"] = "YourForkName",
        ["Timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        // Add any additional character data fields your fork uses
    };
    resonancePublish?.InvokeFunc(characterData);
}
catch
{
    // Resonance not available, ignore
}
```

## Implementation Notes

1. **Always use try-catch**: Resonance might not be installed
2. **Call after successful updates**: Only publish when your own sync succeeds
3. **Include source identification**: Set "Source" to your fork name
4. **Add timestamps**: Helps with data freshness tracking
5. **Keep it simple**: Just 2 code additions total

## TeraSync Example

See the complete implementation in:
- `Plugin.cs` lines 248-257 (Registration)
- `Services/CharaData/CharaDataManager.cs` lines 682-698 and 946-962 (Publishing)

## Requirements

- Resonance plugin installed (users will install this separately)
- Your fork must use Dalamud plugin interface
- Character data should be serializable to Dictionary<string, object>

## Benefits

- Zero breaking changes to your existing sync system
- Your users can sync with users from other Mare forks
- Automatic discovery - no manual server setup needed
- Falls back gracefully when Resonance isn't available