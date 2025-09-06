using System.ComponentModel.DataAnnotations;

namespace TeraSyncV2Shared.Models;

public class BannedRegistrations
{
    [Key]
    [MaxLength(100)]
    public string DiscordIdOrLodestoneAuth { get; set; }
}
