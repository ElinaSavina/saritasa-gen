using Autofac;
using SaritasaGen.FeatureGenerator.Services;
using SaritasaGen.FeatureGenerator.Views;
using SaritasaGen.Infrastructure.Abstractions;
using SaritasaGen.Infrastructure.Mvvm.ViewModels;
using SaritasaGen.Infrastructure.Services;

namespace SaritasaGen.FeatureGenerator.DependencyInjection
{
    /// <summary>
    /// Register services in DI.
    /// </summary>
    public class ServicesAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.RegisterType<FormattingService>().As<IFormattingService>().SingleInstance();
            builder.RegisterType<GenerationService>().As<IGenerationService>().SingleInstance();
            builder.RegisterType<SearchService>().As<ISearchService>().SingleInstance();
            builder.RegisterType<DialogService>().As<IDialogService>().SingleInstance();

            builder.RegisterType<FeatureViewModel>();
            builder.RegisterType<AddFeatureControl>();
        }
    }
}
