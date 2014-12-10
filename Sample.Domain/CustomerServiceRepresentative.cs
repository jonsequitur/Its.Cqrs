using System;
using System.Security.Principal;

namespace Sample.Domain
{
    public class CustomerServiceRepresentative : IPrincipal, IIdentity
    {
        public bool IsInRole(string role)
        {
            return string.Equals(role, "customer-service", StringComparison.OrdinalIgnoreCase);
        }

        public IIdentity Identity
        {
            get
            {
                return this;
            }
        }

        public string Name { get; set; }
        public string AuthenticationType { get; set; }
        public bool IsAuthenticated { get; set; }
    }
}