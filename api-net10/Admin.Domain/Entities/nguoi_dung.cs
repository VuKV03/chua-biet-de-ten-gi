using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Domain.Entities;

namespace Admin.Domain.Entities;

// Ràng buộc UNIQUE trên tai_khoan đã được định nghĩa trực tiếp ở DB (UX_tai_khoan, xem Phase 1 SQL);
// không dùng [Index] của EF Core ở đây để Domain layer không phụ thuộc package EF Core.
[Table("nguoi_dung")]
public class nguoi_dung : BaseAuditableEntity
{
    [Required]
    [StringLength(255)]
    public string tai_khoan { get; set; } = string.Empty;

    [Required]
    [StringLength(512)]
    public string mat_khau { get; set; } = string.Empty;

    [StringLength(255)]
    public string? salt_code { get; set; }

    [Required]
    [StringLength(255)]
    public string ten { get; set; } = string.Empty;

    [StringLength(255)]
    public string? email { get; set; }

    [StringLength(32)]
    public string? so_dien_thoai { get; set; }

    public bool? gioi_tinh { get; set; }
    public DateTime? ngay_sinh { get; set; }
    public Guid? don_vi_id { get; set; }

    [StringLength(255)]
    public string? chuc_vu { get; set; }

    public bool trang_thai { get; set; } = true;
    public bool is_super_admin { get; set; } = false;

    public bool? is_doi_mk { get; set; } = false;
    public DateTime? ngay_doi_mk_gan_nhat { get; set; }

    public int so_lan_dang_nhap_sai { get; set; } = 0;
    public DateTime? khoa_den_ngay { get; set; }
    public int password_hash_type { get; set; } = 1;

    public virtual ICollection<nguoi_dung_refresh_token> ds_refresh_tokens { get; set; } = new HashSet<nguoi_dung_refresh_token>();
}
