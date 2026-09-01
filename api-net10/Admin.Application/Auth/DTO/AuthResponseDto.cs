namespace Admin.Application.Auth.DTO;

public class AuthResponseDto
{
    public string accessToken { get; set; } = string.Empty;
    public string refreshToken { get; set; } = string.Empty;
    public string tokenType { get; set; } = "Bearer";
    public int expiresIn { get; set; }
    public NguoiDungDto? userInfo { get; set; }
}
