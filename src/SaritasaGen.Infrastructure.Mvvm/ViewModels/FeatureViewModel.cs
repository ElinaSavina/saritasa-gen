using EnvDTE;
using GalaSoft.MvvmLight.Command;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SaritasaGen.Domain;
using SaritasaGen.Infrastructure.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SaritasaGen.Infrastructure.Mvvm.ViewModels
{
    /// <summary>
    /// Feature view model.
    /// </summary>
    public class FeatureViewModel : INotifyDataErrorInfo, INotifyPropertyChanged
    {
        private const string MediatrPackageName = "MediatR";
        private const string SaritasaPaginationPackageName = "Saritasa.Tools.Common.Pagination";

        private readonly IGenerationService generationService;
        private readonly IFormattingService formattingService;
        private readonly ISearchService searchService;
        private readonly IDialogService dialogService;

        private readonly Dictionary<string, List<string>> validationErrors =
            new Dictionary<string, List<string>>();

        private bool isBusy;
        private string baseClassFileName;
        private string dtoFileName;

        #region Auxiliary properties to format names

        private string CommandName => $"{FeatureName}Command";

        private string CommandHandlerName => $"{CommandName}Handler";

        private string QueryName => $"{FeatureName}Query";

        private string QueryHandlerName => $"{QueryName}Handler";

        private string DtoName => Path.GetFileNameWithoutExtension(DtoFileName);

        private string BaseClassName => Path.GetFileNameWithoutExtension(BaseClassFileName);

        #endregion Auxiliary properties to format names

        /// <summary>
        /// True if command is creating.
        /// </summary>
        public bool IsBusy
        {
            get => isBusy;
            set
            {
                isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        /// <summary>
        /// Base class file name.
        /// </summary>
        public string BaseClassFileName
        {
            get => baseClassFileName;
            set
            {
                baseClassFileName = formattingService.FormatClassName(value);
            }
        }

        /// <summary>
        /// Feature name.
        /// </summary>
        public string FeatureName { get; set; }

        /// <summary>
        /// DTO file name.
        /// </summary>
        public string DtoFileName
        {
            get => dtoFileName;
            set
            {
                dtoFileName = formattingService.FormatClassName(value);
            }
        }

        /// <summary>
        /// True if DTO is used.
        /// </summary>
        public bool IsDtoUsed { get; set; }

        /// <summary>
        /// True to return list.
        /// </summary>
        public bool IsListReturned { get; set; }

        /// <summary>
        /// True if base class is used.
        /// </summary>
        public bool IsBaseClassUsed { get; set; }

        /// <summary>
        /// True if query is created.
        /// </summary>
        public bool IsQueryCreated { get; set; }

        /// <summary>
        /// Command to add the feature.
        /// </summary>
        public ICommand AddFeatureCommand { get; }

        /// <summary>
        /// Cancel command.
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Classes names in a project.
        /// </summary>
        public List<ClassModel> Classes { get; set; }

        /// <summary>
        /// DTOs.
        /// </summary>
        public List<ClassModel> Dtos { get; set; }

        #region INotifyPropertyChanged members

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// On property changed.
        /// </summary>
        /// <param name="property">Property.</param>
        public void OnPropertyChanged([CallerMemberName] string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        #endregion INotifyPropertyChanged members

        #region INotifyDataErrorInfo members

        /// <inheritdoc />
        public bool HasErrors => validationErrors.Any();

        /// <inheritdoc />
        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !validationErrors.ContainsKey(propertyName))
            {
                return null;
            }
            return validationErrors[propertyName];
        }

        /// <inheritdoc />
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        #endregion INotifyDataErrorInfo members

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureViewModel" /> class.
        /// </summary>
        /// <param name="generationService">Generation service.</param>
        /// <param name="formattingService">Formatting service.</param>
        /// <param name="searchService">Search service.</param>
        /// <param name="dialogService">Dialog service.</param>
        public FeatureViewModel(IGenerationService generationService,
                                        IFormattingService formattingService,
                                        ISearchService searchService,
                                        IDialogService dialogService)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.formattingService = formattingService;
            this.generationService = generationService;
            this.searchService = searchService;
            this.dialogService = dialogService;

            AddFeatureCommand = new RelayCommand<System.Windows.Window>(AddCommand_Execute, AddCommand_CanExecute);
            CancelCommand = new RelayCommand<System.Windows.Window>(CancelCommand_Execute);

            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;

            Classes = searchService.FindAllClasses(dte.Solution.Projects);

            Dtos = Classes.Where(i => i.ClassName.EndsWith("Dto.cs", StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private bool AddCommand_CanExecute(System.Windows.Window arg)
        {
            return !IsBusy;
        }

        private void CancelCommand_Execute(System.Windows.Window window)
        {
            window.Close();
        }

        private void AddCommand_Execute(System.Windows.Window window)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (IsBusy)
            {
                return;
            }

            IsBusy = true;

            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;

            // We get the first item becase it's not possible when selected items more than 1 and less than 0.
            var selectedItem = dte.SelectedItems.Item(1);

            var project = selectedItem.Project ?? selectedItem.ProjectItem.ContainingProject;

            // Make sure that file doesn't exist.
            if (searchService.FindItemInProject(project.ProjectItems, FeatureName, EnvDTE.Constants.vsProjectItemKindPhysicalFolder, true) != null)
            {
                dialogService.ShowError("The folder already exists.", "Error");
                IsBusy = false;
                return;
            }

            window.Close();

            var waitDialog = Package.GetGlobalService(typeof(SVsThreadedWaitDialog)) as IVsThreadedWaitDialog2;

            try
            {
                waitDialog.StartWaitDialog("Creating the feature...", "Please wait...", null, 0, "Feature creating...", 0, false, true);

                // Install MediatR.
                generationService.InstallNugetPackage(project, MediatrPackageName);

                var projectItems = selectedItem.Project?.ProjectItems ?? selectedItem.ProjectItem.ProjectItems;
                var featureFolder = CreateFeature(projectItems, FeatureName);

                var listOfUsingsForHandler = new List<string>
                {
                    "System.Threading.Tasks",
                    "System.Threading",
                     MediatrPackageName
                };

                var dtoNamespace = string.Empty;

                if (IsDtoUsed && !string.IsNullOrEmpty(DtoFileName))
                {
                    dtoNamespace = CreateDtoIfNotExists(dte, featureFolder);
                    if (!string.IsNullOrEmpty(dtoNamespace))
                    {
                        listOfUsingsForHandler.Add(dtoNamespace);
                    }
                }

                var className = IsQueryCreated ? QueryName : CommandName;
                var handlerName = IsQueryCreated ? QueryHandlerName : CommandHandlerName;

                CreateCommandOrQuery(dte, featureFolder, className, dtoNamespace);

                CreateHandler(dte, selectedItem, featureFolder, listOfUsingsForHandler, handlerName, className);
            }
            catch (Exception)
            {
                dialogService.ShowError("An error occured during feature creation.", "Error");
            }
            finally
            {
                IsBusy = false;
                waitDialog.EndWaitDialog();
            }
        }

        private ProjectItem CreateFeature(ProjectItems projectItems, string folderName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var featureFolder = projectItems.AddFolder(folderName, EnvDTE.Constants.vsProjectItemKindPhysicalFolder);
            return featureFolder;
        }

        private string CreateDtoIfNotExists(DTE dte, ProjectItem selectedFolder)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var existedDto = Dtos.FirstOrDefault(d => string.Equals(d.ClassName, DtoFileName, StringComparison.OrdinalIgnoreCase));
            if (existedDto != null)
            {
                return existedDto.ClassNamespace;
            }

            var dtoItem = generationService.CreateClass(dte, DtoFileName, selectedFolder.ProjectItems);
            var dtoClass = searchService.FindClassByName(dtoItem.FileCodeModel.CodeElements.OfType<CodeNamespace>().Single(), DtoName);
            dtoClass.DocComment = $"<doc><summary>\n{formattingService.GetPrettyName(DtoName)}.\n</summary></doc>";
            dtoClass.Access = vsCMAccess.vsCMAccessPublic;

            return string.Empty;
        }

        private CodeClass GetOrCreateClass(DTE dte, SelectedItem selectedItem, string className)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var existsClass = Classes.FirstOrDefault(c => string.Equals(c.ClassName, BaseClassFileName, StringComparison.OrdinalIgnoreCase));
            if (existsClass == null)
            {
                var projectItems = searchService.GetProjectOrParentFolderItems(dte, selectedItem);
                var baseItem = generationService.CreateClass(dte, formattingService.FormatClassName(className), projectItems);
                var codeClass = searchService.FindClassByName(baseItem.FileCodeModel.CodeElements.OfType<CodeNamespace>().Single(), className);
                codeClass.DocComment = $"<doc><summary>\n{formattingService.GetPrettyName(className)}.\n</summary></doc>";
                codeClass.Access = vsCMAccess.vsCMAccessPublic;
                return codeClass;
            }
            else
            {
                return searchService.FindClassInProject(existsClass.ContainingProject.ProjectItems, BaseClassName);
            }
        }

        private void CreateHandler(DTE dte, SelectedItem selectedFolder, ProjectItem featureFolder, List<string> listOfUsings, string handlerName, string className)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            CodeClass baseClass = null;

            if (IsBaseClassUsed && !string.IsNullOrEmpty(BaseClassName))
            {
                baseClass = GetOrCreateClass(dte, selectedFolder, BaseClassName);
            }

            var returnType = GetReturnType();

            AddPaginationNamespaceIfListReturned(listOfUsings);

            generationService.CreateHandlerWithMethod(dte, featureFolder.ProjectItems, new HandlerData
            {
                Name = handlerName,
                BaseClass = baseClass,
                CommandOrQueryName = className,
                ReturnType = returnType,
                IsQueryHandler = IsQueryCreated,
                ListOfUsings = listOfUsings
            });
        }

        private void CreateCommandOrQuery(DTE dte, ProjectItem selectedFolder, string className, string dtoNamespace)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var fileName = formattingService.FormatClassName(className);
            var projectItem = generationService.CreateClass(dte, fileName, selectedFolder.ProjectItems);

            var classNamespace = projectItem.FileCodeModel.CodeElements.OfType<CodeNamespace>().First();
            var createdClass = searchService.FindClassByName(classNamespace, className);
            createdClass.Access = vsCMAccess.vsCMAccessPublic;
            createdClass.DocComment = $"<doc><summary>\n{formattingService.GetPrettyName(className)}.\n</summary></doc>";
            createdClass.AddImplementedInterface(GetMediatRImplementedInterface());

            var listOfUsings = new List<string>
            {
                MediatrPackageName
            };

            if (!string.IsNullOrEmpty(dtoNamespace))
            {
                listOfUsings.Add(dtoNamespace);
            }

            AddPaginationNamespaceIfListReturned(listOfUsings);

            generationService.AddUsingsToClass(projectItem.FileCodeModel.CodeElements, listOfUsings);
        }

        private string GetMediatRImplementedInterface()
        {
            var returnType = GetReturnType();
            if (!string.IsNullOrEmpty(returnType))
            {
                return $"MediatR.IRequest<{returnType}>";
            }

            return $"MediatR.IRequest";
        }

        private string GetReturnType()
        {
            if (IsDtoUsed && !string.IsNullOrEmpty(DtoName))
            {
                return IsListReturned ? $"PagedList<{DtoName}>" : DtoName;
            }
            return string.Empty;
        }

        private void AddPaginationNamespaceIfListReturned(List<string> listOfUsings)
        {
            if (IsDtoUsed && !string.IsNullOrEmpty(DtoName) && IsListReturned)
            {
                listOfUsings.Add(SaritasaPaginationPackageName);
            }
        }
    }
}
