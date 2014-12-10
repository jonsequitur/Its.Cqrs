using System.Security.Principal;

namespace Microsoft.Its.Domain
{
    /// <summary>
    ///     A command that can be applied to an aggregate to trigger some action.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        ///     Gets the name of the command.
        /// </summary>
        string CommandName { get; }

        /// <summary>
        /// Gets the ETag for the command.
        /// </summary>
        string ETag { get; }

        /// <summary>
        ///     Gets or sets the principal on whose behalf the command will be authorized.
        /// </summary>
        IPrincipal Principal { get; }
    }
}