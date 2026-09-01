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

public record LoginCommand : IRequest<AuthResponseDto>
{
    public string taiKhoan { get; set; } = string.Empty;
    public string matKhau { get; set; } = string.Empty;
    public string? diaChiIp { get; set; }
    public string? thongTinThietBi { get; set; }
}

public class LoginCommandHandler : BaseCommandHandler<IAdminDbContext, nguoi_dung>, IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IPasswordHasherService _hasher;
    private readonly IJwtTokenService _jwtService;

    public LoginCommandHandler(
        IAdminDbContext context,
        IMapper mapper,
        IMediator mediator,
        IConfiguration config,
        IPasswordHasherService hasher,
        IJwtTokenService jwtService)
        : base(context, mapper, mediator, config)
    {
        _hasher = hasher;
        _jwtService = jwtService;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var username = request.taiKhoan.Trim();
        var user = await _context.nguoi_dung
            .FirstOrDefaultAsync(u => u.tai_khoan == username, cancellationToken);

        if (user == null)
            throw new RejectException(ErrorCode.Invalid, "Tài khoản hoặc mật khẩu không chính xác.");

        if (!user.trang_thai)
            throw new RejectException(ErrorCode.Invalid, "Tài khoản đã bị vô hiệu hóa.");

        if (user.khoa_den_ngay.HasValue && user.khoa_den_ngay > DateTime.UtcNow)
        {
            var minutesLeft = (int)Math.Ceiling((user.khoa_den_ngay.Value - DateTime.UtcNow).TotalMinutes);
            throw new RejectException(ErrorCode.Invalid, $"Tài khoản tạm khóa do nhập sai nhiều lần. Vui lòng thử lại sau {minutesLeft} phút.");
        }

        var (isValid, _) = _hasher.VerifyPassword(request.matKhau.Trim(), user.mat_khau, user.salt_code, user.password_hash_type);

        if (!isValid)
        {
            user.so_lan_dang_nhap_sai += 1;
            var maxAttempts = _config.GetValue<int>("JwtSettings:MaxFailedAttempts", 5);
            var lockoutMinutes = _config.GetValue<int>("JwtSettings:LockoutMinutes", 15);

            if (user.so_lan_dang_nhap_sai >= maxAttempts)
            {
                user.khoa_den_ngay = DateTime.UtcNow.AddMinutes(lockoutMinutes);
            }

            await _context.SaveChangesAsync(cancellationToken);
            throw new RejectException(ErrorCode.Invalid, "Tài khoản hoặc mật khẩu không chính xác.");
        }

        // Đăng nhập thành công -> Reset lockout
        user.so_lan_dang_nhap_sai = 0;
        user.khoa_den_ngay = null;

        var currentUserDto = new CurrentUserDto
        {
            id = user.id,
            tai_khoan = user.tai_khoan,
            ten = user.ten,
            email = user.email,
            is_super_admin = user.is_super_admin,
            don_vi_id = user.don_vi_id?.ToString()
        };

        var (accessToken, jwtId, accessExpires) = _jwtService.GenerateAccessToken(currentUserDto);
        var (rawRefreshToken, tokenHash, refreshExpires) = _jwtService.GenerateRefreshToken();

        var tokenEntity = new nguoi_dung_refresh_token
        {
            id = Guid.NewGuid(),
            nguoi_dung_id = user.id,
            token_hash = tokenHash,
            jwt_id = jwtId,
            ngay_het_han = refreshExpires,
            dia_chi_ip = request.diaChiIp,
            thong_tin_thiet_bi = request.thongTinThietBi
        };

        _context.nguoi_dung_refresh_token.Add(tokenEntity);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            accessToken = accessToken,
            refreshToken = rawRefreshToken,
            expiresIn = (int)(accessExpires - DateTime.UtcNow).TotalSeconds,
            userInfo = _mapper.Map<NguoiDungDto>(user)
        };
    }
}
