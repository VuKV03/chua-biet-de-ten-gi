using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.Application.Interfaces;

namespace SharedKernel.Application.Commands;

public class BaseCommandHandler<TDbContext, TEntity>
    where TDbContext : IBaseDbContext
    where TEntity : class
{
    protected readonly TDbContext _context;
    protected readonly IMapper _mapper;
    protected readonly DbSet<TEntity> _repo;
    protected readonly IMediator _mediator;
    protected readonly IConfiguration _config;

    public BaseCommandHandler(TDbContext context, IMapper mapper, IMediator mediator, IConfiguration config)
    {
        _context = context;
        _mapper = mapper;
        _mediator = mediator;
        _repo = context.Set<TEntity>();
        _config = config;
    }
}
