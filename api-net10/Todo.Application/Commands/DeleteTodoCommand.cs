using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.Application.Commands;
using SharedKernel.Application.Exceptions;
using SharedKernel.Application.Interfaces;
using Todo.Application.Interfaces;
using Todo.Domain.Entities;

namespace Todo.Application.Commands;

public record DeleteTodoCommand : IRequest<bool>
{
    public Guid id { get; set; }
}

public class DeleteTodoCommandHandler
    : BaseCommandHandler<ITodoDbContext, cong_viec>, IRequestHandler<DeleteTodoCommand, bool>
{
    private readonly IIdentityService _identityService;

    public DeleteTodoCommandHandler(
        ITodoDbContext context,
        IMapper mapper,
        IMediator mediator,
        IConfiguration config,
        IIdentityService identityService)
        : base(context, mapper, mediator, config)
    {
        _identityService = identityService;
    }

    public async Task<bool> Handle(DeleteTodoCommand request, CancellationToken cancellationToken)
    {
        var userId = _identityService.currentUser?.id
            ?? throw new RejectException(ErrorCode.Unauthorized, "Không xác định được người dùng đăng nhập.");

        var entity = await _context.cong_viec
            .FirstOrDefaultAsync(x => x.id == request.id && x.nguoi_dung_id == userId, cancellationToken);

        if (entity == null)
            throw new RejectException(ErrorCode.NotFound, "Không tìm thấy công việc.");

        _context.cong_viec.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}