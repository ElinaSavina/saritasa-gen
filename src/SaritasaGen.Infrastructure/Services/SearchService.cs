using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using SaritasaGen.Domain;
using SaritasaGen.Infrastructure.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SaritasaGen.Infrastructure.Services
{
    /// <summary>
    /// Search service.
    /// </summary>
    public class SearchService : ISearchService
    {
        /// <inheritdoc />
        public string GetUsings(List<CodeImport> codeImports, List<string> listOfUsings)
        {
            var usings = new StringBuilder();

            foreach (var usingName in listOfUsings)
            {
                if (!codeImports.Any(i => i.Namespace == usingName))
                {
                    usings = usings.Append($"{Environment.NewLine}using {usingName};");
                }
            }

            return usings.ToString();
        }

        /// <inheritdoc />
        public string GetUsings(List<string> listOfUsings)
        {
            var usings = new StringBuilder();
            foreach (var usingNamespace in listOfUsings)
            {
                usings = usings.AppendLine($"using {usingNamespace};");
            }
            return usings.ToString();
        }

        /// <inheritdoc />
        public ProjectItems GetProjectOrParentFolderItems(DTE dte, SelectedItem selectedFolder)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // A project has been selectedd (not folder).
            if (selectedFolder.ProjectItem == null)
            {
                return selectedFolder.ProjectItem.ProjectItems;
            }

            var parentFolderPath = Path.GetDirectoryName(selectedFolder.ProjectItem.Properties.Item("FullPath").Value.ToString());
            var projectItem = dte.Solution.FindProjectItem(Path.GetDirectoryName(parentFolderPath));
            if (projectItem != null)
            {
                return projectItem.ProjectItems;
            }
            return selectedFolder.ProjectItem.ContainingProject.ProjectItems;
        }

        /// <inheritdoc />
        public ProjectItem FindItemInProject(ProjectItems projectItems, string name, string kind, bool recursive)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (ProjectItem item in projectItems)
            {
                if (recursive)
                {
                    var childItem = FindItemInProject(item.ProjectItems, name, kind, recursive);
                    if (childItem != null)
                    {
                        return childItem;
                    }
                }

                if (string.Equals(item.Kind, kind) && string.Equals(item.Name, name, StringComparison.CurrentCultureIgnoreCase))
                {
                    return item;
                }
            }
            return null;
        }

        /// <inheritdoc />
        public CodeClass FindClassInProject(ProjectItems projectItems, string name)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (ProjectItem item in projectItems)
            {
                var childItem = FindClassInProject(item.ProjectItems, name);
                if (childItem != null)
                {
                    return childItem;
                }

                if (item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFile && item.FileCodeModel?.CodeElements?.Count > 0)
                {
                    var codeClass = CheckCodeElements(item.FileCodeModel.CodeElements, name);
                    if (codeClass != null)
                    {
                        return codeClass;
                    }
                }
            }

            return null;
        }

        private CodeClass CheckCodeElements(CodeElements codeElements, string name)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (CodeElement codeElement in codeElements)
            {
                if (codeElement.Children.Count > 0)
                {
                    var item = CheckCodeElements(codeElement.Children, name);
                    if (item != null)
                    {
                        return item;
                    }
                }
                if (codeElement is CodeClass codeClass)
                {
                    if (string.Equals(codeClass.Name, name))
                    {
                        return codeClass;
                    }
                }
            }
            return null;
        }

        private void FindClassesInProject(ProjectItems projectItems, List<ClassModel> items)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (ProjectItem projectItem in projectItems)
            {
                if (string.Equals(projectItem.Kind, Constants.vsProjectItemKindSolutionItems))
                {
                    FindClassesInProject(projectItem.SubProject.ProjectItems, items);
                }
                else if (string.Equals(projectItem.Kind, Constants.vsProjectItemKindPhysicalFolder))
                {
                    FindClassesInProject(projectItem.ProjectItems, items);
                }
                else if (string.Equals(projectItem.Kind, Constants.vsProjectItemKindPhysicalFile) && projectItem.Name.EndsWith(".cs"))
                {
                    var classNamespace = projectItem.FileCodeModel.CodeElements.OfType<CodeNamespace>().First();
                    items.Add(new ClassModel(projectItem.Name, classNamespace.Name, projectItem.ContainingProject));
                }
            }
        }

        /// <inheritdoc />
        public List<ClassModel> FindAllClasses(Projects projects)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var items = new List<ClassModel>();
            foreach (Project project in projects)
            {
                FindClassesInProject(project.ProjectItems, items);
            }

            return items;
        }

        /// <inheritdoc />
        public CodeClass FindClassByName(CodeNamespace codeNamespace, string className)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return codeNamespace.Children.OfType<CodeClass>().FirstOrDefault(c =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return string.Equals(c.Name, className);
            });
        }

        /// <summary>
        ///  Search method recursive.
        /// </summary>
        /// <param name="codeElements">Code elements.</param>
        /// <param name="methodNames">Method names.</param>
        public bool SearchMethodRecursive(CodeElements codeElements, List<string> methodNames)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (CodeElement codeElement in codeElements)
            {
                if (CanContainHandler(codeElement) && SearchMethodRecursive(codeElement.Children, methodNames))
                {
                    return true;
                }

                if (codeElement is CodeFunction codeFuncion && methodNames.Contains(codeFuncion.Name))
                {
                    return true;
                }
            }
            return false;
        }

        private bool CanContainHandler(object codeElement)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return codeElement is CodeNamespace || codeElement is CodeClass;
        }
    }
}
