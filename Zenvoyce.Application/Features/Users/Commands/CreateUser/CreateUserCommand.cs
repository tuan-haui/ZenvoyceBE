using AutoMapper;
using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Users.DTOs;
using Zenvoyce.Domain.Entities;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Users.Commands.CreateUser;

public record CreateUserCommand(
    Guid? Madonvi,
    string Tendangnhap,
    string Matkhau,
    string? Dienthoai,
    short Trangthai) : IRequest<UserDto>;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Tendangnhap).NotEmpty().MinimumLength(5).Matches(@"^\S+$");
        RuleFor(x => x.Matkhau)
            .NotEmpty()
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).{8,}$");
    }
}

public class CreateUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IMapper mapper) : IRequestHandler<CreateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.UsernameExistsAsync(request.Tendangnhap, null, cancellationToken))
        {
            throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");
        }

        var now = dateTimeProvider.UtcNow;
        var user = new Nguoidung
        {
            Id = Guid.NewGuid(),
            Madonvi = request.Madonvi,
            Tendangnhap = request.Tendangnhap,
            Matkhau = passwordHasher.Hash(request.Matkhau),
            Dienthoai = request.Dienthoai,
            Trangthai = request.Trangthai,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = currentUserService.UserId,
            UpdatedBy = currentUserService.UserId,
            IsDeleted = false
        };

        await userRepository.AddAsync(user, cancellationToken);
        return mapper.Map<UserDto>(user);
    }
}
