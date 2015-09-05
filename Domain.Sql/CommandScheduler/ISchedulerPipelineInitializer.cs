// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    internal interface ISchedulerPipelineInitializer
    {
        void Initialize(Configuration configuration);
    }
}