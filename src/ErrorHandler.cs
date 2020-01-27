using System;
using System.Collections.Generic;

namespace SqlInliner
{
    public static class ErrorHandler
    {
        public static void HandleArgumentError()
        {
            Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine(
                "SqlInliner takes exactly one argument, which is "
                + "the path to the directory containing your .sqlproj file.");

            Console.ResetColor();

            Environment.Exit(1);
        }

        public static void HandleErrors(List<string> errors)
        {
            Console.ForegroundColor = ConsoleColor.Red;

            foreach (var error in errors)
            {
                Console.WriteLine(error);
            }

            Console.ResetColor();

            Environment.Exit(1);
        }

        public static string GenerateInliningErrorMessage(
            string blockName,
            string fileName,
            string errorMessage
        )
        {
            return
                $"There was an error inlining the block {blockName}"
                + $" in {fileName}. Error message: {errorMessage}\n";
        }

        public static string GenerateDefintionErrorMessage(
            string blockName,
            string fileName,
            string errorMessage
        )
        {
            return
                $"There was an error parsing the definition of {blockName}"
                + $" in {fileName}. Error message: {errorMessage}\n";
        }
    }
}
