using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Users.Commands.ChangePassword;

public record ChangePasswordCommand(Guid Id, string OldPassword, string NewPassword) : IRequest<Unit>;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.OldPassword).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).{8,}$");
    }
}

public class ChangePasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<ChangePasswordCommand, Unit>
{
    public async Task<Unit> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");

        if (!passwordHasher.Verify(request.OldPassword, user.Matkhau))
        {
            throw new InvalidOperationException("Mật khẩu cũ không chính xác.");
        }

        user.Matkhau = passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = dateTimeProvider.UtcNow;
        user.UpdatedBy = currentUserService.UserId;

        await userRepository.UpdateAsync(user, cancellationToken);
        return Unit.Value;
    }
}
