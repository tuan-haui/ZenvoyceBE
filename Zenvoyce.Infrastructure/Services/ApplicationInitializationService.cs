using Microsoft.Extensions.Options;
using Zenvoyce.Application.Abstractions;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.System.DTOs;
using Zenvoyce.Domain.Entities;
using Zenvoyce.Domain.Interfaces;
using Zenvoyce.Infrastructure.Options;
using ZenvoyceDbContext = Zenvoyce.Infrastructure.Entities.ZenvoyceDbContext;

namespace Zenvoyce.Infrastructure.Services;

/// <summary>
/// Khởi tạo nhóm quyền + menu + tài khoản admin và phân quyền sidebar (transaction).
/// </summary>
public class ApplicationInitializationService(
    ZenvoyceDbContext dbContext,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IMenuRepository menuRepository,
    IPermissionRepository permissionRepository,
    IOptions<BootstrapOptions> bootstrapOptions,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider) : IApplicationInitializationService
{
    public async Task<InitializeSystemResponseDto> TryInitializeAsync(CancellationToken cancellationToken)
    {
        if (await userRepository.CountAsync(cancellationToken) > 0)
        {
            return new InitializeSystemResponseDto
            {
                Initialized = false,
                Message = "Hệ thống đã có người dùng; không chạy khởi tạo bootstrap."
            };
        }

        var opts = bootstrapOptions.Value;
        var username = string.IsNullOrWhiteSpace(opts.AdminUsername) ? "admin" : opts.AdminUsername.Trim();
        var password = string.IsNullOrWhiteSpace(opts.AdminPassword) ? "Admin@123" : opts.AdminPassword;
        var fullName = string.IsNullOrWhiteSpace(opts.AdminFullName) ? "Quản trị viên" : opts.AdminFullName.Trim();

        if (await userRepository.UsernameExistsAsync(username, null, cancellationToken))
        {
            return new InitializeSystemResponseDto
            {
                Initialized = false,
                Message = $"Tài khoản '{username}' đã tồn tại; không bootstrap."
            };
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = dateTimeProvider.UtcNow;
            var rolesCreated = 0;
            var menusCreated = 0;

            var adminRoleId = Guid.NewGuid();
            var staffRoleId = Guid.NewGuid();

            await roleRepository.AddAsync(new Nhomquyen
            {
                Id = adminRoleId,
                Tenquyen = "Quản trị viên",
                Mota = "Toàn quyền quản trị hệ thống (bootstrap).",
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = null,
                UpdatedBy = null,
                IsDeleted = false
            }, cancellationToken);
            rolesCreated++;

            await roleRepository.AddAsync(new Nhomquyen
            {
                Id = staffRoleId,
                Tenquyen = "Nhân viên",
                Mota = "Quyền thao tác nghiệp vụ (danh mục + hóa đơn) — bootstrap.",
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = null,
                UpdatedBy = null,
                IsDeleted = false
            }, cancellationToken);
            rolesCreated++;

            var adminMenuIds = new List<Guid>();

            async Task<Guid> AddMenuAsync(Guid roleId, string tenmenu, string? duongdan, Guid? menuchaId)
            {
                var id = Guid.NewGuid();
                await menuRepository.AddAsync(new Sysmenu
                {
                    Id = id,
                    Tenmenu = tenmenu,
                    Duongdan = duongdan,
                    MenuchaId = menuchaId,
                    QuyenId = roleId
                }, cancellationToken);
                menusCreated++;
                if (roleId == adminRoleId)
                {
                    adminMenuIds.Add(id);
                }

                return id;
            }

            // --- Menu gốc: Quản trị viên ---
            _ = await AddMenuAsync(adminRoleId, "Dashboard", "/admin/dashboard", null);

            var sysRoot = await AddMenuAsync(adminRoleId, "Hệ thống", null, null);
            _ = await AddMenuAsync(adminRoleId, "Người dùng", "/admin/users", sysRoot);
            _ = await AddMenuAsync(adminRoleId, "Nhóm quyền", "/admin/roles", sysRoot);
            _ = await AddMenuAsync(adminRoleId, "Nhật ký hệ thống", "/admin/system/logs", sysRoot);

            var catRoot = await AddMenuAsync(adminRoleId, "Danh mục", null, null);
            _ = await AddMenuAsync(adminRoleId, "Công ty", "/admin/companies", catRoot);
            _ = await AddMenuAsync(adminRoleId, "Khách hàng", "/admin/customers", catRoot);
            _ = await AddMenuAsync(adminRoleId, "Sản phẩm", "/admin/products", catRoot);

            var tplRoot = await AddMenuAsync(adminRoleId, "Mẫu in", null, null);
            _ = await AddMenuAsync(adminRoleId, "Cấu hình mẫu", "/admin/templates/setup", tplRoot);
            _ = await AddMenuAsync(adminRoleId, "Kho mẫu", "/admin/templates/warehouse", tplRoot);

            var invRoot = await AddMenuAsync(adminRoleId, "Hóa đơn", null, null);
            _ = await AddMenuAsync(adminRoleId, "Danh sách hóa đơn", "/admin/invoices", invRoot);
            _ = await AddMenuAsync(adminRoleId, "Báo cáo doanh thu", "/admin/reports/sales", invRoot);

            // --- Menu nhân viên: dashboard + danh mục + hóa đơn (không hệ thống / không mẫu in chi tiết) ---
            _ = await AddMenuAsync(staffRoleId, "Dashboard", "/admin/dashboard", null);
            var stCatRoot = await AddMenuAsync(staffRoleId, "Danh mục", null, null);
            _ = await AddMenuAsync(staffRoleId, "Công ty", "/admin/companies", stCatRoot);
            _ = await AddMenuAsync(staffRoleId, "Khách hàng", "/admin/customers", stCatRoot);
            _ = await AddMenuAsync(staffRoleId, "Sản phẩm", "/admin/products", stCatRoot);
            var stInvRoot = await AddMenuAsync(staffRoleId, "Hóa đơn", null, null);
            _ = await AddMenuAsync(staffRoleId, "Danh sách hóa đơn", "/admin/invoices", stInvRoot);
            _ = await AddMenuAsync(staffRoleId, "Báo cáo doanh thu", "/admin/reports/sales", stInvRoot);

            var adminUser = new Nguoidung
            {
                Id = Guid.NewGuid(),
                Madonvi = null,
                Tendangnhap = username,
                Matkhau = passwordHasher.Hash(password),
                Hoten = fullName,
                Email = string.IsNullOrWhiteSpace(opts.AdminEmail) ? null : opts.AdminEmail.Trim(),
                Dienthoai = null,
                Trangthai = 1,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = null,
                UpdatedBy = null,
                IsDeleted = false
            };

            await userRepository.AddAsync(adminUser, cancellationToken);

            await permissionRepository.AssignMenusAsync(
                adminRoleId,
                adminUser.Id,
                adminMenuIds,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new InitializeSystemResponseDto
            {
                Initialized = true,
                Message =
                    $"Đã khởi tạo hệ thống. Đăng nhập bằng '{username}' và đổi mật khẩu ngay. " +
                    $"Nhóm quyền mặc định: Quản trị viên, Nhân viên.",
                AdminUserId = adminUser.Id,
                RolesCreated = rolesCreated,
                MenusCreated = menusCreated,
                AdminPermissionRows = adminMenuIds.Count
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
