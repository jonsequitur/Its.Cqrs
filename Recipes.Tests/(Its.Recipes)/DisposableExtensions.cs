// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;

namespace Microsoft.Its.Recipes
{
    /// <summary>
    /// Provides methods extending <see cref="IDisposable" />.
    /// </summary>
    internal static partial class DisposableExtensions
    {
        /// <summary>
        /// Disposes an object after retrieving a value from it.
        /// </summary>
        /// <typeparam name="TDisposable">The type of the disposable.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="disposable">The disposable.</param>
        /// <param name="getValue">A delegate to return a value from the disposable object before it is disposed.</param>
        /// <exception cref="System.ArgumentNullException">disposable</exception>
        public static TValue DisposeAfter<TDisposable, TValue>(
            this TDisposable disposable,
            Func<TDisposable, TValue> getValue)
            where TDisposable : IDisposable
        {
            if (disposable == null)
            {
                throw new ArgumentNullException("disposable");
            }
            using (disposable)
            {
                return getValue(disposable);
            }
        }

        /// <summary>
        /// Disposes an object after performing an action using it.
        /// </summary>
        /// <typeparam name="TDisposable">The type of the disposable.</typeparam>
        /// <param name="disposable">The disposable.</param>
        /// <param name="action">The action to be performed before the object is disposed.</param>
        /// <exception cref="System.ArgumentNullException">disposable</exception>
        public static void DisposeAfter<TDisposable>(
            this TDisposable disposable,
            Action<TDisposable> action)
            where TDisposable : IDisposable
        {
            if (disposable == null)
            {
                throw new ArgumentNullException("disposable");
            }
            using (disposable)
            {
                action(disposable);
            }
        }
    }
}