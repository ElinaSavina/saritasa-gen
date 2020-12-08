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
        private const string NoneReturnType = "None";
        private const string BuiltInReturnType = "Built-in Type";
        private const string CustomDtoReturnType = "Custom DTO";

        private readonly IGenerationService generationService;
        private readonly IFormattingService formattingService;
        private readonly ISearchService searchService;
        private readonly IDialogService dialogService;

        private readonly Dictionary<string, List<string>> validationErrors =
            new Dictionary<string, List<string>>();

        private bool isBusy;
        private string baseClassFileName;
        private string dtoFileName;
        private bool useList;

        #region Auxiliary properties to format names

        private string CommandName => $"{FeatureName}Command";

        private string CommandHandlerName => $"{CommandName}Handler";

        private string QueryName => $"{FeatureName}Query";

        private string QueryHandlerName => $"{QueryName}Handler";

        private string DtoName => Path.GetFileNameWithoutExtension(DtoFileName);

        private string BaseClassName => Path.GetFileNameWithoutExtension(BaseClassFileName);

        #endregion Auxiliary properties to format names

        /// <summary>
        /// True to return list in command or query.
        /// </summary>
        public bool UseList
        {
            get => useList;
            set
            {
                useList = value;
                if (!useList)
                {
                    ReturnCollection = false;
                    OnPropertyChanged(nameof(ReturnCollection));

                    ReturnPagedList = false;
                    OnPropertyChanged(nameof(ReturnPagedList));
                }
            }
        }

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
        /// Return type. None, Simple type or Custom DTO.
        /// </summary>
        public string ReturnType { get; set; }

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
        /// Built-in type name.
        /// </summary>
        public ReturnType SelectedBuiltInType { get; set; }

        /// <summary>
        /// True to return paged list.
        /// </summary>
        public bool ReturnPagedList { get; set; }

        /// <summary>
        /// True to return simple collection.
        /// </summary>
        public bool ReturnCollection { get; set; }

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

        #region Collections

        /// <summary>
        /// Classes names in a project.
        /// </summary>
        public List<ClassModel> Classes { get; private set; }

        /// <summary>
        /// DTOs.
        /// </summary>
        public List<ClassModel> Dtos { get; private set; }

        /// <summary>
        /// List of possible return types.
        /// </summary>
        public List<string> ReturnTypes => new List<string>
        {
            NoneReturnType,
            BuiltInReturnType,
            CustomDtoReturnType,
        };

        /// <summary>
        /// Built-in types.
        /// </summary>
        public List<ReturnType> BuiltInTypes => new List<ReturnType>
        {
            new ReturnType("bool", typeof(bool).Namespace, true),
            new ReturnType("byte", typeof(byte).Namespace, true),
            new ReturnType("char", typeof(char).Namespace, true),
            new ReturnType("decimal", typeof(decimal).Namespace, true),
            new ReturnType("double", typeof(double).Namespace, true),
            new ReturnType("float", typeof(float).Namespace, true),
            new ReturnType("int", typeof(int).Namespace, true),
            new ReturnType("long", typeof(long).Namespace, true),
            new ReturnType("object", typeof(object).Namespace, true),
            new ReturnType("sbyte", typeof(sbyte).Namespace, true),
            new ReturnType("short", typeof(short).Namespace, true),
            new ReturnType("string", typeof(string).Namespace, true),
            new ReturnType("uint", typeof(uint).Namespace, true),
            new ReturnType("ulong", typeof(ulong).Namespace, true),
            new ReturnType("ushort", typeof(ushort).Namespace, true),
        };

        #endregion Collections

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

                var returnType = GetReturnType(dte, featureFolder);
                if (!string.IsNullOrEmpty(returnType?.Namespace))
                {
                    listOfUsingsForHandler.Add(returnType.Namespace);
                }

                var className = IsQueryCreated ? QueryName : CommandName;
                var handlerName = IsQueryCreated ? QueryHandlerName : CommandHandlerName;

                CreateCommandOrQuery(dte, featureFolder, className, returnType);

                CreateHandler(dte, selectedItem, featureFolder, listOfUsingsForHandler, handlerName, className, returnType);
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

        private ReturnType GetReturnType(DTE dte, ProjectItem featureFolder)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            switch (ReturnType)
            {
                case BuiltInReturnType:
                    return SelectedBuiltInType;

                case CustomDtoReturnType:
                    ReturnType returnType = null;
                    if (!string.IsNullOrEmpty(DtoFileName))
                    {
                        returnType.Namespace = CreateDtoIfNotExists(dte, featureFolder);
                        returnType.IsBuiltIn = false;
                        returnType.Name = DtoName;
                    }

                    return returnType;

                default: return null;
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

        private CodeClass GetOrCreateClass(DTE dte, SelectedItem selectedItem, string className, List<string> listOfUsings)
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
                listOfUsings.Add(existsClass.ClassNamespace);
                return searchService.FindClassInProject(existsClass.ContainingProject.ProjectItems, BaseClassName);
            }
        }

        private void CreateHandler(DTE dte, SelectedItem selectedFolder, ProjectItem featureFolder, List<string> listOfUsings,
            string handlerName, string className, ReturnType returnType)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            CodeClass baseClass = null;

            if (IsBaseClassUsed && !string.IsNullOrEmpty(BaseClassName))
            {
                baseClass = GetOrCreateClass(dte, selectedFolder, BaseClassName, listOfUsings);
            }

            var formattedReturnType = GetFormattedReturnType(returnType);

            AddPaginationNamespaceIfListReturned(listOfUsings);

            generationService.CreateHandlerWithMethod(dte, featureFolder.ProjectItems, new HandlerData
            {
                Name = handlerName,
                BaseClass = baseClass,
                CommandOrQueryName = className,
                ReturnType = formattedReturnType,
                IsQueryHandler = IsQueryCreated,
                ListOfUsings = listOfUsings
            });
        }

        private void CreateCommandOrQuery(DTE dte, ProjectItem selectedFolder, string className, ReturnType returnType)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var fileName = formattingService.FormatClassName(className);
            var projectItem = generationService.CreateClass(dte, fileName, selectedFolder.ProjectItems);

            var classNamespace = projectItem.FileCodeModel.CodeElements.OfType<CodeNamespace>().First();
            var createdClass = searchService.FindClassByName(classNamespace, className);
            createdClass.Access = vsCMAccess.vsCMAccessPublic;
            createdClass.DocComment = $"<doc><summary>\n{formattingService.GetPrettyName(className)}.\n</summary></doc>";
            createdClass.AddImplementedInterface(GetMediatRImplementedInterface(returnType));

            var listOfUsings = new List<string>
            {
                MediatrPackageName
            };

            if (!string.IsNullOrEmpty(returnType?.Namespace))
            {
                listOfUsings.Add(returnType.Namespace);
            }

            AddPaginationNamespaceIfListReturned(listOfUsings);

            generationService.AddUsingsToClass(projectItem.FileCodeModel.CodeElements, listOfUsings);
        }

        private string GetMediatRImplementedInterface(ReturnType returnType)
        {
            if (returnType == null)
            {
                return $"MediatR.IRequest";
            }

            return $"MediatR.IRequest<{GetFormattedReturnType(returnType)}>";
        }

        private string GetFormattedReturnType(ReturnType returnType)
        {
            if (ReturnPagedList)
            {
                return $"PagedList<{returnType.Name}>";
            }

            if (ReturnCollection)
            {
                return $"ICollection<{returnType.Name}>";
            }

            return returnType.Name;
        }

        private void AddPaginationNamespaceIfListReturned(List<string> listOfUsings)
        {
            if (ReturnPagedList)
            {
                listOfUsings.Add(SaritasaPaginationPackageName);
            }
            else if (ReturnCollection)
            {
                listOfUsings.Add(typeof(ICollection).Namespace);
            }
        }
    }
}
