// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Microsoft.Its.Domain
{
    internal class BackgroundWorkQueue : ConcurrentQueue<BackgroundWork>
    {
    }
}