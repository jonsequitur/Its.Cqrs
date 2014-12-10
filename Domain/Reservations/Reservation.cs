using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Provides methods for reserving unique values.
    /// </summary>
    public static class Reservation
    {
        /// <summary>
        /// Attempts to reserve the specified value.
        /// </summary>
        /// <typeparam name="TCommand">The type of the command.</typeparam>
        /// <param name="command">The command.</param>
        /// <param name="valueToReserve">A delegate returning the value to be reserved.</param>
        /// <param name="scope">The scope in which the reserved value is unique.</param>
        /// <param name="ownerToken"></param>
        /// <returns>A task whose result is true if the value has been successfully reserved; otherwise, false.</returns>
        /// <remarks>The method is repeatable, such that if the same principal sends multiple commands requiring the same reserved value, each will return true unless the reservation has expired. If a diferent principle sends a command attempting to reserve the same value, the result will be false.</remarks>
        public static Task<bool> RequiresReserved<TCommand>(
            this TCommand command,
            Func<TCommand, string> valueToReserve,
            string scope,
            string ownerToken,
            TimeSpan? lease = null)
            where TCommand : ICommand
        {
            if (scope == null)
            {
                throw new ArgumentNullException("scope");
            }

            return Configuration.Current
                                .ReservationService
                                .Reserve(valueToReserve(command),
                                         scope,
                                         ownerToken,
                                         lease);
        }
    }
}