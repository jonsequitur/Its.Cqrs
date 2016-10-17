// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using System.Linq;
using Moq;

#pragma warning disable CS0436 // Type conflicts with imported type

namespace Pocket
{
#if !RecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal static class PocketContainerAutoMockingStrategy
    {
        public static PocketContainer AutoMockInterfacesAndAbstractClasses(
            this PocketContainer container)
        {
            return container.AddStrategy(type =>
            {
                if (type.IsInterface || type.IsAbstract)
                {
                    var moqType = typeof (Mock<>).MakeGenericType(type);
                    return c =>
                    {
                        var mock = Activator.CreateInstance(moqType) as Mock;
                        mock.DefaultValue = DefaultValue.Mock;
                        return ((dynamic) mock).Object;
                    };
                }
                return null;
            });
        }
    }
}
