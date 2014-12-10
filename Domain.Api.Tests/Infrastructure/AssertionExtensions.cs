using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Its.Log.Instrumentation;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Recipes;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain.Api.Tests.Infrastructure
{
    public static class AssertionExtensions
    {
        static AssertionExtensions()
        {
            Formatter<ObjectContent>.RegisterForAllMembers();
            Formatter<HttpError>.RegisterForAllMembers();
        }

        public static HttpResponseMessage ShouldSucceed(
            this HttpResponseMessage response,
            HttpStatusCode? expected = null)
        {
            try
            {
                response.EnsureSuccessStatusCode();

                var actualStatusCode = response.StatusCode;
                if (expected != null && actualStatusCode != expected.Value)
                {
                    throw new AssertionException(
                        string.Format("Status code was successful but not of the expected type: {0} was expected but {1} was returned.",
                                      expected,
                                      actualStatusCode));
                }
            }
            catch (Exception ex)
            {
                ThrowVerboseAssertion(response, ex);
            }
            return response;
        }

        public static HttpResponseMessage ShouldFailWith(this HttpResponseMessage response, HttpStatusCode code)
        {
            if (response.StatusCode != code)
            {
                ThrowVerboseAssertion(response, null);
            }

            return response;
        }

        private static void ThrowVerboseAssertion(HttpResponseMessage response, Exception ex)
        {
            var message = string.Format("{0}{1}{1}{2}",
                                        response,
                                        Environment.NewLine,
                                        response.Content.IfTypeIs<ObjectContent>()
                                                .Then(v => v.Value)
                                                .Else(() => response.Content).ToLogString());
            throw new AssertionException(message);
        }

        public static dynamic JsonContent(this HttpResponseMessage response)
        {
            return JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
        }
    }
}