using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.Application.Commands;
using SharedKernel.Application.Exceptions;
using SharedKernel.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Todo.Application.DTO;
using Todo.Application.Interfaces;
using Todo.Domain.Entities;
using Todo.Domain.Enums;

namespace Todo.Application.Commands
{


    public record UpdateTodoCommand : IRequest<TodoDto>
    {
        public Guid id { get; set; }
        public string title { get; set; } = string.Empty;
        public int status { get; set; }
    }

    public class UpdateTodoCommandHandler : BaseCommandHandler<ITodoDbContext, cong_viec>, IRequestHandler<UpdateTodoCommand, TodoDto>
    {
        private readonly IIdentityService _identityService;

        public UpdateTodoCommandHandler(
            ITodoDbContext context,
            IMapper mapper,
            IMediator mediator,
            IConfiguration config,
            IIdentityService identityService)
            : base(context, mapper, mediator, config)
        {
            _identityService = identityService;
        }

        public async Task<TodoDto> Handle(UpdateTodoCommand request, CancellationToken cancellationToken)
        {
            var userId = _identityService.currentUser?.id
                ?? throw new RejectException(ErrorCode.Unauthorized, "Không xác định được người dùng đăng nhập!");

            var entity = await _repo.FirstOrDefaultAsync(x => x.id == request.id && x.nguoi_dung_id == userId, cancellationToken);

            if (entity == null) throw new RejectException(ErrorCode.NotFound, "Không tìm thấy công việc!");

            // update
            var trangThaiMoi = (TrangThaiCongViec)request.status;
            if(request.title.Trim() != "")
            {
                entity.tieu_de = request.title.Trim();
            }
            
            entity.trang_thai = trangThaiMoi;


            

            await _context.SaveChangesAsync(cancellationToken);

            return _mapper.Map<TodoDto>(entity);
        }
    }
}
