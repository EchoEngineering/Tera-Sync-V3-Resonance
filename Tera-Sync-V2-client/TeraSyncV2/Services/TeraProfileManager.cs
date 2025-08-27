using Microsoft.Extensions.Logging;
using TeraSyncV2.API.Data;
using TeraSyncV2.API.Data.Comparer;
using TeraSyncV2.TeraSyncConfiguration;
using TeraSyncV2.Services.Mediator;
using TeraSyncV2.WebAPI;
using System.Collections.Concurrent;

namespace TeraSyncV2.Services;

public class TeraProfileManager : MediatorSubscriberBase
{
    private const string _teraLogo = "";
    private const string _teraLogoLoading = "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";
    private const string _teraLogoNsfw = "";
    private const string _teraSyncV2Supporter = "";
    private const string _noDescription = "-- User has no description set --";
    private const string _nsfw = "Profile not displayed - NSFW";
    private readonly ApiController _apiController;
    private readonly TeraSyncConfigService _teraSyncConfigService;
    private readonly ConcurrentDictionary<UserData, TeraProfileData> _teraProfiles = new(UserDataComparer.Instance);

    private readonly TeraProfileData _defaultProfileData = new(IsFlagged: false, IsNSFW: false, _teraLogo, string.Empty, _noDescription);
    private readonly TeraProfileData _loadingProfileData = new(IsFlagged: false, IsNSFW: false, _teraLogoLoading, string.Empty, "Loading Data from server...");
    private readonly TeraProfileData _nsfwProfileData = new(IsFlagged: false, IsNSFW: false, _teraLogoNsfw, string.Empty, _nsfw);

    public TeraProfileManager(ILogger<TeraProfileManager> logger, TeraSyncConfigService teraConfigService,
        TeraMediator mediator, ApiController apiController) : base(logger, mediator)
    {
        _teraSyncConfigService = teraConfigService;
        _apiController = apiController;

        Mediator.Subscribe<ClearProfileDataMessage>(this, (msg) =>
        {
            if (msg.UserData != null)
                _teraProfiles.Remove(msg.UserData, out _);
            else
                _teraProfiles.Clear();
        });
        Mediator.Subscribe<DisconnectedMessage>(this, (_) => _teraProfiles.Clear());
    }

    public TeraProfileData GetTeraProfile(UserData data)
    {
        if (!_teraProfiles.TryGetValue(data, out var profile))
        {
            _ = Task.Run(() => GetTeraProfileFromService(data));
            return (_loadingProfileData);
        }

        return (profile);
    }

    private async Task GetTeraProfileFromService(UserData data)
    {
        try
        {
            _teraProfiles[data] = _loadingProfileData;
            var profile = await _apiController.UserGetProfile(new API.Dto.User.UserDto(data)).ConfigureAwait(false);
            TeraProfileData profileData = new(profile.Disabled, profile.IsNSFW ?? false,
                string.IsNullOrEmpty(profile.ProfilePictureBase64) ? _teraLogo : profile.ProfilePictureBase64,
                !string.IsNullOrEmpty(data.Alias) && !string.Equals(data.Alias, data.UID, StringComparison.Ordinal) ? _teraSyncV2Supporter : string.Empty,
                string.IsNullOrEmpty(profile.Description) ? _noDescription : profile.Description);
            if (profileData.IsNSFW && !_teraSyncConfigService.Current.ProfilesAllowNsfw && !string.Equals(_apiController.UID, data.UID, StringComparison.Ordinal))
            {
                _teraProfiles[data] = _nsfwProfileData;
            }
            else
            {
                _teraProfiles[data] = profileData;
            }
        }
        catch (Exception ex)
        {
            // if fails save DefaultProfileData to dict
            Logger.LogWarning(ex, "Failed to get Profile from service for user {user}", data);
            _teraProfiles[data] = _defaultProfileData;
        }
    }
}