using System;

namespace Microsoft.Its.Domain
{
    [Serializable]
    public class PreconditionNotMetException : InvalidOperationException
    {
    }
}