using TeraSyncV2.WebAPI.SignalR.Utils;
using Microsoft.AspNetCore.SignalR.Client;
using TeraSyncV2.API.Dto.Group;

namespace TeraSyncV2.WebAPI;

public partial class ApiController
{
    public async Task GroupBanUser(GroupPairDto dto, string reason)
    {
        CheckConnection();
        await _teraHub!.SendAsync(nameof(GroupBanUser), dto, reason).ConfigureAwait(false);
    }

    public async Task GroupChangeGroupPermissionState(GroupPermissionDto dto)
    {
        CheckConnection();
        await _teraHub!.SendAsync(nameof(GroupChangeGroupPermissionState), dto).ConfigureAwait(false);
    }

    public async Task GroupChangeIndividualPermissionState(GroupPairUserPermissionDto dto)
    {
        CheckConnection();
        await SetBulkPermissions(new(new(StringComparer.Ordinal),
            new(StringComparer.Ordinal) {
                { dto.Group.GID, dto.GroupPairPermissions }
            })).ConfigureAwait(false);
    }

    public async Task GroupChangeOwnership(GroupPairDto groupPair)
    {
        CheckConnection();
        await _teraHub!.SendAsync(nameof(GroupChangeOwnership), groupPair).ConfigureAwait(false);
    }

    public async Task<bool> GroupChangePassword(GroupPasswordDto groupPassword)
    {
        CheckConnection();
        return await _teraHub!.InvokeAsync<bool>(nameof(GroupChangePassword), groupPassword).ConfigureAwait(false);
    }

    public async Task GroupClear(GroupDto group)
    {
        CheckConnection();
        await _teraHub!.SendAsync(nameof(GroupClear), group).ConfigureAwait(false);
    }

    public async Task<GroupJoinDto> GroupCreate()
    {
        CheckConnection();
        return await _teraHub!.InvokeAsync<GroupJoinDto>(nameof(GroupCreate)).ConfigureAwait(false);
    }

    public async Task<List<string>> GroupCreateTempInvite(GroupDto group, int amount)
    {
        CheckConnection();
        return await _teraHub!.InvokeAsync<List<string>>(nameof(GroupCreateTempInvite), group, amount).ConfigureAwait(false);
    }

    public async Task GroupDelete(GroupDto group)
    {
        CheckConnection();
        await _teraHub!.SendAsync(nameof(GroupDelete), group).ConfigureAwait(false);
    }

    public async Task<List<BannedGroupUserDto>> GroupGetBannedUsers(GroupDto group)
    {
        CheckConnection();
        return await _teraHub!.InvokeAsync<List<BannedGroupUserDto>>(nameof(GroupGetBannedUsers), group).ConfigureAwait(false);
    }

    public async Task<GroupJoinInfoDto> GroupJoin(GroupPasswordDto passwordedGroup)
    {
        CheckConnection();
        return await _teraHub!.InvokeAsync<GroupJoinInfoDto>(nameof(GroupJoin), passwordedGroup).ConfigureAwait(false);
    }

    public async Task<bool> GroupJoinFinalize(GroupJoinDto passwordedGroup)
    {
        CheckConnection();
        return await _teraHub!.InvokeAsync<bool>(nameof(GroupJoinFinalize), passwordedGroup).ConfigureAwait(false);
    }

    public async Task GroupLeave(GroupDto group)
    {
        CheckConnection();
        await _teraHub!.SendAsync(nameof(GroupLeave), group).ConfigureAwait(false);
    }

    public async Task GroupRemoveUser(GroupPairDto groupPair)
    {
        CheckConnection();
        await _teraHub!.SendAsync(nameof(GroupRemoveUser), groupPair).ConfigureAwait(false);
    }

    public async Task GroupSetUserInfo(GroupPairUserInfoDto groupPair)
    {
        CheckConnection();
        await _teraHub!.SendAsync(nameof(GroupSetUserInfo), groupPair).ConfigureAwait(false);
    }

    public async Task<int> GroupPrune(GroupDto group, int days, bool execute)
    {
        CheckConnection();
        return await _teraHub!.InvokeAsync<int>(nameof(GroupPrune), group, days, execute).ConfigureAwait(false);
    }

    public async Task<List<GroupFullInfoDto>> GroupsGetAll()
    {
        CheckConnection();
        return await _teraHub!.InvokeAsync<List<GroupFullInfoDto>>(nameof(GroupsGetAll)).ConfigureAwait(false);
    }

    public async Task GroupUnbanUser(GroupPairDto groupPair)
    {
        CheckConnection();
        await _teraHub!.SendAsync(nameof(GroupUnbanUser), groupPair).ConfigureAwait(false);
    }

    private void CheckConnection()
    {
        if (ServerState is not (SignalR.Utils.ServerState.Connected or SignalR.Utils.ServerState.Connecting or SignalR.Utils.ServerState.Reconnecting)) throw new InvalidDataException("Not connected");
    }
}