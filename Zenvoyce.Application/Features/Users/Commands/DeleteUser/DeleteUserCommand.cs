using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Users.Commands.DeleteUser;

public record DeleteUserCommand(Guid Id) : IRequest<Unit>;

public class DeleteUserCommandHandler(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<DeleteUserCommand, Unit>
{
    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");

        user.IsDeleted = true;
        user.UpdatedAt = dateTimeProvider.UtcNow;
        user.UpdatedBy = currentUserService.UserId;

        await userRepository.UpdateAsync(user, cancellationToken);
        return Unit.Value;
    }
}
