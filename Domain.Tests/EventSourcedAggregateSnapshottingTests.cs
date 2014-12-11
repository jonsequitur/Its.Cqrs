using System.Reactive.Disposables;
using Microsoft.Its.Domain.Testing;
using NUnit.Framework;
using Sample.Domain;
using Sample.Domain.Ordering;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class EventSourcedAggregateSnapshottingTests
    {
        private CompositeDisposable disposables;

        [SetUp]
        public void SetUp()
        {
            // disable authorization
            Command<Order>.AuthorizeDefault = (o, c) => true;
            Command<CustomerAccount>.AuthorizeDefault = (o, c) => true;

            disposables = new CompositeDisposable();

            var configurationContext = ConfigurationContext
                .Establish(
                    new Configuration()
                        .UseInMemoryEventStore()
                        .IgnoreScheduledCommands());
            disposables.Add(configurationContext);
        }

        [TearDown]
        public void TearDown()
        {
            disposables.Dispose();
        }

    
    }
}