// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Its.Domain.Sql.Tests
{
    public class FlakyEventStream :
        IEnumerable<StorableEvent>
    {
        private readonly StorableEvent[] events;
        private readonly int startFlakingOnEnumeratorNumber;
        private int enumeratorCount = 0;
        private readonly Action<long> doSomethingFlaky;

        public FlakyEventStream(
            StorableEvent[] events,
            int startFlakingOnEnumeratorNumber,
            Action<long> doSomethingFlaky)
        {
            this.events = events;
            this.startFlakingOnEnumeratorNumber = startFlakingOnEnumeratorNumber;
            this.doSomethingFlaky = doSomethingFlaky;
        }

        public IEnumerator<StorableEvent> GetEnumerator()
        {
            enumeratorCount++;

            Console.WriteLine("FlakyEventStream.GetEnumerator() #" + enumeratorCount);

            if (enumeratorCount >= startFlakingOnEnumeratorNumber)
            {
                return new FlakyEnumerator(this);
            }

            return events.Cast<StorableEvent>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public class FlakyEnumerator : IEnumerator<StorableEvent>
        {
            private readonly FlakyEventStream eventStream;
            private long position = -1;

            public FlakyEnumerator(FlakyEventStream eventStream)
            {
                this.eventStream = eventStream;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                eventStream.doSomethingFlaky(position);

                if (position >= eventStream.events.Length - 1)
                {
                    return false;
                }

                position++;
                return true;
            }

            public void Reset()
            {
                position = 0;
            }

            public StorableEvent Current
            {
                get
                {
                    return eventStream.events[position];
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }
        }
    }
}
