using System;
using System.Diagnostics;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    [DebuggerStepThrough]
    internal class AnonymousClock : IClock
    {
        private readonly Func<DateTimeOffset> now;

        public AnonymousClock(Func<DateTimeOffset> now)
        {
            if (now == null)
            {
                throw new ArgumentNullException("now");
            }
            this.now = now;
        }

        public string Name { get; set; }

        public DateTimeOffset Now()
        {
            return now();
        }

        public override string ToString()
        {
            return string.Format("{0}: {2}{1}",
                                 GetType(),
                                 Name.IfNotNullOrEmptyOrWhitespace()
                                     .Then(n => string.Format(" ({0})", n))
                                     .ElseDefault(),
                                 Now().ToString("O"));
        }
    }
}