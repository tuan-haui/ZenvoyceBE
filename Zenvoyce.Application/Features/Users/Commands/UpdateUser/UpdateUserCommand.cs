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
    string? Dienthoai,
    short Trangthai) : IRequest<UserDto>;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
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

        user.Madonvi = request.Madonvi;
        user.Dienthoai = request.Dienthoai;
        user.Trangthai = request.Trangthai;
        user.UpdatedAt = dateTimeProvider.UtcNow;
        user.UpdatedBy = currentUserService.UserId;

        await userRepository.UpdateAsync(user, cancellationToken);
        return mapper.Map<UserDto>(user);
    }
}
