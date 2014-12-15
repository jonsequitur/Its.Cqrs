// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using System.Diagnostics;

namespace Its.Log.Lite
{
    /// <summary>
    /// Writes messages to trace output or to Its.Log via the Its.Log TraceListener, if it is in use. 
    /// </summary>
#if !RecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal static class Log
    {
        private static Lazy<bool> usingItsLog = LazyTraceListenerCheck();
      
        public static void ResetItsLogCheck()
        {
            usingItsLog = LazyTraceListenerCheck();
        }
        
        private static Lazy<bool> LazyTraceListenerCheck()
        {
            return new Lazy<bool>(() =>
            {
                foreach (var listener in Trace.Listeners)
                {
                    if (listener.GetType().FullName == "Its.Log.Instrumentation.TraceListener")
                    {
                        return true;
                    }
                }

                return false;
            });
        }

        /// <summary>
        ///     Writes the specified object to the log.
        /// </summary>
        /// <param name="getSubject">A function that returns the object to be written.</param>
        /// <param name="comment">A comment to provide context to the log entry.</param>
        public static void Write<T>(Func<T> getSubject, string comment = null) where T : class
        {
            Trace.WriteLine(getSubject.GetString(), comment);
        }

        /// <summary>
        ///     Indicates to the log that execution is entering a region.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Type" /> of the anonymous type of <paramref name="getSubject" /> used to enclose parameters to be logged at this boundary.
        /// </typeparam>
        /// <param name="getSubject">An anonymous type enclosing parameters to be logged.</param>
        /// <returns>
        ///     An <see cref="IDisposable" /> that, when disposed, writes out the closing log entry, including the updated state of the return value of <paramref name="getSubject" />.
        /// </returns>
        public static IDisposable Enter<T>(Func<T> getSubject) where T : class
        {
            Trace.WriteLine(getSubject.GetString(), "Start");

            return new TraceActivity(() => Trace.WriteLine(getSubject.GetString(), "Stop"));
        }

        private static object GetString<T>(this Func<T> subject)
        {
            if (usingItsLog.Value)
            {
                // the Its.Log trace listener will unpack the subject
                return subject;
            }

            return subject();
        }

        private class TraceActivity : IDisposable
        {
            private readonly Action stop;

            public TraceActivity(Action stop)
            {
                this.stop = stop;
            }

            public void Dispose()
            {
                stop();
            }
        }
    }
}