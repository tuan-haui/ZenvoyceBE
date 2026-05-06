using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Services;
using Zenvoyce.Application.Features.Ai.DTOs;

namespace Zenvoyce.Application.Features.Ai.Commands.ChatWithVertexAi;

public record ChatWithVertexAiCommand(string Message) : IRequest<AiChatResponseDto>;

public sealed class ChatWithVertexAiCommandValidator : AbstractValidator<ChatWithVertexAiCommand>
{
    public ChatWithVertexAiCommandValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .MaximumLength(8000);
    }
}

public sealed class ChatWithVertexAiCommandHandler(IVertexAiService vertexAiService)
    : IRequestHandler<ChatWithVertexAiCommand, AiChatResponseDto>
{
    public Task<AiChatResponseDto> Handle(ChatWithVertexAiCommand request, CancellationToken cancellationToken)
    {
        return vertexAiService.ChatAsync(request.Message, cancellationToken);
    }
}
