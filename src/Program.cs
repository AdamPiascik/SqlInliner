using System.Collections.Generic;
using System.Linq;

namespace SqlInliner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
                ErrorHandler.HandleArgumentError();

            var inliner = new Inliner(args[0]);

            List<string> errors = new List<string>();

            errors.AddRange(
                inliner.GetAllBlockDefinitions());

            if (errors.Any())
                ErrorHandler.HandleErrors(errors);

            errors.AddRange(
                inliner.InlineAllBlocks());

            if (errors.Any())
                ErrorHandler.HandleErrors(errors);
        }
    }
}
