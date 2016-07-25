// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    [DisableCommandAuthorization]
    [UseInMemoryEventStore]
    public class SnapshotRepositoryExtensionsTests
    {
        [Test]
        public async Task ETags_are_saved_in_the_snapshot()
        {
            var etag = Any.Guid().ToString();

            var account = new CustomerAccount().Apply(new RequestSpam
            {
                ETag = etag
            });
            await Configuration.Current.Repository<CustomerAccount>().Save(account);

            var repository = Configuration.Current.SnapshotRepository();
            await repository.SaveSnapshot(account);

            var snapshot = await repository.GetSnapshot(account.Id);

            snapshot.ETags.MayContain(etag).Should().BeTrue();
        }

        [Test]
        public async Task When_an_aggregate_has_pending_events_then_creating_a_snapshot_throws()
        {
            var account = new CustomerAccount().Apply(new RequestSpam());
            await Configuration.Current.Repository<CustomerAccount>().Save(account);
            await account.ApplyAsync(new NotifyOrderCanceled());

            var repository = Configuration.Current.SnapshotRepository();
            Action save = () => repository.SaveSnapshot(account).Wait();

            save.ShouldThrow<InvalidOperationException>()
                .WithMessage("A snapshot can only be created from an aggregate having no pending events. Save the aggregate before creating a snapshot.");
        }
    }
}