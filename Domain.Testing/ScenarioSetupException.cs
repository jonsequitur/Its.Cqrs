// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    /// Thrown when a scenario setup fails.
    /// </summary>
    public class ScenarioSetupException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Exception"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error. </param>
        public ScenarioSetupException(string message) : base(message)
        {
        }
    }
}