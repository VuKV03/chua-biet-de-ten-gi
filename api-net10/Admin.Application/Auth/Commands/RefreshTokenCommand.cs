using System.Security.Cryptography;
using System.Text;
using Admin.Application.Auth.DTO;
using Admin.Application.Interfaces;
using Admin.Domain.Entities;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.Application.Commands;
using SharedKernel.Application.DTO;
using SharedKernel.Application.Exceptions;
using SharedKernel.Application.Interfaces;

namespace Admin.Application.Auth.Commands;

public record RefreshTokenCommand : IRequest<AuthResponseDto>
{
    public string accessToken { get; set; } = string.Empty;
    public string refreshToken { get; set; } = string.Empty;
}

public class RefreshTokenCommandHandler : BaseCommandHandler<IAdminDbContext, nguoi_dung>, IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IJwtTokenService _jwtService;

    public RefreshTokenCommandHandler(
        IAdminDbContext context,
        IMapper mapper,
        IMediator mediator,
        IConfiguration config,
        IJwtTokenService jwtService)
        : base(context, mapper, mediator, config)
    {
        _jwtService = jwtService;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var principal = _jwtService.GetPrincipalFromExpiredToken(request.accessToken);
        if (principal == null)
            throw new RejectException(ErrorCode.Invalid, "Access token không hợp lệ.");

        var jwtId = principal.Claims.FirstOrDefault(c => c.Type == "jti")?.Value;
        var userIdStr = principal.Claims.FirstOrDefault(c => c.Type == "id")?.Value;

        if (string.IsNullOrEmpty(jwtId) || !Guid.TryParse(userIdStr, out var userId))
            throw new RejectException(ErrorCode.Invalid, "Token không chứa định danh hợp lệ.");

        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(request.refreshToken)));

        var storedToken = await _context.nguoi_dung_refresh_token
            .FirstOrDefaultAsync(t => t.token_hash == tokenHash && t.jwt_id == jwtId, cancellationToken);

        if (storedToken == null)
            throw new RejectException(ErrorCode.Invalid, "Refresh token không tồn tại.");

        // Phát hiện Replay Attack (Token đã bị dùng lại) -> Thu hồi tất cả token của User
        if (storedToken.da_su_dung || storedToken.da_thu_hoi)
        {
            var userTokens = await _context.nguoi_dung_refresh_token
                .Where(t => t.nguoi_dung_id == storedToken.nguoi_dung_id && !t.da_thu_hoi)
                .ToListAsync(cancellationToken);

            foreach (var t in userTokens) t.da_thu_hoi = true;
            await _context.SaveChangesAsync(cancellationToken);

            throw new RejectException(ErrorCode.Invalid, "Cảnh báo bảo mật: Phiên đăng nhập đã bị hủy. Vui lòng đăng nhập lại.");
        }

        if (storedToken.IsExpired)
            throw new RejectException(ErrorCode.Invalid, "Refresh token đã hết hạn. Vui lòng đăng nhập lại.");

        // Token Rotation: Thu hồi token cũ và sinh token mới
        storedToken.da_su_dung = true;

        var user = await _context.nguoi_dung.FirstOrDefaultAsync(u => u.id == userId, cancellationToken);
        if (user == null || !user.trang_thai)
            throw new RejectException(ErrorCode.Invalid, "Tài khoản không hợp lệ.");

        var currentUserDto = new CurrentUserDto
        {
            id = user.id,
            tai_khoan = user.tai_khoan,
            ten = user.ten,
            email = user.email,
            is_super_admin = user.is_super_admin,
            don_vi_id = user.don_vi_id?.ToString()
        };

        var (newAccessToken, newJwtId, accessExpires) = _jwtService.GenerateAccessToken(currentUserDto);
        var (newRawRefresh, newTokenHash, refreshExpires) = _jwtService.GenerateRefreshToken();

        storedToken.thay_the_boi_token = newTokenHash;

        var newRefreshTokenEntity = new nguoi_dung_refresh_token
        {
            id = Guid.NewGuid(),
            nguoi_dung_id = user.id,
            token_hash = newTokenHash,
            jwt_id = newJwtId,
            ngay_het_han = refreshExpires,
            dia_chi_ip = storedToken.dia_chi_ip,
            thong_tin_thiet_bi = storedToken.thong_tin_thiet_bi
        };

        _context.nguoi_dung_refresh_token.Add(newRefreshTokenEntity);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            accessToken = newAccessToken,
            refreshToken = newRawRefresh,
            expiresIn = (int)(accessExpires - DateTime.UtcNow).TotalSeconds,
            userInfo = _mapper.Map<NguoiDungDto>(user)
        };
    }
}
