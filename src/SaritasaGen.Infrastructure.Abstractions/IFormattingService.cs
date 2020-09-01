using System.Collections.Generic;

namespace SaritasaGen.Infrastructure.Abstractions
{
    /// <summary>
    /// Formatting service.
    /// </summary>
    public interface IFormattingService
    {
        /// <summary>
        /// Get pretty name. Divide CamelCase string to separate words.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="capitalizeName">Capitalize name.</param>
        string GetPrettyName(string name, bool capitalizeName = true);

        /// <summary>
        /// Format class name.
        /// </summary>
        /// <param name="className">Class name.</param>
        string FormatClassName(string className);

        /// <summary>
        /// Get constructor comment.
        /// </summary>
        /// <param name="className">Class name.</param>
        /// <param name="parameters">Parameters.</param>
        string GetConstructorComment(string className, List<string> parameters);

        /// <summary>
        /// Get function comment.
        /// </summary>
        /// <param name="methodName">Method name.</param>
        /// <param name="parameters">Parameters.</param>
        string GetFunctionComment(string methodName, List<string> parameters);
    }
}
