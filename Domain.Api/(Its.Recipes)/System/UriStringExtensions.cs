// THIS FILE IS NOT INTENDED TO BE EDITED UNLESS YOU ARE WORKING IN THE Recipes PROJECT. 
// 
// It has been imported using NuGet. 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;

namespace System
{
    /// <summary>
    ///     Extension methods for manipulating URIs as strings.
    /// </summary>
#if !RecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal static class UriStringExtensions
    {
        private static readonly char[] slash = new[] { '/' };

        /// <summary>
        ///     Appends a key/value pair to the query string of a given Uri string.
        /// </summary>
        /// <param name="baseUri">Raw Uri string value to append to</param>
        /// <param name="key">Key to append</param>
        /// <param name="value">Value to append</param>
        /// <param name="urlEncode">Value indicating whether or not key and value need to be encoded</param>
        /// <returns>New Raw Uri value</returns>
        public static string AppendQueryString(this string baseUri, string key, string value, bool urlEncode = true)
        {
            return baseUri.AppendQueryString(new NameValueCollection { { key, value } }, urlEncode);
        }

        /// <summary>
        ///     Appends a key/value pair to the query string of a given Uri string.
        /// </summary>
        /// <param name="baseUri">Raw Uri value to append to</param>
        /// <param name="parameters">Parameters to append</param>
        /// <param name="urlEncode">Value indicating whether or not key and value need to be encoded</param>
        /// <returns>New Raw Uri value</returns>
        public static string AppendQueryString(
            this string baseUri,
            NameValueCollection parameters,
            bool urlEncode = true)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }
            baseUri = baseUri ?? "";

            var builder = new StringBuilder(baseUri);
            if (!baseUri.Contains("?"))
            {
                builder.Append("?");
            }
            else
            {
                if (!baseUri.EndsWith("&", StringComparison.OrdinalIgnoreCase))
                {
                    builder.Append("&");
                }
            }

            for (var i = 0; i < parameters.AllKeys.Length; i++)
            {
                var key = parameters.AllKeys[i];
                var keyToAppend = urlEncode
                                      ? HttpUtility.UrlEncode(key)
                                      : key;
                var valueToAppend = urlEncode
                                        ? HttpUtility.UrlEncode(parameters[key] ?? "")
                                        : (parameters[key] ?? "");

                builder.AppendFormat(@"{0}={1}", keyToAppend, valueToAppend);

                if (i != parameters.AllKeys.Length - 1)
                {
                    builder.Append("&");
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Appends a segment to a URI string.
        /// </summary>
        /// <param name="baseUri">The base URI string.</param>
        /// <param name="segment">The segment to append.</param>
        /// <param name="uriFormat">The URI escape format.</param>
        /// <returns>A new string with the segment appended</returns>
        public static string AppendSegment(
            this string baseUri,
            string segment,
            UriFormat uriFormat = UriFormat.Unescaped)
        {
            var uri = new Uri(baseUri, UriKind.RelativeOrAbsolute);

            UriBuilder builder;
            UriComponents components;
            if (uri.IsAbsoluteUri)
            {
                components = UriComponents.AbsoluteUri & ~UriComponents.Port;
                builder = new UriBuilder(uri);
            }
            else
            {
                components = UriComponents.PathAndQuery;
                builder = new UriBuilder("http://p/" + baseUri.TrimStart(slash));
            }

            builder.Path = (builder.Path).TrimEnd(slash) + "/" + (segment ?? "").TrimStart(slash);
            var appended = builder.Uri.GetComponents(components, uriFormat);

            if (!uri.IsAbsoluteUri && !baseUri.StartsWith("/"))
            {
                // trim off the leading slash that UriBuilder added
                appended = appended.TrimStart(slash);
            }

            return appended;
        }
    }
}