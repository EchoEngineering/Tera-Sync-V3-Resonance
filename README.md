# TeraSync V3 (Experimental) - Resonance Integration

⚠️ **EXPERIMENTAL BUILD** ⚠️

This is an experimental version of TeraSync with cross-fork synchronization capabilities using Resonance.

## What's New in V3

- **Cross-Fork Sync**: Synchronize with users on different Mare clients (Neko Net, Anatoli Test, etc.)
- **Resonance Integration**: Uses AT Protocol for decentralized cross-client communication
- **Automatic Discovery**: See and connect with users regardless of their Mare fork choice

## Requirements

- **Resonance Plugin**: Must be installed separately for cross-fork sync to work
- **TeraSync V2**: This is **NOT** a replacement for stable TeraSync V2, it's experimental

## Installation

1. Install Resonance plugin from its repository
2. Download TeraSync V3 experimental build
3. Both plugins will work together automatically

## What Works

✅ **TeraSync Internal Sync**: Full compatibility with existing TeraSync server and users
✅ **Client Discovery**: Shows up in Resonance discovery UI with other fork users  
✅ **Data Publishing**: Publishes character data to AT Protocol for cross-fork sync
✅ **Safe Fallback**: Works normally if Resonance isn't installed

## What's Experimental

⚠️ **Cross-Fork Data Retrieval**: Receiving data from other forks (in development)
⚠️ **Performance Impact**: May affect game performance (untested at scale)  
⚠️ **Data Compatibility**: Character data format may not match between different forks

## Feedback & Issues

- Use this build at your own risk
- Report issues in the GitHub Issues section
- For stable sync, use regular TeraSync V2

## For Developers

See `RESONANCE_INTEGRATION.md` for the 2-step integration pattern to add cross-fork sync to your Mare fork.