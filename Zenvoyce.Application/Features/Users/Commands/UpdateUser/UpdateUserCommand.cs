using AutoMapper;
using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Users.DTOs;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Users.Commands.UpdateUser;

public record UpdateUserCommand(
    Guid Id,
    Guid? Madonvi,
    string? Hoten,
    string? Email,
    string? Dienthoai,
    short Trangthai) : IRequest<UserDto>;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email!).EmailAddress();
        });
    }
}

public class UpdateUserCommandHandler(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IMapper mapper) : IRequestHandler<UpdateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");

        if (!string.IsNullOrWhiteSpace(request.Email)
            && await userRepository.EmailExistsAsync(request.Email, request.Id, cancellationToken))
        {
            throw new InvalidOperationException("Email đã được sử dụng.");
        }

        user.Madonvi = request.Madonvi;
        user.Hoten = string.IsNullOrWhiteSpace(request.Hoten) ? null : request.Hoten.Trim();
        user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        user.Dienthoai = request.Dienthoai;
        user.Trangthai = request.Trangthai;
        user.UpdatedAt = dateTimeProvider.UtcNow;
        user.UpdatedBy = currentUserService.UserId;

        await userRepository.UpdateAsync(user, cancellationToken);
        return mapper.Map<UserDto>(user);
    }
}
