namespace Microsoft.Its.Domain
{
    public static class Optional
    {
        public static Optional<T> Create<T>(T value)
        {
            return new Optional<T>(value);
        }
    }
}