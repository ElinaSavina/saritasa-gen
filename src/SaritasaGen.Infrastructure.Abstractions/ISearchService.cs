using EnvDTE;
using EnvDTE80;
using SaritasaGen.Domain;
using System.Collections.Generic;

namespace SaritasaGen.Infrastructure.Abstractions
{
    /// <summary>
    /// Contains helper methods to search items.
    /// </summary>
    public interface ISearchService
    {
        /// <summary>
        /// Get string with usings which class doesn't contain.
        /// </summary>
        /// <param name="codeImports">Code imports.</param>
        /// <param name="listOfUsings">List of usings.</param>
        string GetUsings(List<CodeImport> codeImports, List<string> listOfUsings);

        /// <summary>
        /// Get string with usings.
        /// </summary>
        /// <param name="listOfUsings">List of usings.</param>
        string GetUsings(List<string> listOfUsings);

        /// <summary>
        /// Get project or parent folder items.
        /// </summary>
        /// <param name="dte">DTE.</param>
        /// <param name="selectedFolder">Selected folder.</param>
        ProjectItems GetProjectOrParentFolderItems(DTE dte, SelectedItem selectedFolder);

        /// <summary>
        /// Find item in project.
        /// </summary>
        /// <param name="projectItems">Project items.</param>
        /// <param name="name">Item name.</param>
        /// <param name="kind">Kind of item.</param>
        /// <param name="recursive">True to search recursive.</param>
        ProjectItem FindItemInProject(ProjectItems projectItems, string name, string kind, bool recursive);

        /// <summary>
        /// Find class in project.
        /// </summary>
        /// <param name="projectItems">Project items.</param>
        /// <param name="name">Class name.</param>
        CodeClass FindClassInProject(ProjectItems projectItems, string name);

        /// <summary>
        /// Find all classes.
        /// </summary>
        /// <param name="projects">Projects.</param>
        List<ClassModel> FindAllClasses(Projects projects);

        /// <summary>
        /// Find DTOs.
        /// </summary>
        /// <param name="projects">Projects.</param>
        List<ClassModel> FindDtos(Projects projects);

        /// <summary>
        /// Find class by name.
        /// </summary>
        /// <param name="codeNamespace">Namespace.</param>
        /// <param name="className">Class name.</param>
        CodeClass FindClassByName(CodeNamespace codeNamespace, string className);
    }
}
