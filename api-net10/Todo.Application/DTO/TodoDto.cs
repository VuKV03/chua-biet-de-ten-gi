using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;
using Todo.Domain.Entities;

namespace Todo.Application.DTO
{
    public class TodoDto
    {
        public Guid id { get; set; }
        public string title { get; set; } = string.Empty; // = "" to avoid null reference
        public int status { get; set; }
        public DateTime? completedAt { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }

    }

    public class TodoProfile : Profile
    {
        public TodoProfile()
        {
            CreateMap<cong_viec, TodoDto>()
                .ForMember(d => d.title, opt => opt.MapFrom(s => s.tieu_de))
                .ForMember(d => d.status, opt => opt.MapFrom(s => (int)s.trang_thai))
                .ForMember(d => d.completedAt, opt => opt.MapFrom(s => s.ngay_hoan_thanh))
                .ForMember(d => d.createdAt, opt => opt.MapFrom(s => s.ngay_tao))
                .ForMember(d => d.updatedAt, opt => opt.MapFrom(s => s.ngay_chinh_sua));
        }
    }
}
