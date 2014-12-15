// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace System.Dynamic
{
    /// <summary>
    ///     Provides dynamic methods to assist in reflection-based tasks.
    /// </summary>
#if !RecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal static partial class TypeExtensions
    {
        /// <summary>
        ///     Provides dynamic access to a static member on the specified type.
        /// </summary>
        /// <param name="type">The type on which to access the member.</param>
        /// <returns>The value, if any, of the member.</returns>
        public static dynamic Member(this Type type)
        {
            return new StaticMemberInvoker(type);
        }

        /// <summary>
        ///     Forwards dynamc calls to static members on a target type.
        /// </summary>
        private class StaticMemberInvoker : DynamicObject
        {
            private readonly Type targetType;

            /// <summary>
            ///     Initializes a new instance of the <see cref="StaticMemberInvoker" /> class.
            /// </summary>
            /// <param name="targetType">The type.</param>
            /// <exception cref="System.ArgumentNullException">type</exception>
            public StaticMemberInvoker(Type targetType)
            {
                if (targetType == null)
                {
                    throw new ArgumentNullException("targetType");
                }
                this.targetType = targetType;
            }

            /// <summary>
            ///     Provides the implementation for operations that get member values. Classes derived from the
            ///     <see
            ///         cref="T:System.Dynamic.DynamicObject" />
            ///     class can override this method to specify dynamic behavior for operations such as getting a value for a property.
            /// </summary>
            /// <returns>
            ///     true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a run-time exception is thrown.)
            /// </returns>
            /// <param name="binder">
            ///     Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member on which the dynamic operation is performed. For example, for the Console.WriteLine(sampleObject.SampleProperty) statement, where sampleObject is an instance of the class derived from the
            ///     <see
            ///         cref="T:System.Dynamic.DynamicObject" />
            ///     class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.
            /// </param>
            /// <param name="result">
            ///     The result of the get operation. For example, if the method is called for a property, you can assign the property value to
            ///     <paramref
            ///         name="result" />
            ///     .
            /// </param>
            public override bool TryGetMember(
                GetMemberBinder binder,
                out object result)
            {
                var property = targetType.GetProperty(binder.Name,
                                                      BindingFlags.FlattenHierarchy |
                                                      BindingFlags.Public |
                                                      BindingFlags.Static);
                if (property == null)
                {
                    result = null;
                    return false;
                }

                result = property.GetValue(null, null);
                return true;
            }

            /// <summary>
            ///     Provides the implementation for operations that invoke a member. Classes derived from the
            ///     <see
            ///         cref="T:System.Dynamic.DynamicObject" />
            ///     class can override this method to specify dynamic behavior for operations such as calling a method.
            /// </summary>
            /// <returns>
            ///     true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
            /// </returns>
            /// <param name="binder">
            ///     Provides information about the dynamic operation. The binder.Name property provides the name of the member on which the dynamic operation is performed. For example, for the statement sampleObject.SampleMethod(100), where sampleObject is an instance of the class derived from the
            ///     <see
            ///         cref="T:System.Dynamic.DynamicObject" />
            ///     class, binder.Name returns "SampleMethod". The binder.IgnoreCase property specifies whether the member name is case-sensitive.
            /// </param>
            /// <param name="args">
            ///     The arguments that are passed to the object member during the invoke operation. For example, for the statement sampleObject.SampleMethod(100), where sampleObject is derived from the <see cref="T:System.Dynamic.DynamicObject" /> class, <paramref name="args" /> is equal to 100.
            /// </param>
            /// <param name="result">The result of the member invocation.</param>
            public override bool TryInvokeMember(
                InvokeMemberBinder binder,
                object[] args,
                out object result)
            {
                var method = targetType.GetMethod(binder.Name,
                                                  BindingFlags.FlattenHierarchy |
                                                  BindingFlags.Public |
                                                  BindingFlags.Static);
                if (method == null)
                {
                    result = null;
                    return false;
                }

                if (binder.CallInfo.ArgumentNames.Any() &&
                    binder.CallInfo.ArgumentNames.First() == "of")
                {
                    var type = args.First() as Type;
                    if (type != null)
                    {
                        method = method.MakeGenericMethod(new[] { type });
                        args = args.Skip(1).ToArray();
                    }
                }
                else
                {
                    var csharpBinder = binder.GetType().GetInterface("Microsoft.CSharp.RuntimeBinder.ICSharpInvokeOrInvokeMemberBinder");
                    var genericArguments = (csharpBinder.GetProperty("TypeArguments").GetValue(binder, null) as IEnumerable<Type>);

                    if (genericArguments != null && genericArguments.Any())
                    {
                        method = method.MakeGenericMethod(genericArguments.ToArray());
                    }
                }

                result = method.Invoke(null, args);
                return true;
            }
        }
    }
}