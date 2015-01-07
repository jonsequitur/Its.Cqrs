// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class SnapshotRepositoryExtensionsTests
    {
        private CompositeDisposable disposables;

        [SetUp]
        public void SetUp()
        {
            Command<CustomerAccount>.AuthorizeDefault = (order, command) => true;
            Command<Order>.AuthorizeDefault = (order, command) => true;
            disposables = new CompositeDisposable(ConfigurationContext.Establish(new Configuration()));
        }

        [TearDown]
        public void TearDown()
        {
            disposables.Dispose();
        }

        [Test]
        public void SaveSnapshot_throws_an_informative_exception_when_no_snapshot_creator_is_found()
        {
            var repository = new InMemorySnapshotRepository();

            Action save = () => repository.SaveSnapshot(new Order(new CreateOrder(Any.FullName()))).Wait();

            save.ShouldThrow<DomainConfigurationException>();
        }

        [Test]
        public async Task ETags_are_saved_in_the_snapshot()
        {
            var etag = Any.Guid().ToString();
            var account = new CustomerAccount().Apply(new RequestSpam
                                                      {
                                                          ETag = etag
                                                      });

            var repository = new InMemorySnapshotRepository();
            await repository.SaveSnapshot(account);

            var snapshot = await repository.Get(account.Id);

            snapshot.ETags
                    .Should()
                    .Contain(etag);
        }
    }
}
