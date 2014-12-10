using System;
using System.Linq;
using Microsoft.Its.Domain;
using Microsoft.Its.Domain.Api;
using Sample.Domain.Ordering;

namespace Sample.Domain.Api.Controllers
{
    public class OrderApiController : DomainApiController<Order>
    {
        public OrderApiController(IEventSourcedRepository<Order> repository) : base(repository)
        {
        }
    }
}