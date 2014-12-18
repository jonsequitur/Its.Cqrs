namespace Microsoft.Its.Domain
{
    public interface ICreateSnapshot<in TAggregate>
        where TAggregate : class, IEventSourced
    {
        ISnapshot CreateSnapshot(TAggregate aggregate);
    }
}