using EnvDTE;
using SaritasaGen.Domain;
using System.Collections.Generic;

namespace SaritasaGen.Infrastructure.Abstractions
{
    /// <summary>
    /// Contains helper methods to create and update elements (classes, methods, etc).
    /// </summary>
    public interface IGenerationService
    {
        /// <summary>
        /// Add usings to class.
        /// </summary>
        /// <param name="codeElements">Code elements.</param>
        /// <param name="listOfUsings">List of usings.</param>
        void AddUsingsToClass(CodeElements codeElements, List<string> listOfUsings);

        /// <summary>
        /// Create class.
        /// </summary>
        /// <param name="dte">DTE.</param>
        /// <param name="name">Name.</param>
        /// <param name="projectItems">Project items.</param>
        ProjectItem CreateClass(DTE dte, string name, ProjectItems projectItems);

        /// <summary>
        /// Install nuget package.
        /// </summary>
        /// <param name="project">Project.</param>
        /// <param name="package">Package name.</param>
        bool InstallNugetPackage(Project project, string package);

        /// <summary>
        /// Create handler with method.
        /// </summary>
        /// <param name="dte">DTE.</param>
        /// <param name="projectItems">Project items.</param>
        /// <param name="handlerData">Handler data.</param>
        void CreateHandlerWithMethod(DTE dte, ProjectItems projectItems, HandlerData handlerData);
    }
}
