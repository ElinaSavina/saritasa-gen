using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using NuGet.VisualStudio;
using SaritasaGen.Domain;
using SaritasaGen.Infrastructure.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace SaritasaGen.Infrastructure.Services
{
    /// <summary>
    /// Generation service.
    /// </summary>
    public class GenerationService : IGenerationService
    {
        private readonly ISearchService searchService;
        private readonly IFormattingService formattingService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerationService" /> class.
        /// </summary>
        /// <param name="searchService">Search service.</param>
        /// <param name="formattingService">Formatting service.</param>
        public GenerationService(ISearchService searchService, IFormattingService formattingService)
        {
            this.searchService = searchService;
            this.formattingService = formattingService;
        }

        /// <inheritdoc />
        public void AddUsingsToClass(CodeElements codeElements, List<string> listOfUsings)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var imports = codeElements.OfType<CodeImport>().ToList();

            if (imports.Any())
            {
                imports.Last().GetEndPoint().CreateEditPoint().Insert(searchService.GetUsings(imports, listOfUsings));
            }
            else
            {
                codeElements.Item(1).GetStartPoint().CreateEditPoint().Insert(searchService.GetUsings(listOfUsings));
            }
        }

        private void AddAsyncMethodToClass(CodeClass codeClass, string name, string parameterType, string parameterName, string returnType)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Add the method to the end of class.
            var handlerMethod = codeClass.AddFunction(name, vsCMFunction.vsCMFunctionFunction, returnType, -1);

            // Add parameters.
            handlerMethod.AddParameter("cancellationToken", "CancellationToken");
            handlerMethod.AddParameter(parameterName, parameterType);

            // Make the method async.
            handlerMethod.StartPoint.CreateEditPoint()
                .ReplaceText(0, "public async ", (int)vsEPReplaceTextOptions.vsEPReplaceTextAutoformat);

            // Fill the body.
            handlerMethod.GetStartPoint(vsCMPart.vsCMPartBody)
                .CreateEditPoint().ReplaceText(0, "throw new System.NotImplementedException();", (int)vsEPReplaceTextOptions.vsEPReplaceTextAutoformat);

            // Add the comment.
            handlerMethod.DocComment = $"<doc><inheritdoc /></doc>";
        }

        /// <inheritdoc />
        public ProjectItem CreateClass(DTE dte, string name, ProjectItems projectItems)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Solution2 solution = dte.Solution as Solution2;
            var classTemplate = solution.GetProjectItemTemplate("Class", "CSharp");
            return projectItems.AddFromTemplate(classTemplate, name);
        }

        /// <inheritdoc />
        public bool InstallNugetPackage(Project project, string package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var isPackageInstalled = true;
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            try
            {
                var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
                var installerServices = componentModel.GetService<IVsPackageInstallerServices>();
                if (!installerServices.IsPackageInstalled(project, package))
                {
                    dte.StatusBar.Text = $@"Installing {package} NuGet package...";
                    var installer = componentModel.GetService<IVsPackageInstaller>();
                    installer.InstallPackage(null, project, package, (System.Version)null, false);
                    dte.StatusBar.Text = $@"The {package} NuGet package has been installed.";
                }
            }
            catch
            {
                isPackageInstalled = false;
                dte.StatusBar.Text = $@"Unable to install the {package} NuGet package.";
            }

            return isPackageInstalled;
        }

        /// <inheritdoc />
        public void CreateHandlerWithMethod(DTE dte, ProjectItems projectItems, HandlerData handlerData)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var handlerItem = CreateClass(dte, formattingService.FormatClassName(handlerData.Name), projectItems);
            var handlerClass = searchService.FindClassByName(handlerItem.FileCodeModel.CodeElements.OfType<CodeNamespace>().First(), handlerData.Name);
            handlerClass.Access = vsCMAccess.vsCMAccessProject;
            handlerClass.DocComment = $"<doc><summary>\nHandle <see cref=\"{handlerData.CommandOrQueryName}\" /> {GetActionName(handlerData.IsQueryHandler)}.\n</summary></doc>";
            handlerClass.GetEndPoint(vsCMPart.vsCMPartNavigate).CreateEditPoint()
                .Insert($" : {GetMediatRImplementedInterface(handlerData.CommandOrQueryName, handlerData.ReturnType)}");

            if (handlerData.BaseClass != null)
            {
                handlerClass.AddBase(handlerData.BaseClass.Name);
                var baseConstructors = handlerData.BaseClass.Members.OfType<CodeFunction>().Where(f =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return f.FunctionKind == vsCMFunction.vsCMFunctionConstructor;
                });

                foreach (CodeFunction baseConstructor in baseConstructors)
                {
                    var constructor = handlerClass.AddFunction(handlerClass.Name, vsCMFunction.vsCMFunctionConstructor, vsCMTypeRef.vsCMTypeRefVoid, Access: vsCMAccess.vsCMAccessPublic);
                    var parameters = new List<string>();
                    TextPoint endPoint = constructor.GetEndPoint();

                    foreach (CodeParameter parameter in baseConstructor.Parameters)
                    {
                        handlerData.ListOfUsings.Add(parameter.Type.CodeType.Namespace.FullName);
                        endPoint = constructor.AddParameter(parameter.Name, parameter.Type.CodeType.Name, -1).GetEndPoint();
                        parameters.Add(parameter.Name);
                    }

                    endPoint.CreateEditPoint().Insert($") : base({string.Join(", ", parameters)}");

                    constructor.DocComment = formattingService.GetConstructorComment(handlerData.Name, parameters);
                }
            }

            var returnType = string.IsNullOrEmpty(handlerData.ReturnType) ? "Task<Unit>" : $"Task<{handlerData.ReturnType}>";
            AddAsyncMethodToClass(handlerClass, "Handle", handlerData.CommandOrQueryName, GetActionName(handlerData.IsQueryHandler), returnType);

            AddUsingsToClass(handlerItem.FileCodeModel.CodeElements, handlerData.ListOfUsings);
        }

        private string GetActionName(bool isQueryHandler)
        {
            return isQueryHandler ? "query" : "command";
        }

        private string GetMediatRImplementedInterface(string commandName, string returnType)
        {
            var types = commandName;
            if (!string.IsNullOrEmpty(returnType))
            {
                types += $", {returnType}";
            }

            return $"IRequestHandler<{types}>";
        }
    }
}
