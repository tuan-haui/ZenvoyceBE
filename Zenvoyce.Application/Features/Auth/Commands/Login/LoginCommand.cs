using AutoMapper;
using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Auth.DTOs;
using Zenvoyce.Application.Features.Users.DTOs;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Auth.Commands.Login;

public record LoginCommand(string Username, string Password) : IRequest<LoginResponseDto>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(5).Matches(@"^\S+$");
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginCommandHandler(
    IUserRepository userRepository,
    IUserPermissionRepository userPermissionRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IDateTimeProvider dateTimeProvider,
    IMapper mapper) : IRequestHandler<LoginCommand, LoginResponseDto>
{
    public async Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Password, user.Matkhau))
        {
            throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không đúng.");
        }

        if (user.Trangthai != 1)
        {
            throw new UnauthorizedAccessException("Tài khoản đang bị khóa.");
        }

        var now = dateTimeProvider.UtcNow;
        var expiredAt = now.AddMinutes(15);
        var roleIds = await userPermissionRepository.GetRoleIdsByUserIdAsync(user.Id, cancellationToken);
        var token = jwtTokenService.GenerateToken(user.Id, roleIds, expiredAt);

        return new LoginResponseDto
        {
            Token = token,
            ExpiredAt = expiredAt,
            UserInfo = mapper.Map<LoginUserInfoDto>(user)
        };
    }
}
