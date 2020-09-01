using SaritasaGen.Infrastructure.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SaritasaGen.Infrastructure.Services
{
    /// <summary>
    /// Formatting service.
    /// </summary>
    public class FormattingService : IFormattingService
    {
        private Regex regex = new Regex(@"(?<!^)(?=[A-Z](?![A-Z]|$))", RegexOptions.Compiled);

        /// <inheritdoc />
        public string GetPrettyName(string name, bool capitalizeName = true)
        {
            var prettyName = string.Join(" ", regex.Split(name));
            prettyName = prettyName.ToLower();
            if (capitalizeName)
            {
                return FirstCharToUpper(prettyName);
            }
            return prettyName;
        }

        private string FirstCharToUpper(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentException();
            }

            return input.First().ToString().ToUpper() + input.Substring(1);
        }

        /// <inheritdoc />
        public string FormatClassName(string className)
        {
            if (string.IsNullOrEmpty(className) || !string.IsNullOrEmpty(Path.GetExtension(className)))
            {
                return className;
            }
            return $"{className}.cs";
        }

        /// <inheritdoc />
        public string GetConstructorComment(string className, List<string> parameters)
        {
            var comment = $"<doc><summary>\nInitializes a new instance of the <see cref=\"{className}\"/> class.\n</summary>";
            comment += AddParametersToComment(parameters);
            return comment + "</doc>";
        }

        /// <inheritdoc />
        public string GetFunctionComment(string methodName, List<string> parameters)
        {
            var comment = $"<doc><summary>\n{GetPrettyName(methodName)}\"/> class.\n</summary>";
            comment += AddParametersToComment(parameters);
            return comment + "</doc>";
        }

        private string AddParametersToComment(List<string> parameters)
        {
            var comment = string.Empty;
            foreach (var param in parameters)
            {
                comment += $"<param name=\"{param}\">{GetPrettyName(param)}.</param>";
            }
            return comment;
        }
    }
}
