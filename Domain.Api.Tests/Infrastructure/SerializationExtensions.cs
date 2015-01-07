// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace Microsoft.Its.Domain.Api.Tests.Infrastructure
{
    public static class SerializationExtensions
    {
        public static dynamic FromJson(this HttpResponseMessage response)
        {
            var result = response.Content.ReadAsStringAsync().Result;
            
            try
            {
                return JToken.Parse(result);
            }
            catch (Exception)
            {
                Console.WriteLine(result);
                throw;
            }
        }
    }
}
