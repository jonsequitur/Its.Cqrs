using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.Its.Domain.Api.Serialization;

namespace Microsoft.Its.Domain.Api.Documentation
{
    [ServesJsonByDefault]
    public class HelpController : ApiController
    {
        public HttpResponseMessage Get()
        {
            var content = Configuration.Services
                                       .GetApiExplorer()
                                       .ApiDescriptions
                                       .Select(d => new
                                       {
                                           d.HttpMethod,
                                           d.RelativePath,
                                           d.Route,
                                           ParameterDescriptions = d.ParameterDescriptions.Select(
                                               pd => new
                                               {
                                                   pd.Name,
                                                   pd.ParameterDescriptor.IsOptional
                                               }),
                                           d.Documentation
                                       });

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new JsonContent(content)
            };
        }
    }
}