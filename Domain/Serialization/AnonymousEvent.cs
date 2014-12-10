namespace Microsoft.Its.Domain.Serialization
{
    internal class AnonymousEvent<TAggregate> : Event<TAggregate> where TAggregate : IEventSourced
    {
        public string Body { get; set; }

        public override void Update(TAggregate aggregate)
        {
        }
    }
}