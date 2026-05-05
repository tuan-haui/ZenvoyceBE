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
/// Khởi tạo nhóm quyền + menu + SysGroupMenu + tài khoản admin (transaction).
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

            var allMenuIds = new List<Guid>();

            async Task<Guid> AddMenuAsync(string tenmenu, string? duongdan, Guid? menuchaId, int stt = 0)
            {
                var id = Guid.NewGuid();
                await menuRepository.AddAsync(new Sysmenu
                {
                    Id = id,
                    Tenmenu = tenmenu,
                    Duongdan = duongdan,
                    MenuchaId = menuchaId,
                    Icon = null,
                    Stt = stt
                }, cancellationToken);
                menusCreated++;
                allMenuIds.Add(id);
                return id;
            }

            var dashId = await AddMenuAsync("Dashboard", "/admin/dashboard", null, 1);

            var sysRoot = await AddMenuAsync("Hệ thống", null, null, 10);
            _ = await AddMenuAsync("Người dùng", "/admin/users", sysRoot, 11);
            _ = await AddMenuAsync("Nhóm quyền", "/admin/roles", sysRoot, 12);
            _ = await AddMenuAsync("Nhật ký hệ thống", "/admin/system/logs", sysRoot, 13);

            var catRoot = await AddMenuAsync("Danh mục", null, null, 20);
            var companiesId = await AddMenuAsync("Công ty", "/admin/companies", catRoot, 21);
            var customersId = await AddMenuAsync("Khách hàng", "/admin/customers", catRoot, 22);
            var productsId = await AddMenuAsync("Hàng hoá", "/admin/products", catRoot, 23);

            var tplRoot = await AddMenuAsync("Mẫu in", null, null, 30);
            _ = await AddMenuAsync("Cấu hình mẫu", "/admin/templates/setup", tplRoot, 31);
            _ = await AddMenuAsync("Kho mẫu", "/admin/templates/warehouse", tplRoot, 32);

            var invRoot = await AddMenuAsync("Hóa đơn", null, null, 40);
            var invoicesId = await AddMenuAsync("Danh sách hóa đơn", "/admin/invoices", invRoot, 41);
            var salesId = await AddMenuAsync("Báo cáo doanh thu", "/admin/reports/sales", invRoot, 42);

            await permissionRepository.AssignMenusToRoleAsync(adminRoleId, allMenuIds, null, cancellationToken);

            var staffMenuIds = new[]
            {
                dashId,
                catRoot,
                companiesId,
                customersId,
                productsId,
                invRoot,
                invoicesId,
                salesId
            };

            await permissionRepository.AssignMenusToRoleAsync(staffRoleId, staffMenuIds, null, cancellationToken);

            var adminUser = new Nguoidung
            {
                Id = Guid.NewGuid(),
                Madonvi = null,
                Quyenid = adminRoleId,
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
                AdminPermissionRows = allMenuIds.Count
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
