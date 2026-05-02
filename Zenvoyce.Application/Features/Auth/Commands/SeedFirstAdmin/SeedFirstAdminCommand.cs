using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Auth.DTOs;
using Zenvoyce.Domain.Entities;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Auth.Commands.SeedFirstAdmin;

/// <summary>
/// Tạo tài khoản admin đầu tiên khi DB chưa có người dùng (bootstrap).
/// </summary>
public record SeedFirstAdminCommand : IRequest<SeedFirstAdminResponseDto>;

public class SeedFirstAdminCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IMenuRepository menuRepository,
    IPermissionRepository permissionRepository,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<SeedFirstAdminCommand, SeedFirstAdminResponseDto>
{
    private const string BootstrapUsername = "admin";
    private const string BootstrapPassword = "Admin@123";
    private const string DefaultAdminRoleName = "Quản trị viên";

    public async Task<SeedFirstAdminResponseDto> Handle(SeedFirstAdminCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.CountAsync(cancellationToken) > 0)
        {
            return new SeedFirstAdminResponseDto
            {
                Seeded = false,
                Message = "Hệ thống đã có người dùng; không bootstrap thêm admin."
            };
        }

        if (await userRepository.UsernameExistsAsync(BootstrapUsername, null, cancellationToken))
        {
            return new SeedFirstAdminResponseDto
            {
                Seeded = false,
                Message = "Tài khoản admin đã tồn tại."
            };
        }

        var menus = (await menuRepository.GetAllMenusAsync(cancellationToken)).ToList();
        var roles = (await roleRepository.GetAllAsync(cancellationToken)).ToList();
        var adminRoleId = await ResolveAdminRoleIdAsync(roles, menus, cancellationToken);

        var now = dateTimeProvider.UtcNow;
        var user = new Nguoidung
        {
            Id = Guid.NewGuid(),
            Madonvi = null,
            Tendangnhap = BootstrapUsername,
            Matkhau = passwordHasher.Hash(BootstrapPassword),
            Hoten = "Quản trị viên",
            Email = null,
            Dienthoai = null,
            Trangthai = 1,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = null,
            UpdatedBy = null,
            IsDeleted = false
        };

        await userRepository.AddAsync(user, cancellationToken);

        if (menus.Count > 0)
        {
            var menuIds = menus.Select(m => m.Id).ToArray();
            await permissionRepository.AssignMenusAsync(adminRoleId, user.Id, menuIds, cancellationToken);
        }

        return new SeedFirstAdminResponseDto
        {
            Seeded = true,
            Message = $"Đã tạo tài khoản {BootstrapUsername}. Nên đăng nhập và đổi mật khẩu ngay.",
            UserId = user.Id
        };
    }

    private async Task<Guid> ResolveAdminRoleIdAsync(
        List<Nhomquyen> roles,
        List<Sysmenu> menus,
        CancellationToken cancellationToken)
    {
        var rankingMenuRoleIds = menus
            .Where(m => m.QuyenId.HasValue)
            .GroupBy(m => m.QuyenId!.Value)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key);

        foreach (var roleId in rankingMenuRoleIds)
        {
            if (await roleRepository.ExistsAsync(roleId, cancellationToken))
            {
                return roleId;
            }
        }

        var fallbackFromMenus = roles
            .Select(r => r.Id)
            .FirstOrDefault(rid => menus.Any(m => m.QuyenId == rid));

        if (fallbackFromMenus != Guid.Empty)
        {
            return fallbackFromMenus;
        }

        if (roles.Count > 0)
        {
            return roles[0].Id;
        }

        var newRoleId = Guid.NewGuid();
        var now = dateTimeProvider.UtcNow;
        await roleRepository.AddAsync(new Nhomquyen
        {
            Id = newRoleId,
            Tenquyen = DefaultAdminRoleName,
            Mota = "Được tạo tự động khi khởi tạo admin đầu tiên.",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = null,
            UpdatedBy = null,
            IsDeleted = false
        }, cancellationToken);

        return newRoleId;
    }
}
