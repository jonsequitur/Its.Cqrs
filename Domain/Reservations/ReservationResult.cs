// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain
{
    public class ReservationResult
    {
        public bool IsSuccessful { get; private set; }

        public string Message { get; private set; }

        public ReservationResult ReservationIsSuccessful(string response)
        {
            IsSuccessful = true;
            Message = response;
            return this;
        }

        public ReservationResult ReservationFailed(string response)
        {
            IsSuccessful = false;
            Message = response;
            return this;
        }
    }
}