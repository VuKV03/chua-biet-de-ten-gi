using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Domain.Entities;

namespace Admin.Domain.Entities;

[Table("nguoi_dung_refresh_token")]
public class nguoi_dung_refresh_token : BaseAuditableEntity
{
    [Required]
    public Guid nguoi_dung_id { get; set; }

    [Required]
    [StringLength(255)]
    public string token_hash { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string jwt_id { get; set; } = string.Empty;

    public DateTime ngay_het_han { get; set; }
    public bool da_su_dung { get; set; } = false;
    public bool da_thu_hoi { get; set; } = false;

    [StringLength(45)]
    public string? dia_chi_ip { get; set; }

    [StringLength(500)]
    public string? thong_tin_thiet_bi { get; set; }

    [StringLength(255)]
    public string? thay_the_boi_token { get; set; }

    [ForeignKey(nameof(nguoi_dung_id))]
    public virtual nguoi_dung? nguoi_dung { get; set; }

    [NotMapped]
    public bool IsExpired => DateTime.UtcNow >= ngay_het_han;

    [NotMapped]
    public bool IsActive => !da_thu_hoi && !da_su_dung && !IsExpired;
}
