using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SqlInliner
{
    public class Inliner
    {
        private Dictionary<string, string> _blockDefinitions { get; set; }
            = new Dictionary<string, string>();

        private readonly string _sqlProjectPath;

        private const string _blockDefinitionStartToken = @"\[SqlBlockDef_";

        private const string _blockDefinitionEndToken = @"_SqlBlockDef\]";

        private const string _staticBlockPlaceholderStartToken = @"'\[SqlBlock_";

        private const string _staticBlockPlaceholderEndToken = @"_SqlBlock\]'";

        private const string _dynamicBlockPlaceholderStartToken = @"''\[SqlBlock_";

        private const string _dynamicBlockPlaceholderEndToken = @"_SqlBlock\]''";

        private static Regex _blockDefinitionPattern
            = new Regex($@"{_blockDefinitionStartToken}([\w\W]+?){_blockDefinitionEndToken}");

        private static Regex _staticBlockPlaceholderPattern
            = new Regex($@"{_staticBlockPlaceholderStartToken}([\w\W]+){_staticBlockPlaceholderEndToken}");

        private static Regex _dynamicBlockPlaceholderPattern
            = new Regex($@"{_dynamicBlockPlaceholderStartToken}([\w\W]+){_dynamicBlockPlaceholderEndToken}");

        private static Regex _variablePlaceholderPattern
            = new Regex($@"{_variablePlaceholderToken}([\d]+)");

        private const char _nameBlockSeparator = '=';
        private const char _nameVariablesSeparator = '(';
        private const string _variableSeparator = ",,";
        private const string _variablePlaceholderToken = "@Var";

        public Inliner(string sqlProjectPath)
        {
            _sqlProjectPath = sqlProjectPath;
        }

        public List<string> GetAllBlockDefinitions()
        {
            List<string> errors = new List<string>();

            if (!Directory.Exists(_sqlProjectPath))
            {
                errors.Add(
                    $"Could not find the directory at {_sqlProjectPath}"
                    + " because it doesn't exist");

                return errors;
            }

            var sqlBlockFiles = Directory.EnumerateFiles(
                _sqlProjectPath,
                "*.sqlblock",
                SearchOption.AllDirectories);

            sqlBlockFiles =
                sqlBlockFiles.Where(x =>
                    (
                        !x.Contains(@"sqltemp\")
                        && !x.Contains(@"bin\")
                        && !x.Contains(@"obj\")
                    ));

            foreach (string file in sqlBlockFiles)
            {
                GetBlockDefinitionsFromFile(file, errors);
            }

            return errors;
        }

        public List<string> InlineAllBlocks()
        {
            List<string> errors = new List<string>();

            var sqlFiles = Directory.EnumerateFiles(
                _sqlProjectPath,
                "*.sql",
                SearchOption.AllDirectories);

            sqlFiles =
                sqlFiles.Where(x =>
                    (
                        !x.Contains("sqltemp")
                        && !x.Contains("bin")
                        && !x.Contains("obj")
                    ));

            foreach (string file in sqlFiles)
            {
                InlineBlocksInFile(file, errors);
            }

            return errors;
        }

        private void GetBlockDefinitionsFromFile(
            string filePath,
            List<string> errors)
        {
            string fileText = File.ReadAllText(filePath, Encoding.UTF8);

            var blockDefinitions =
                _blockDefinitionPattern.Matches(fileText);

            foreach (Match match in blockDefinitions)
            {
                string blockDefinition =
                    match.Groups[0].Value
                        .Trim()
                        .TrimStart(_blockDefinitionStartToken.ToCharArray())
                        .TrimEnd(_blockDefinitionEndToken.ToCharArray());

                int nameBlockSeparatorIndex =
                    blockDefinition
                    .IndexOf(_nameBlockSeparator);

                string blockName =
                    blockDefinition
                    .Substring(0, nameBlockSeparatorIndex)
                    .Trim();

                string block =
                    blockDefinition
                    .Substring(nameBlockSeparatorIndex + 1)
                    .Trim();

                if (!_blockDefinitions.TryAdd(blockName, block))
                {
                    errors.Add(
                    ErrorHandler.GenerateDefintionErrorMessage(
                        blockName,
                        filePath,
                        "The block has been defined multiple times."));
                }
            }
        }

        private void InlineBlocksInFile(
            string filePath,
            List<string> errors)
        {
            string fileText = File.ReadAllText(filePath, Encoding.UTF8);

            var dynamicBlockPlaceholders =
                _dynamicBlockPlaceholderPattern.Matches(fileText);

            foreach (Match match in dynamicBlockPlaceholders)
            {
                string parsedText =
                    ParseBlockDefinition(
                        match,
                        filePath,
                        errors,
                        true);

                if (parsedText != string.Empty)
                {
                    fileText =
                    fileText.Replace(
                        match.Value,
                        parsedText);
                }
            }

            var staticblockPlaceholders =
                _staticBlockPlaceholderPattern.Matches(fileText);

            foreach (Match match in staticblockPlaceholders)
            {
                string parsedText =
                    ParseBlockDefinition(
                        match,
                        filePath,
                        errors,
                        false);

                if (parsedText != string.Empty)
                {
                    fileText =
                    fileText.Replace(
                        match.Value,
                        parsedText);
                }
            }

            File.WriteAllText(filePath, fileText, Encoding.UTF8);
        }

        private string ParseBlockDefinition(
            Match match,
            string filePath,
            List<string> errors,
            bool bDynamic)
        {
            string startToken =
                bDynamic ?
                _dynamicBlockPlaceholderStartToken :
                _staticBlockPlaceholderStartToken;

            string endToken =
                bDynamic ?
                _dynamicBlockPlaceholderEndToken :
                _staticBlockPlaceholderEndToken;

            string blockPlaceholder =
                    match.Groups[0].Value
                        .Trim()
                        .TrimStart(startToken.ToCharArray())
                        .TrimEnd(endToken.ToCharArray());

            int nameVariablesSeparatorIndex =
                blockPlaceholder
                .IndexOf(_nameVariablesSeparator);

            string blockName =
                blockPlaceholder.Substring(
                    0,
                    nameVariablesSeparatorIndex)
                .Trim();

            string strVariables =
                blockPlaceholder
                .Substring(nameVariablesSeparatorIndex + 1)
                .Trim();

            var variables =
                strVariables
                .Remove(strVariables.Length - 1)
                .Split(_variableSeparator);

            int variableCount = variables.Count();

            if (variableCount == 1 && variables[0] == string.Empty)
                variableCount = 0;

            if (!_blockDefinitions.TryGetValue(blockName, out string block))
            {
                errors.Add(
                    ErrorHandler.GenerateInliningErrorMessage(
                        blockName,
                        filePath,
                        "Could not find block definition."));

                return string.Empty;
            }

            int variableDefinitionCount =
                _variablePlaceholderPattern
                .Matches(block)
                .Distinct()
                .Count();

            if (variableDefinitionCount != variableCount)
            {
                errors.Add(
                    ErrorHandler.GenerateInliningErrorMessage(
                        blockName,
                        filePath,
                        "The block placeholder contained a different number of variables to the block definition."));

                return string.Empty;
            }

            int variableCounter = 1;
            foreach (string variable in variables)
            {
                block =
                    block.Replace(
                        $"{_variablePlaceholderToken}{variableCounter}",
                        variable.Trim());

                ++variableCounter;
            }

            if (bDynamic)
            {
                return
                    $"' +\n--Start SqlBlock {blockName}--\n"
                    + $"' {block} '"
                    + $"\n--End SqlBlock {blockName}--\n+ '";
            }
            else
            {
                return
                    $"\n--Start SqlBlock {blockName}--\n"
                    + block
                    + $"\n--End SqlBlock {blockName}--\n";
            }
        }
    }
}