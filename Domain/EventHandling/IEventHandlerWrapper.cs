namespace Microsoft.Its.Domain
{
    internal interface IEventHandlerWrapper
    {
        object InnerHandler { get; }
    }
}