using EnvDTE;
using System.Collections.Generic;

namespace SaritasaGen.Domain
{
    /// <summary>
    /// Handler data.
    /// </summary>
    public class HandlerData
    {
        /// <summary>
        /// Handler name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Command or query name.
        /// </summary>
        public string CommandOrQueryName { get; set; }

        /// <summary>
        /// Return type.
        /// </summary>
        public string ReturnType { get; set; }

        /// <summary>
        /// Base class.
        /// </summary>
        public CodeClass BaseClass { get; set; }

        /// <summary>
        /// True if it's query handler.
        /// </summary>
        public bool IsQueryHandler { get; set; }

        /// <summary>
        /// List of usings.
        /// </summary>
        public List<string> ListOfUsings { get; set; }
    }
}
