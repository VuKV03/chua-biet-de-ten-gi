using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.Application.DTO;
using SharedKernel.Application.Exceptions;
using SharedKernel.Application.Interfaces;
using SharedKernel.Application.Queries;
using Todo.Application.DTO;
using Todo.Application.Interfaces;
using Todo.Domain.Entities;
using Todo.Domain.Enums;

namespace Todo.Application.Queries
{
    public class GetTodoAllQuery : IRequest<PagedResult<TodoDto>>
    {
        public int? status { get; set; }
        public int page { get; set; } = 1;
        public int pageSize { get; set; } = 20;
    }

    public class GetTodoAllQueryHandler : BaseQueryHandler<ITodoDbContext, cong_viec>, IRequestHandler<GetTodoAllQuery, PagedResult<TodoDto>>
    {
        private readonly IIdentityService _identityService;

        public GetTodoAllQueryHandler(
            ITodoDbContext context,
            IMapper mapper,
            IConfiguration config,
            IIdentityService identifyService)
            : base(context, mapper, config)
        {
            _identityService = identifyService;
        }

        public async Task<PagedResult<TodoDto>> Handle(GetTodoAllQuery request, CancellationToken cancellationToken)
        {
            var userId = _identityService.currentUser?.id
                ?? throw new RejectException(ErrorCode.Unauthorized, "Không xác định được người dùng đăng nhập.");

            var query = _repo.Where(x => x.nguoi_dung_id == userId);

            if (request.status.HasValue)
            {
                var trangThai = (TrangThaiCongViec)request.status.Value;
                query = query.Where(x => x.trang_thai == trangThai);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var activeCount = await query.Where(x => x.trang_thai == TrangThaiCongViec.DangLam).CountAsync(cancellationToken);
            var completedCount = await query.Where(x => x.trang_thai == TrangThaiCongViec.HoanThanh).CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.ngay_tao)
                .Skip((request.page - 1) * request.pageSize)
                .Take(request.pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<TodoDto>
            {
                items = _mapper.Map<List<TodoDto>>(items),
                totalCount = totalCount,
                activeCount = activeCount,
                completedCount = completedCount,
                page = request.page,
                pageSize = request.pageSize
            };
        }
    }
}
