using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Admin.NET.Application.GmRpc;

namespace Admin.NET.Application.Service;

public sealed class GameServerItemAuditQueryInput : BasePageInput
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "ServerId 必须大于 0")]
    public int ServerId { get; set; }

    [Required]
    [Range(typeof(long), "1", "9223372036854775807", ErrorMessage = "RoleId 必须大于 0")]
    public long RoleId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "ItemId 必须大于 0")]
    public int? ItemId { get; set; }

    [DefaultValue(null)]
    public ItemChangeSourceType? SourceType { get; set; }

    [DefaultValue(null)]
    public ItemChangeOperationType? OperationType { get; set; }

    [Range(typeof(long), "1", "9223372036854775807", ErrorMessage = "OperatorId 必须大于 0")]
    public long? OperatorId { get; set; }

    [MaxLength(128)]
    public string? TraceId { get; set; }

    [MaxLength(128)]
    public string? ReasonCode { get; set; }

    [Range(typeof(long), "1", "9223372036854775807", ErrorMessage = "StartTimeTicksUtc 必须大于 0")]
    public long? StartTimeTicksUtc { get; set; }

    [Range(typeof(long), "1", "9223372036854775807", ErrorMessage = "EndTimeTicksUtc 必须大于 0")]
    public long? EndTimeTicksUtc { get; set; }

    [Range(1, int.MaxValue)]
    public override int Page { get; set; } = 1;

    [Range(1, 100)]
    public override int PageSize { get; set; } = 50;
}

public sealed class GameServerItemAuditPageOutput
{
    public int ServerId { get; set; }

    public string ServerName { get; set; } = string.Empty;

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int Total { get; set; }

    public int TotalPages { get; set; }

    public bool HasMore { get; set; }

    public bool HasNextPage { get; set; }

    public bool HasPrevPage { get; set; }

    public List<GameServerItemAuditOutput> Items { get; set; } = new();
}

public sealed class GameServerItemAuditOutput
{
    public long LogId { get; set; }

    public long RoleId { get; set; }

    public int ItemId { get; set; }

    public long BeforeNum { get; set; }

    public long AfterNum { get; set; }

    public long Delta { get; set; }

    public ItemChangeSourceType SourceType { get; set; }

    public ItemChangeOperationType OperationType { get; set; }

    public string ReasonCode { get; set; } = string.Empty;

    public string Remark { get; set; } = string.Empty;

    public long OperatorId { get; set; }

    public string OperatorName { get; set; } = string.Empty;

    public string TraceId { get; set; } = string.Empty;

    public string RequestUniId { get; set; } = string.Empty;

    public long ChangeTimeTicks { get; set; }
}
