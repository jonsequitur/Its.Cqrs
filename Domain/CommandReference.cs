using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    public class CommandReference
    {
        public string CommandName { get; set; }

        public string CommandField { get; set; }

        public override string ToString()
        {
            return CommandName + CommandField.IfNotNull()
                                             .Then(field => "." + field)
                                             .Else(() => "");
        }
    }
}