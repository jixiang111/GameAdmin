using System.ComponentModel.DataAnnotations;

namespace Admin.NET.Application.Service;

public sealed class GameServerItemAdjustInput
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "ServerId 必须大于 0")]
    public int ServerId { get; set; }

    [Required]
    [Range(typeof(long), "1", "9223372036854775807", ErrorMessage = "RoleId 必须大于 0")]
    public long RoleId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "ItemId 必须大于 0")]
    public int ItemId { get; set; }

    public long Delta { get; set; }

    [Required]
    [MaxLength(128)]
    public string ReasonCode { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Remark { get; set; }

    [MaxLength(128)]
    public string? TraceId { get; set; }
}

public sealed class GameServerItemAdjustOutput
{
    public int ServerId { get; set; }

    public long RoleId { get; set; }

    public int ItemId { get; set; }

    public long AppliedDelta { get; set; }

    public long BeforeNum { get; set; }

    public long AfterNum { get; set; }

    public long ChangeTimeTicks { get; set; }

    public string TraceId { get; set; } = string.Empty;

    public string ReasonCode { get; set; } = string.Empty;

    public string Remark { get; set; } = string.Empty;

    public long OperatorId { get; set; }

    public string OperatorName { get; set; } = string.Empty;
}
