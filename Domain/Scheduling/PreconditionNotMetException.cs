// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    [Serializable]
    public class PreconditionNotMetException : InvalidOperationException
    {
        public PreconditionNotMetException(IPrecondition precondition)
            : base("Precondition was not met: " + precondition)
        {
        }

        public PreconditionNotMetException(string message) : base(message)
        {
        }
    }
}