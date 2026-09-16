using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.Application.Exceptions;
using SharedKernel.Application.Interfaces;
using SharedKernel.Application.Queries;
using Todo.Application.DTO;
using Todo.Application.Interfaces;
using Todo.Domain.Entities;


namespace Todo.Application.Queries
{
    public record GetTodoByIdQuery : IRequest<TodoDto>
    {
        public Guid id { get; set; }
    }

    public class GetTodoByIdQueryHandler : BaseQueryHandler<ITodoDbContext, cong_viec>, IRequestHandler<GetTodoByIdQuery, TodoDto>
    {
        private readonly IIdentityService _identityService;

        public GetTodoByIdQueryHandler(
            ITodoDbContext context,
            IMapper mapper,
            IConfiguration config,
            IIdentityService identifyService)
            : base(context, mapper, config)
        {
            _identityService = identifyService;
        }

        public async Task<TodoDto> Handle(GetTodoByIdQuery request, CancellationToken cancellationToken)
        {
            var userId = _identityService.currentUser?.id
                ?? throw new RejectException(ErrorCode.Unauthorized, "Không xác định được người dùng đăng nhập!");

            var entity = await _repo.FirstOrDefaultAsync(x => x.id == request.id && x.nguoi_dung_id == userId, cancellationToken);

            if (entity == null) throw new RejectException(ErrorCode.NotFound, "Không tìm thấy công việc!");

            return _mapper.Map<TodoDto>(entity); 
        }
    }

}
