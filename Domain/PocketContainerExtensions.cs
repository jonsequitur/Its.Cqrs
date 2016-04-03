// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Pocket;

namespace Microsoft.Its.Domain
{
    internal static class PocketContainerExtensions
    {
        public static PocketContainer UseImmediateCommandScheduling(this PocketContainer container)
        {
            return container.AddStrategy(type =>
            {
                if (type.IsInterface &&
                    type.IsGenericType &&
                    type.GetGenericTypeDefinition() == typeof (ICommandScheduler<>))
                {
                    var targetType = type.GetGenericArguments().First();
                    var schedulerType = typeof (CommandScheduler<>).MakeGenericType(targetType);

                    return c => c.Resolve(schedulerType);
                }

                return null;
            });
        }

        public static PocketContainer AddStoreStrategy(this PocketContainer container)
        {
            return container.AddStrategy(type =>
            {
                if (type.IsInterface &&
                    type.IsGenericType &&
                    type.GetGenericTypeDefinition() == typeof (IStore<>))
                {
                    var targetType = type.GetGenericArguments().First();

                    Type applierType;
                    if (typeof (IEventSourced).IsAssignableFrom(targetType))
                    {
                        applierType = typeof (IEventSourcedRepository<>).MakeGenericType(targetType);

                        return c => c.Resolve(applierType);
                    }
                }

                return null;
            });
        }

        public static PocketContainer DefaultToJsonSnapshots(this PocketContainer container)
        {
            return container.AddStrategy(type =>
            {
                if (type.IsInterface &&
                    type.IsGenericType)
                {
                    if (type.GetGenericTypeDefinition() == typeof (ICreateSnapshot<>))
                    {
                        var snapshotCreatorType = typeof (JsonSnapshotter<>)
                            .MakeGenericType(type.GetGenericArguments().Single());

                        return c => c.Resolve(snapshotCreatorType);
                    }

                    if (type.GetGenericTypeDefinition() == typeof (IApplySnapshot<>))
                    {
                        var snapshotCreatorType = typeof (JsonSnapshotter<>)
                            .MakeGenericType(type.GetGenericArguments().Single());

                        return c => c.Resolve(snapshotCreatorType);
                    }
                }

                return null;
            });
        }
    }
}