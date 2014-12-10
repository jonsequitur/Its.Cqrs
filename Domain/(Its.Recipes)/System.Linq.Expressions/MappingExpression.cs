// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using System.Linq;
using System.Reflection;

namespace System.Linq.Expressions
{
#if !RecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal static partial class MappingExpression
    {
        /// <summary>
        ///     Builds expressions for mapping property and field values between objects.
        /// </summary>
        /// <typeparam name="TFrom">The type of the object from which member values will be taken.</typeparam>
        public class From<TFrom>
        {
            /// <summary>
            ///     Creates an expression that will instantiate a new instance of TTo and populate its members based on a source instance of TFrom.
            /// </summary>
            /// <typeparam name="TTo">The type of the object to be instantiated.</typeparam>
            public static Expression<Func<TFrom, TTo>> ToNew<TTo>()
            {
                var fromMembers = typeof (TFrom).GetProperties().Cast<MemberInfo>()
                                                .Concat(typeof (TFrom).GetFields());
                var toMembers = typeof (TTo).GetProperties().Cast<MemberInfo>()
                                            .Concat(typeof (TTo).GetFields());

                var paramExpr = Expression.Parameter(typeof (TFrom), "from");

                MemberBinding[] memberAssignments = toMembers
                    .Where(m =>
                           fromMembers.Any(pi => pi.Name.Equals(m.Name, StringComparison.OrdinalIgnoreCase)))
                    .Select(to =>
                    {
                        Type returnType = to.MemberType ==
                                          MemberTypes.Property
                                              ? ((PropertyInfo) to).PropertyType
                                              : ((FieldInfo) to).FieldType;
                        return Expression.Bind(to,
                                               Expression.Convert(
                                                   Expression.PropertyOrField(paramExpr,
                                                                              fromMembers.First(pi => pi.Name == to.Name).Name),
                                                   returnType));
                    })
                    .ToArray();

                var memberInitExpression = Expression.MemberInit(
                    Expression.New(typeof (TTo)),
                    memberAssignments);

                return Expression.Lambda<Func<TFrom, TTo>>(memberInitExpression, paramExpr);
            }

            /// <summary>
            ///     Creates an expression that will populate the members of an instance of TTo from a source instance of TFrom.
            /// </summary>
            /// <typeparam name="TTo">The type of the object whose members will be set.</typeparam>
            public static Expression<Action<TFrom, TTo>> ToExisting<TTo>()
            {
                var fromMembers = typeof (TFrom).GetProperties().Cast<MemberInfo>()
                                                .Concat(typeof (TFrom).GetFields());
                var toMembers = typeof (TTo).GetProperties().Cast<MemberInfo>()
                                            .Concat(typeof (TTo).GetFields());

                var toExpr = Expression.Parameter(typeof (TTo), "to");
                var fromExpr = Expression.Parameter(typeof (TFrom), "from");

                var memberAssignments = toMembers
                    .Where(m =>
                           fromMembers.Any(pi => pi.Name.Equals(m.Name, StringComparison.OrdinalIgnoreCase)))
                    .Select(to => Expression.Assign(
                        Expression.PropertyOrField(toExpr, to.Name),
                        Expression.PropertyOrField(fromExpr, fromMembers.First(pi => pi.Name == to.Name).Name)));

                return Expression.Lambda<Action<TFrom, TTo>>(
                    Expression.Block(memberAssignments),
                    fromExpr,
                    toExpr);
            }
        }
    }
}