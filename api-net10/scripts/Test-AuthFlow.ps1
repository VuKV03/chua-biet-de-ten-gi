<#
.SYNOPSIS
    Test end-to-end toàn bộ 4 API của AuthController (login / refresh-token / me / logout),
    bao gồm cả các case bảo mật quan trọng: token rotation, phát hiện replay attack, revoke khi logout.

.DESCRIPTION
    Không dùng framework test (xUnit...) để chạy được ngay bằng PowerShell có sẵn trên Windows,
    không cần cài thêm gì. Gọi thẳng vào API đang chạy (dotnet run) qua HTTP.

.PARAMETER BaseUrl
    Địa chỉ gốc của Admin.Api đang chạy. Mặc định http://localhost:5043 (theo launchSettings.json).

.PARAMETER TaiKhoan / MatKhau
    Tài khoản dùng để test — mặc định là tài khoản seed trong Database/02_seed_admin.sql.

.PARAMETER TestLockout
    Bật thêm case đăng nhập sai liên tục để kích hoạt khóa tài khoản (MaxFailedAttempts).
    MẶC ĐỊNH TẮT vì sau khi chạy, tài khoản test sẽ bị khóa thật trong LockoutMinutes phút
    (mặc định 15p, xem appsettings.json) — chỉ bật khi bạn chấp nhận việc đó, hoặc dùng
    tài khoản test riêng không phải "admin" thật đang dùng để demo.

.EXAMPLE
    powershell -File scripts/Test-AuthFlow.ps1
    powershell -File scripts/Test-AuthFlow.ps1 -BaseUrl http://localhost:5043 -TestLockout
#>
param(
    [string]$BaseUrl = "http://localhost:5043",
    [string]$TaiKhoan = "admin",
    [string]$MatKhau = "12345678aA@",
    [switch]$TestLockout
)

$ErrorActionPreference = "Stop"
$script:PassCount = 0
$script:FailCount = 0

function Write-Result {
    param([string]$Name, [bool]$Ok, [string]$Detail = "")
    if ($Ok) {
        Write-Host "[PASS] $Name" -ForegroundColor Green
        $script:PassCount++
    }
    else {
        Write-Host "[FAIL] $Name" -ForegroundColor Red
        if ($Detail) { Write-Host "       -> $Detail" -ForegroundColor Yellow }
        $script:FailCount++
    }
}

