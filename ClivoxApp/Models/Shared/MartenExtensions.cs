using Marten;

namespace ClivoxApp.Models.Shared;

public static class MartenExtensions
{
    public static Guid StoreEvents<T>(this IDocumentSession session, Guid? streamId, List<DomainEvent> events, string? createdBy)
        where T : class, IAggregateRoot
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(events);

        var actualStreamId = streamId ?? session.Events.StartStream<T>(events).Id;

        // Initialize metadata for all events
        events.ForEach(e => e.InitMetadata(actualStreamId, createdBy ?? "anonymous"));

        // Append to existing stream if streamId was provided
        if (streamId.HasValue)
        {
            session.Events.Append(actualStreamId, events);
        }

        return actualStreamId;
    }

    public static Guid StoreEvents<T>(this IDocumentSession session, Guid? streamId, DomainEvent @event, string? createdBy)
        where T : class, IAggregateRoot
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(@event);

        return session.StoreEvents<T>(streamId, [@event], createdBy);
    }
}
