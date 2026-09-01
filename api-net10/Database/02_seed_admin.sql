-- ═══════════════════════════════════════════════════════════
-- Seed tài khoản Admin đầu tiên
-- Tài khoản : admin
-- Mật khẩu  : 12345678aA@
--
-- Hash bên dưới được sinh THẬT bằng đúng thuật toán của
-- SharedKernel.Infrastructure/Services/PasswordHasherService.cs
-- (PBKDF2-HMACSHA512, 100.000 vòng lặp, salt 16 byte, subkey 32 byte,
-- 1 byte marker định dạng = 0x01, sau đó Base64) — KHÔNG phải hash mẫu
-- kiểu ASP.NET Core Identity, nên phải sinh lại nếu bạn đổi thuật toán hash.
-- ═══════════════════════════════════════════════════════════
USE `my_auth_db`;

INSERT INTO `nguoi_dung` (
    `id`, `tai_khoan`, `mat_khau`, `ten`, `email`, `trang_thai`, `is_super_admin`, `password_hash_type`
) VALUES (
    UUID(),
    'admin',
    'AW7vYPuWX/ayoYlHqJX/MHEQApY4jkc57XMV/JfQ8ILktqCiTuIYBi0F8cDvGqsFcA==',
    'Administrator',
    'admin@domain.com',
    1,
    1,
    1
) ON DUPLICATE KEY UPDATE
    `mat_khau` = VALUES(`mat_khau`),
    `trang_thai` = 1,
    `so_lan_dang_nhap_sai` = 0,
    `khoa_den_ngay` = NULL;