# Gọi API kiểu "raw": không throw khi status != 2xx, luôn trả về Status + Json để tự assert.
# Windows PowerShell 5.1 (Invoke-WebRequest) throw terminating error trên status lỗi nên phải bọc try/catch
# và tự đọc lại response stream để lấy status code + body thật.
function Invoke-Api {
    param(
        [string]$Method,
        [string]$Path,
        [object]$Body = $null,
        [string]$Token = $null
    )
    $uri = "$BaseUrl$Path"
    $headers = @{ }
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }

    $bodyJson = $null
    if ($null -ne $Body) { $bodyJson = $Body | ConvertTo-Json -Depth 6 }

    try {
        if ($null -ne $bodyJson) {
            $resp = Invoke-WebRequest -Uri $uri -Method $Method -Headers $headers `
                -ContentType "application/json" -Body $bodyJson -UseBasicParsing
        }
        else {
            $resp = Invoke-WebRequest -Uri $uri -Method $Method -Headers $headers -UseBasicParsing
        }
        $status = [int]$resp.StatusCode
        $text = $resp.Content
    }
    catch {
        if ($_.Exception.Response) {
            $status = [int]$_.Exception.Response.StatusCode
            try {
                $stream = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($stream)
                $text = $reader.ReadToEnd()
            }
            catch { $text = "" }
        }
        else {
            # Không kết nối được server (chưa dotnet run, sai port...)
            $status = -1
            $text = $_.Exception.Message
        }
    }

    $obj = $null
    if ($text) {
        try { $obj = $text | ConvertFrom-Json } catch { $obj = $null }
    }
    return [PSCustomObject]@{ Status = $status; Text = $text; Json = $obj }
}

Write-Host "=== Test-AuthFlow: $BaseUrl (tai_khoan=$TaiKhoan) ===" -ForegroundColor Cyan

# 0. Server có sống không?
$ping = Invoke-Api -Method GET -Path "/swagger/v1/swagger.json"
if ($ping.Status -eq -1) {
    Write-Host "Không kết nối được tới $BaseUrl — hãy chạy 'dotnet run' trong Admin.Api trước." -ForegroundColor Red
    exit 1
}
Write-Result "Server đang chạy (Swagger JSON phản hồi)" ($ping.Status -eq 200) "status=$($ping.Status)"

# 1. Login: body rỗng -> phải bị FluentValidation chặn (400), KHÔNG được để lọt xuống DB
$r = Invoke-Api -Method POST -Path "/api/auth/login" -Body @{ taiKhoan = ""; matKhau = "" }
Write-Result "POST /login body rỗng -> 400 (validation)" ($r.Status -eq 400) "status=$($r.Status) body=$($r.Text)"

# 2. Login sai mật khẩu -> 400, KHÔNG được lộ accessToken
$r = Invoke-Api -Method POST -Path "/api/auth/login" -Body @{ taiKhoan = $TaiKhoan; matKhau = "sai-mat-khau-123" }
Write-Result "POST /login sai mật khẩu -> 400" ($r.Status -eq 400) "status=$($r.Status) body=$($r.Text)"

# 3. Login sai tài khoản -> 400
$r = Invoke-Api -Method POST -Path "/api/auth/login" -Body @{ taiKhoan = "khong-ton-tai-xyz"; matKhau = "12345678aA@" }
Write-Result "POST /login tài khoản không tồn tại -> 400" ($r.Status -eq 400) "status=$($r.Status) body=$($r.Text)"

# 4. Login đúng -> 200 + có đủ accessToken/refreshToken/userInfo
$r = Invoke-Api -Method POST -Path "/api/auth/login" -Body @{ taiKhoan = $TaiKhoan; matKhau = $MatKhau }
$loginOk = ($r.Status -eq 200) -and $r.Json -and $r.Json.accessToken -and $r.Json.refreshToken -and $r.Json.userInfo
Write-Result "POST /login đúng tài khoản/mật khẩu -> 200 + đủ token" $loginOk "status=$($r.Status) body=$($r.Text)"
if (-not $loginOk) {
    Write-Host "`nDừng test sớm vì login thất bại — không thể test tiếp các API cần token." -ForegroundColor Red
    Write-Host "Kiểm tra: đã chạy Database/01_schema.sql + 02_seed_admin.sql chưa? ConnectionStrings trong appsettings.json đã đúng chưa?" -ForegroundColor Yellow
    Write-Host "`n=== Kết quả: $script:PassCount PASS / $script:FailCount FAIL ===" -ForegroundColor Cyan
    exit 1
}
$accessToken1 = $r.Json.accessToken
$refreshToken1 = $r.Json.refreshToken
Write-Result "userInfo.taiKhoan khớp tài khoản vừa đăng nhập" ($r.Json.userInfo.taiKhoan -eq $TaiKhoan) "userInfo=$($r.Json.userInfo | ConvertTo-Json -Compress)"

# 5. GET /me không có token -> 401
$r = Invoke-Api -Method GET -Path "/api/auth/me"
Write-Result "GET /me không có Authorization -> 401" ($r.Status -eq 401) "status=$($r.Status)"

# 6. GET /me với accessToken hợp lệ -> 200 + đúng tai_khoan
$r = Invoke-Api -Method GET -Path "/api/auth/me" -Token $accessToken1
$meOk = ($r.Status -eq 200) -and $r.Json -and ($r.Json.tai_khoan -eq $TaiKhoan)
Write-Result "GET /me với accessToken hợp lệ -> 200 + đúng user" $meOk "status=$($r.Status) body=$($r.Text)"

# 7. GET /me với token bị sửa (sai chữ ký) -> 401
$tamperedToken = $accessToken1.Substring(0, $accessToken1.Length - 5) + "AAAAA"
$r = Invoke-Api -Method GET -Path "/api/auth/me" -Token $tamperedToken
Write-Result "GET /me với token bị sửa chữ ký -> 401" ($r.Status -eq 401) "status=$($r.Status)"

# 8. Refresh token hợp lệ -> 200 + token MỚI khác token cũ (rotation)
$r = Invoke-Api -Method POST -Path "/api/auth/refresh-token" -Body @{ accessToken = $accessToken1; refreshToken = $refreshToken1 }
$refreshOk = ($r.Status -eq 200) -and $r.Json -and $r.Json.accessToken -and $r.Json.refreshToken
Write-Result "POST /refresh-token hợp lệ -> 200" $refreshOk "status=$($r.Status) body=$($r.Text)"
if ($refreshOk) {
    $accessToken2 = $r.Json.accessToken
    $refreshToken2 = $r.Json.refreshToken
    Write-Result "Token rotation: accessToken mới khác accessToken cũ" ($accessToken2 -ne $accessToken1)
    Write-Result "Token rotation: refreshToken mới khác refreshToken cũ" ($refreshToken2 -ne $refreshToken1)
}

# 9. Refresh LẠI bằng refreshToken1 (đã bị rotate ở bước 8) -> phải bị từ chối (replay attack)
$r = Invoke-Api -Method POST -Path "/api/auth/refresh-token" -Body @{ accessToken = $accessToken1; refreshToken = $refreshToken1 }
Write-Result "Dùng lại refreshToken đã rotate -> bị từ chối (400, phát hiện replay)" ($r.Status -eq 400) "status=$($r.Status) body=$($r.Text)"

# 10. Vì replay attack ở bước 9 phải thu hồi TOÀN BỘ token của user -> refreshToken2 (vừa cấp ở bước 8) cũng phải bị vô hiệu
if ($refreshOk) {
    $r = Invoke-Api -Method POST -Path "/api/auth/refresh-token" -Body @{ accessToken = $accessToken2; refreshToken = $refreshToken2 }
    Write-Result "Sau replay attack: refreshToken2 (cấp trước đó) cũng đã bị thu hồi -> 400" ($r.Status -eq 400) "status=$($r.Status) body=$($r.Text)"
}

# 11. refreshToken không tồn tại trong DB -> 400
$r = Invoke-Api -Method POST -Path "/api/auth/refresh-token" -Body @{ accessToken = $accessToken1; refreshToken = "token-khong-ton-tai-xyz" }
Write-Result "POST /refresh-token với refreshToken không tồn tại -> 400" ($r.Status -eq 400) "status=$($r.Status) body=$($r.Text)"

# 12. Login lại lấy cặp token sạch để test logout (vì token cũ đã bị revoke hết ở các bước trên)
$r = Invoke-Api -Method POST -Path "/api/auth/login" -Body @{ taiKhoan = $TaiKhoan; matKhau = $MatKhau }
$loginOk2 = ($r.Status -eq 200) -and $r.Json.accessToken -and $r.Json.refreshToken
Write-Result "Login lại lấy token sạch cho phần test logout" $loginOk2 "status=$($r.Status)"
if ($loginOk2) {
    $accessToken3 = $r.Json.accessToken
    $refreshToken3 = $r.Json.refreshToken

    # 13. Logout không có Authorization -> 401
    $r = Invoke-Api -Method POST -Path "/api/auth/logout" -Body @{ refreshToken = $refreshToken3 }
    Write-Result "POST /logout không có Authorization -> 401" ($r.Status -eq 401) "status=$($r.Status)"

    # 14. Logout hợp lệ -> 200
    $r = Invoke-Api -Method POST -Path "/api/auth/logout" -Body @{ refreshToken = $refreshToken3 } -Token $accessToken3
    Write-Result "POST /logout hợp lệ -> 200" ($r.Status -eq 200) "status=$($r.Status) body=$($r.Text)"

    # 15. Sau logout, refreshToken3 phải KHÔNG dùng lại được nữa
    $r = Invoke-Api -Method POST -Path "/api/auth/refresh-token" -Body @{ accessToken = $accessToken3; refreshToken = $refreshToken3 }
    Write-Result "Sau logout: refreshToken đã revoke không dùng lại được -> 400" ($r.Status -eq 400) "status=$($r.Status) body=$($r.Text)"

    # 16. (Thông tin, KHÔNG phải lỗi) accessToken vẫn còn hiệu lực tới khi hết hạn dù đã logout
    #     -> đúng bản chất JWT stateless: logout chỉ thu hồi refresh token, không blacklist access token.
    $r = Invoke-Api -Method GET -Path "/api/auth/me" -Token $accessToken3
    if ($r.Status -eq 200) {
        Write-Host "[INFO] accessToken vẫn dùng được sau logout tới khi hết hạn (AccessTokenExpiryMinutes) — đây là hành vi thiết kế của JWT stateless, không phải lỗi." -ForegroundColor DarkYellow
    }
}

if ($TestLockout) {
    Write-Host "`n--- TestLockout: sẽ khóa tài khoản '$TaiKhoan' trong LockoutMinutes phút ---" -ForegroundColor Magenta
    $locked = $false
    for ($i = 1; $i -le 6; $i++) {
        $r = Invoke-Api -Method POST -Path "/api/auth/login" -Body @{ taiKhoan = $TaiKhoan; matKhau = "sai-$i" }
        if ($i -le 5) {
            Write-Result "Lần đăng nhập sai #$i -> 400" ($r.Status -eq 400) "status=$($r.Status)"
        }
        else {
            # Lần thứ 6: đã vượt MaxFailedAttempts (mặc định 5) -> tài khoản bị khóa
            Write-Result "Lần thứ 6 (đã vượt MaxFailedAttempts) -> vẫn 400 do bị khóa" ($r.Status -eq 400) "status=$($r.Status) body=$($r.Text)"
        }
    }
    # Thử đăng nhập ĐÚNG mật khẩu trong lúc đang bị khóa -> vẫn phải bị từ chối
    $r = Invoke-Api -Method POST -Path "/api/auth/login" -Body @{ taiKhoan = $TaiKhoan; matKhau = $MatKhau }
    Write-Result "Đăng nhập ĐÚNG mật khẩu trong lúc đang khóa -> vẫn 400" ($r.Status -eq 400) "status=$($r.Status) body=$($r.Text)"
    Write-Host "Tài khoản '$TaiKhoan' hiện đang bị khóa thật. Muốn mở khóa ngay: chạy SQL sau trên my_auth_db:" -ForegroundColor Yellow
    Write-Host "  UPDATE nguoi_dung SET so_lan_dang_nhap_sai=0, khoa_den_ngay=NULL WHERE tai_khoan='$TaiKhoan';" -ForegroundColor Yellow
}

Write-Host "`n=== Kết quả: $script:PassCount PASS / $script:FailCount FAIL ===" -ForegroundColor Cyan
if ($script:FailCount -gt 0) { exit 1 } else { exit 0 }
