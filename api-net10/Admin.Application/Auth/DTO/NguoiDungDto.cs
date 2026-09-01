using Admin.Domain.Entities;
using AutoMapper;

namespace Admin.Application.Auth.DTO;

public class NguoiDungDto
{
    public Guid id { get; set; }
    public string taiKhoan { get; set; } = string.Empty;
    public string ten { get; set; } = string.Empty;
    public string? email { get; set; }
    public string? soDienThoai { get; set; }
    public bool isSuperAdmin { get; set; }
    public Guid? donViId { get; set; }
}

public class NguoiDungProfile : Profile
{
    public NguoiDungProfile()
    {
        CreateMap<nguoi_dung, NguoiDungDto>()
            .ForMember(d => d.taiKhoan, opt => opt.MapFrom(s => s.tai_khoan))
            .ForMember(d => d.ten, opt => opt.MapFrom(s => s.ten))
            .ForMember(d => d.email, opt => opt.MapFrom(s => s.email))
            .ForMember(d => d.soDienThoai, opt => opt.MapFrom(s => s.so_dien_thoai))
            .ForMember(d => d.isSuperAdmin, opt => opt.MapFrom(s => s.is_super_admin))
            .ForMember(d => d.donViId, opt => opt.MapFrom(s => s.don_vi_id));
    }
}
