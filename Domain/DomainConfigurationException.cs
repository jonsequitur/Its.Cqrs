// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Indicates a problem with the domain configuration.
    /// </summary>
    /// <seealso cref="System.Exception" />
    [Serializable]
    public class DomainConfigurationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DomainConfigurationException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DomainConfigurationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainConfigurationException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public DomainConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
