namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Provides a way to differentiate event handlers having the same type but different implementations, e.g. anonymous and composite handlers.
    /// </summary>
    public interface INamedEventHandler 
    {
        string Name { get; }
    }
}
