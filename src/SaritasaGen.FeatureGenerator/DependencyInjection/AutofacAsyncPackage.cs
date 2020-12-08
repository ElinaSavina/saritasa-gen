using Autofac;
using Microsoft.VisualStudio.Shell;
using System;
using System.Threading;

namespace SaritasaGen.FeatureGenerator.DependencyInjection
{
    /// <summary>
    /// Autofac async package.
    /// </summary>
    public class AutofacAsyncPackage : AsyncPackage
    {
        private IContainer container;
        private readonly ContainerBuilder containerBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutofacEnabledAsyncPackage" /> class.
        /// </summary>
        public AutofacAsyncPackage()
        {
            containerBuilder = new ContainerBuilder();
        }

        /// <summary>
        /// Register module.
        /// </summary>
        public void RegisterModule<TModule>() where TModule : Autofac.Core.IModule, new()
        {
            containerBuilder.RegisterModule<TModule>();
        }

        /// <summary>
        /// Get custom service.
        /// </summary>
        /// <param name="serviceType">Service type.</param>
        public object GetCustomService(Type serviceType)
        {
            return GetService(serviceType);
        }

        protected override System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            container = containerBuilder.Build();
            return base.InitializeAsync(cancellationToken, progress);
        }

        protected override object GetService(Type serviceType)
        {
            if (container?.IsRegistered(serviceType) ?? false)
            {
                return container.Resolve(serviceType);
            }
            return base.GetService(serviceType);
        }

        protected override WindowPane InstantiateToolWindow(Type toolWindowType) => (WindowPane)GetService(toolWindowType);

        protected override void Dispose(bool disposing)
        {
            try
            {
                container?.Dispose();
            }
            catch { }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
