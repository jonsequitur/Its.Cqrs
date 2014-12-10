using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Web.Http.Controllers;
using Microsoft.Its.Domain.Serialization;

namespace Microsoft.Its.Domain.Api.Serialization
{
    [DebuggerStepThrough]
    internal class ServesJsonByDefaultAttribute : Attribute, IControllerConfiguration
    {
        public void Initialize(HttpControllerSettings settings, HttpControllerDescriptor descriptor)
        {
            foreach (var formatter in settings.Formatters.OfType<JsonMediaTypeFormatter>())
            {
                formatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
                formatter.SerializerSettings = Serializer.Settings;
            }
        }
    }
}