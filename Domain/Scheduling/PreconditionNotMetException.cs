// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Indicates that a command was applied without a precondition having been met.
    /// </summary>
    /// <seealso cref="System.InvalidOperationException" />
    [Serializable]
    public class PreconditionNotMetException : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PreconditionNotMetException"/> class.
        /// </summary>
        /// <param name="precondition">The precondition.</param>
        public PreconditionNotMetException(IPrecondition precondition)
            : base("Precondition was not met: " + precondition)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreconditionNotMetException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public PreconditionNotMetException(string message) : base(message)
        {
        }
    }
}