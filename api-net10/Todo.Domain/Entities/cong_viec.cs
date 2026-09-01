using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Domain.Entities;
using Todo.Domain.Enums;

namespace Todo.Domain.Entities;

[Table("cong_viec")]
public class cong_viec : BaseAuditableEntity
{
    [Required]
    public Guid nguoi_dung_id { get; set; }

    [Required]
    [StringLength(500)]
    public string tieu_de { get; set; } = string.Empty;

    public TrangThaiCongViec trang_thai { get; set; } = TrangThaiCongViec.ChuaLam;

    public DateTime? ngay_hoan_thanh { get; set; }
}