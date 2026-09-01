using System.Security.Cryptography;
using System.Text;
using Admin.Application.Interfaces;
using Admin.Domain.Entities;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.Application.Commands;

namespace Admin.Application.Auth.Commands;

public record LogoutCommand : IRequest<bool>
{
    public string refreshToken { get; set; } = string.Empty;
    public bool logoutAllDevices { get; set; } = false;
    public Guid? userId { get; set; }
}

public class LogoutCommandHandler : BaseCommandHandler<IAdminDbContext, nguoi_dung>, IRequestHandler<LogoutCommand, bool>
{
    public LogoutCommandHandler(IAdminDbContext context, IMapper mapper, IMediator mediator, IConfiguration config)
        : base(context, mapper, mediator, config) { }

    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (request.logoutAllDevices && request.userId.HasValue)
        {
            var activeTokens = await _context.nguoi_dung_refresh_token
                .Where(t => t.nguoi_dung_id == request.userId.Value && !t.da_thu_hoi)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens) token.da_thu_hoi = true;
        }
        else if (!string.IsNullOrEmpty(request.refreshToken))
        {
            var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(request.refreshToken)));

            var storedToken = await _context.nguoi_dung_refresh_token
                .FirstOrDefaultAsync(t => t.token_hash == tokenHash, cancellationToken);

            if (storedToken != null) storedToken.da_thu_hoi = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
