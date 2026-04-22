namespace Zenvoyce.Domain.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
