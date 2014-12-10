using System;

namespace Sample.Domain
{
    public static class Inventory
    {
        public static Func<string, bool> IsAvailable = sku => true;
    }
}