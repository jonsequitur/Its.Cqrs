// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain
{
    public static class Optional
    {
        public static Optional<T> Create<T>(T value) => new Optional<T>(value);
    }
}
