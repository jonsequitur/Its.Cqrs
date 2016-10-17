// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Test.Domain.Ordering;
#pragma warning disable 618

namespace Microsoft.Its.Domain.Tests
{
    public class TestConsequenter :
        IHaveConsequencesWhen<Order.Cancelled>,
        IHaveConsequencesWhen<Order.Created>,
        IHaveConsequencesWhen<Order.Delivered>
    {
        private readonly Action<Order.Cancelled> onCancelled;
        private readonly Action<Order.Created> onCreated;
        private readonly Action<Order.Delivered> onDelivered;

        public TestConsequenter(
            Action<Order.Cancelled> onCancelled = null,
            Action<Order.Created> onCreated = null,
            Action<Order.Delivered> onDelivered = null)
        {
            this.onCancelled = onCancelled ?? (e => { });
            this.onCreated = onCreated ?? (e => { });
            this.onDelivered = onDelivered ?? (e => { });
        }

        public void HaveConsequences(Order.Cancelled @event)
        {
            onCancelled(@event);
        }

        public void HaveConsequences(Order.Created @event)
        {
            onCreated(@event);
        }

        public void HaveConsequences(Order.Delivered @event)
        {
            onDelivered(@event);
        }
    }
}
