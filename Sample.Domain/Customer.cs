using System;
using System.Security.Principal;

namespace Sample.Domain
{
    public class Customer : IPrincipal, IIdentity
    {
        public Customer(string name = null)
        {
            Name = name;
        }

        public bool IsInRole(string role)
        {
            return string.Equals(role, "customer", StringComparison.OrdinalIgnoreCase);
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

        public Guid Id { get; set; }
    }
}