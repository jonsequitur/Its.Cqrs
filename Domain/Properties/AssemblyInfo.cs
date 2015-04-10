// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Microsoft.Its.Domain")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyCulture("")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("c492ccfe-7ae4-4bd8-b080-dddfec7faf75")]

[assembly: InternalsVisibleTo("Microsoft.Its.Domain.Api")]
[assembly: InternalsVisibleTo("Microsoft.Its.Domain.Sql")]
[assembly: InternalsVisibleTo("Microsoft.Its.Domain.Testing")]
[assembly: InternalsVisibleTo("Microsoft.Its.Domain.Sql.Tests")]
[assembly: InternalsVisibleTo("Microsoft.Its.Domain.Tests.Infrastructure")]

[assembly:ComVisible(false)]
