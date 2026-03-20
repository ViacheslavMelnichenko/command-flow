namespace CommandFlow;

/// <summary>
/// Combines <see cref="ISender"/> and <see cref="IPublisher"/> into a single interface.
/// This is the primary entry point for dispatching requests and notifications.
/// </summary>
public interface IMediator : ISender, IPublisher;

