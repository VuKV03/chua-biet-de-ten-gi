namespace SharedKernel.Domain.Entities;

public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime? ngay_tao { get; set; }
    public string? nguoi_tao { get; set; }
    public DateTime? ngay_chinh_sua { get; set; }
    public string? nguoi_chinh_sua { get; set; }
}
