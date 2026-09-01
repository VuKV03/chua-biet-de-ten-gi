-- ═══════════════════════════════════════════════════════════
-- Phase 1 — Schema khởi tạo Database MySQL cho MyAuthProject
-- Chạy trên MySQL 8.0+ / MariaDB 10.6+
-- ═══════════════════════════════════════════════════════════
CREATE DATABASE IF NOT EXISTS `my_auth_db`
    CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `my_auth_db`;

-- ═══════════════════════════════════════
-- Bảng 1: nguoi_dung
-- ═══════════════════════════════════════
CREATE TABLE `nguoi_dung` (
    `id`                     char(36)     NOT NULL,
    `tai_khoan`              varchar(255) NOT NULL,
    `mat_khau`               varchar(512) NOT NULL,
    `salt_code`              varchar(255) NULL DEFAULT '',
    `ten`                    varchar(255) NOT NULL,
    `email`                  varchar(255) NULL,
    `so_dien_thoai`          varchar(32)  NULL,
    `gioi_tinh`              tinyint(1)   NULL,
    `ngay_sinh`              datetime     NULL,
    `don_vi_id`              char(36)     NULL,
    `chuc_vu`                varchar(255) NULL,
    `trang_thai`             tinyint(1)   NOT NULL DEFAULT 1, -- 1: Hoạt động, 0: Khóa
    `is_super_admin`         tinyint(1)   NOT NULL DEFAULT 0,
    `is_doi_mk`              tinyint(1)   NULL DEFAULT 0,
    `ngay_doi_mk_gan_nhat`   datetime     NULL,
    `so_lan_dang_nhap_sai`   int          NOT NULL DEFAULT 0,
    `khoa_den_ngay`          datetime     NULL,
    `password_hash_type`     int          NOT NULL DEFAULT 1, -- 1: PBKDF2/HMACSHA512
    `ngay_tao`               datetime     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `nguoi_tao`              varchar(255) NULL,
    `ngay_chinh_sua`         datetime     NULL,
    `nguoi_chinh_sua`        varchar(255) NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `UX_tai_khoan` (`tai_khoan`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ═══════════════════════════════════════
-- Bảng 2: nguoi_dung_refresh_token
-- ═══════════════════════════════════════
CREATE TABLE `nguoi_dung_refresh_token` (
    `id`                     char(36)     NOT NULL,
    `nguoi_dung_id`          char(36)     NOT NULL,
    `token_hash`             varchar(255) NOT NULL, -- SHA256 Hash (Base64)
    `jwt_id`                 varchar(128) NOT NULL, -- Jti của Access Token
    `ngay_tao`               datetime     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `ngay_het_han`           datetime     NOT NULL,
    `da_su_dung`             tinyint(1)   NOT NULL DEFAULT 0,
    `da_thu_hoi`             tinyint(1)   NOT NULL DEFAULT 0,
    `dia_chi_ip`             varchar(45)  NULL,
    `thong_tin_thiet_bi`     varchar(500) NULL,
    `thay_the_boi_token`     varchar(255) NULL,
    `nguoi_tao`              varchar(255) NULL,
    `ngay_chinh_sua`         datetime     NULL,
    `nguoi_chinh_sua`        varchar(255) NULL,
    PRIMARY KEY (`id`),
    INDEX `IX_nguoi_dung_id` (`nguoi_dung_id`),
    INDEX `IX_token_hash` (`token_hash`),
    CONSTRAINT `FK_refresh_token_nguoi_dung`
        FOREIGN KEY (`nguoi_dung_id`) REFERENCES `nguoi_dung`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
