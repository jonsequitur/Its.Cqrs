// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    [Serializable]
    public class DomainConfigurationException : Exception
    {
        public DomainConfigurationException(string message) : base(message)
        {
        }
    }
}