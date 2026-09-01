using MediatR;

namespace SharedKernel.Domain.Entities;

public abstract class BaseEvent : INotification
{
    public DateTime DateOccurred { get; protected set; } = DateTime.UtcNow;
}
