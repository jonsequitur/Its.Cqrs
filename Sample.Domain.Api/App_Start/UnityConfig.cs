// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain;
using Microsoft.Its.Domain.Sql;
using Microsoft.Practices.Unity;
using Sample.Domain.Ordering;

namespace Sample.Domain.Api
{
    public static class UnityConfig
    {
        public static void Register(UnityContainer container)
        {
            container.RegisterType<IEventSourcedRepository<Order>, SqlEventSourcedRepository<Order>>();
        }
    }
}