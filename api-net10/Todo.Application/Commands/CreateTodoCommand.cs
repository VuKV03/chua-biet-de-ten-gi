using AutoMapper;
using MediatR;
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
    public record CreateTodoCommand : IRequest<TodoDto>
    {
        public string title { get; set; } = string.Empty; // = "" to avoid null reference
    }

    public class CreateTodoCommandHandler : BaseCommandHandler<ITodoDbContext, cong_viec>, IRequestHandler<CreateTodoCommand, TodoDto>
    {
        private readonly IIdentityService _identityService;

        public CreateTodoCommandHandler(
            ITodoDbContext context,
            IMapper mapper,
            IMediator mediator,
            IConfiguration config,
            IIdentityService identityService)
            : base(context, mapper, mediator, config)
        {
            _identityService = identityService;
        }
        
        public async Task<TodoDto> Handle(CreateTodoCommand request, CancellationToken cancellationToken)
        {
            var userId = _identityService.currentUser?.id
                ?? throw new RejectException(ErrorCode.Unauthorized, "Không xác định được người dùng đăng nhập!");

            var entity = new cong_viec
            {
                id = Guid.NewGuid(),
                nguoi_dung_id = userId,
                tieu_de = request.title.Trim(),
                trang_thai = TrangThaiCongViec.ChuaLam

            };

            _context.cong_viec.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return _mapper.Map<TodoDto>(entity);
        }
    }
}
