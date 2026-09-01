namespace SharedKernel.Application.DTO;

public class CurrentUserDto
{
    public Guid? id { get; set; }
    public string tai_khoan { get; set; } = string.Empty;
    public string? ten { get; set; }
    public string? email { get; set; }
    public string? so_dien_thoai { get; set; }
    public bool is_super_admin { get; set; }
    public string? don_vi_id { get; set; }
}
