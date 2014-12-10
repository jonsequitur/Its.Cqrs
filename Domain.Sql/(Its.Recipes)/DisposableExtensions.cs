// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;

namespace Microsoft.Its.Recipes
{
    internal static partial class DisposableExtensions
    {
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