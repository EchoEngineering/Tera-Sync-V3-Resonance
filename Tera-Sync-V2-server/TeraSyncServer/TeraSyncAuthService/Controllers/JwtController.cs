using TeraSyncV2.API.Routes;
using TeraSyncV2AuthService.Services;
using TeraSyncV2Shared;
using TeraSyncV2Shared.Data;
using TeraSyncV2Shared.Services;
using TeraSyncV2Shared.Utils;
using TeraSyncV2Shared.Utils.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace TeraSyncV2AuthService.Controllers;

[Route(TeraAuth.Auth)]
public class JwtController : AuthControllerBase
{
    public JwtController(ILogger<JwtController> logger,
        IHttpContextAccessor accessor, IDbContextFactory<TeraDbContext> teraDbContextFactory,
        SecretKeyAuthenticatorService secretKeyAuthenticatorService,
        IConfigurationService<AuthServiceConfiguration> configuration,
        IDatabase redisDb, GeoIPService geoIPProvider)
            : base(logger, accessor, teraDbContextFactory, secretKeyAuthenticatorService,
                configuration, redisDb, geoIPProvider)
    {
    }

    [AllowAnonymous]
    [HttpPost(TeraAuth.Auth_CreateIdent)]
    public async Task<IActionResult> CreateToken(string auth, string charaIdent)
    {
        using var dbContext = await TeraDbContextFactory.CreateDbContextAsync();
        return await AuthenticateInternal(dbContext, auth, charaIdent).ConfigureAwait(false);
    }

    [Authorize(Policy = "Authenticated")]
    [HttpGet(TeraAuth.Auth_RenewToken)]
    public async Task<IActionResult> RenewToken()
    {
        using var dbContext = await TeraDbContextFactory.CreateDbContextAsync();
        try
        {
            var uid = HttpContext.User.Claims.Single(p => string.Equals(p.Type, TeraClaimTypes.Uid, StringComparison.Ordinal))!.Value;
            var ident = HttpContext.User.Claims.Single(p => string.Equals(p.Type, TeraClaimTypes.CharaIdent, StringComparison.Ordinal))!.Value;
            var alias = HttpContext.User.Claims.SingleOrDefault(p => string.Equals(p.Type, TeraClaimTypes.Alias))?.Value ?? string.Empty;

            if (await dbContext.Auth.Where(u => u.UserUID == uid || u.PrimaryUserUID == uid).AnyAsync(a => a.MarkForBan))
            {
                var userAuth = await dbContext.Auth.SingleAsync(u => u.UserUID == uid);
                await EnsureBan(uid, userAuth.PrimaryUserUID, ident);

                return Unauthorized("Your Tera Sync V2 account is banned.");
            }

            if (await IsIdentBanned(dbContext, ident))
            {
                return Unauthorized("Your XIV service account is banned from using the service.");
            }

            Logger.LogInformation("RenewToken:SUCCESS:{id}:{ident}", uid, ident);
            return await CreateJwtFromId(uid, ident, alias);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "RenewToken:FAILURE");
            return Unauthorized("Unknown error while renewing authentication token");
        }
    }

    protected async Task<IActionResult> AuthenticateInternal(TeraDbContext dbContext, string auth, string charaIdent)
    {
        try
        {
            if (string.IsNullOrEmpty(auth)) return BadRequest("No Authkey");
            if (string.IsNullOrEmpty(charaIdent)) return BadRequest("No CharaIdent");

            var ip = HttpAccessor.GetIpAddress();

            var authResult = await SecretKeyAuthenticatorService.AuthorizeAsync(ip, auth);

            return await GenericAuthResponse(dbContext, charaIdent, authResult);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Authenticate:UNKNOWN");
            return Unauthorized("Unknown internal server error during authentication");
        }
    }
}