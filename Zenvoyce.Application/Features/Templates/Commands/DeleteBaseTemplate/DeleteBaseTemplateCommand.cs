using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Templates.Commands.DeleteBaseTemplate;

public record DeleteBaseTemplateCommand(Guid Id) : IRequest<Unit>;

public class DeleteBaseTemplateCommandValidator : AbstractValidator<DeleteBaseTemplateCommand>
{
    public DeleteBaseTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteBaseTemplateCommandHandler(
    ITemplateRepository templateRepository,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<DeleteBaseTemplateCommand, Unit>
{
    public async Task<Unit> Handle(DeleteBaseTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await templateRepository.GetBaseTemplateByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy mẫu hóa đơn gốc.");

        //if (await templateRepository.IsBaseTemplateInUseAsync(template.Id, cancellationToken))
        //{
        //    throw new InvalidOperationException("Không thể xóa mẫu hóa đơn gốc vì đã được đưa vào sử dụng.");
        //}

        await templateRepository.DeleteBaseTemplateAsync(
            template.Id,
            dateTimeProvider.UtcNow,
            currentUserService.UserId,
            cancellationToken);

        return Unit.Value;
    }
}
