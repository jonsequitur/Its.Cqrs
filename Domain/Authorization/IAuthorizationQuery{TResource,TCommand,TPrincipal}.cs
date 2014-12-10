namespace Microsoft.Its.Domain.Authorization
{
    /// <summary>
    ///     Represents a check for whether a principal can issue a specific command pertaining to a specific resource.
    /// </summary>
    /// <typeparam name="TResource">The type of the resource.</typeparam>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <typeparam name="TPrincipal">The type of the principal.</typeparam>
    public interface IAuthorizationQuery<out TResource, out TCommand, out TPrincipal>
    {
        /// <summary>
        ///     Gets the command to be authorized.
        /// </summary>
        TCommand Command { get; }

        /// <summary>
        ///     Gets the resource to which the command would be applied.
        /// </summary>
        TResource Resource { get; }

        /// <summary>
        ///     Gets the principal that would apply the command.
        /// </summary>
        TPrincipal Principal { get; }
    }
}