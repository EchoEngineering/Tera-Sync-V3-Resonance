using Microsoft.Extensions.Logging;

namespace TeraSyncV2.Services.Mediator;

public abstract class MediatorSubscriberBase : IMediatorSubscriber
{
    protected MediatorSubscriberBase(ILogger logger, TeraMediator mediator)
    {
        Logger = logger;

        Logger.LogTrace("Creating {type} ({this})", GetType().Name, this);
        Mediator = mediator;
    }

    public TeraMediator Mediator { get; }
    protected ILogger Logger { get; }

    protected void UnsubscribeAll()
    {
        Logger.LogTrace("Unsubscribing from all for {type} ({this})", GetType().Name, this);
        Mediator.UnsubscribeAll(this);
    }
}