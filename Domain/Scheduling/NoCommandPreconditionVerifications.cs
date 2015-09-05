// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    internal class NoCommandPreconditionVerifications : ICommandPreconditionVerifier
    {
        public async Task<bool> IsPreconditionSatisfied(IScheduledCommand scheduledCommand)
        {
            if (scheduledCommand.DeliveryPrecondition == null)
            {
                return true;
            }

            throw new InvalidOperationException("No ICommandPreconditionVerifier has been configured.");
        }
    }
}