namespace Sitecore.Support.Services.Infrastructure.Sitecore.DependencyInjection
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Reflection;
  using System.Web.Http;
  using System.Web.Http.ExceptionHandling;
  using System.Web.Routing;
  using global::Sitecore.Services.Core.ComponentModel.DataAnnotations;
  using global::Sitecore.Services.Core.Configuration;
  using global::Sitecore.Services.Core.Security;
  using global::Sitecore.Services.Infrastructure.Sitecore.DependencyInjection;
  using global::Sitecore.Services.Infrastructure;
  using global::Sitecore.Services.Infrastructure.Configuration;
  using global::Sitecore.Services.Infrastructure.Net.Http;
  using global::Sitecore.Services.Infrastructure.Reflection;
  using global::Sitecore.Services.Infrastructure.Sitecore;
  using global::Sitecore.Services.Infrastructure.Sitecore.Configuration;
  using global::Sitecore.Services.Infrastructure.Sitecore.Controllers;
  using global::Sitecore.Services.Infrastructure.Sitecore.Diagnostics;
  using global::Sitecore.Services.Infrastructure.Sitecore.Handlers;
  using global::Sitecore.Services.Infrastructure.Sitecore.Security;
  using global::Sitecore.Services.Infrastructure.Web.Http;
  using global::Sitecore.Services.Infrastructure.Web.Http.ExceptionHandling;
  using global::Sitecore.Services.Infrastructure.Web.Http.Filters;
  using SitecoreServicesInfrastructureSitecoreDependencyInjection = global::Sitecore.Services.Infrastructure.Sitecore.DependencyInjection;
  using SitecoreServicesCoreDiagnostics = global::Sitecore.Services.Core.Diagnostics;
  using Microsoft.Extensions.DependencyInjection;

  public class ComponentServicesConfigurator : SitecoreServicesInfrastructureSitecoreDependencyInjection.ComponentServicesConfigurator
  {
    public new void Configure(IServiceCollection serviceCollection)
    {
      serviceCollection.AddScoped<IUserService, UserService>().AddScoped<IExceptionLogger, SitecoreExceptionLogger>().AddScoped<SitecoreExceptionLogger, SitecoreExceptionLogger>().AddScoped<SitecoreServicesCoreDiagnostics.ILogger, SitecoreLogger>().AddScoped<IHandlerProvider, HandlerProvider>().AddScoped<IEntityValidator, EntityValidator>();
      serviceCollection.AddScoped<ExceptionLogger, SitecoreExceptionLogger>();
      ServiceCollectionServiceExtensions.AddScoped<IRuntimeSettings>(ServiceCollectionServiceExtensions.AddScoped<Assembly[]>(ServiceCollectionServiceExtensions.AddScoped<ConfigurationSettings>(ServiceCollectionServiceExtensions.AddScoped<RouteCollection>(ServiceCollectionServiceExtensions.AddScoped<HttpConfiguration>(ServiceCollectionServiceExtensions.AddScoped<BuilderMapping>(serviceCollection.AddScoped<AggregateDiscoveryController>(), (Func<IServiceProvider, BuilderMapping>)(provider => new BuilderMapping(CreateDefaultBuilderMapping()))).AddScoped<IConfigurationSectionProvider, ConfigurationSectionReader>().AddScoped<IRequestOrigin, HttpRequestOrigin>().AddScoped<ConfigurationReader>(), (Func<IServiceProvider, HttpConfiguration>)(provider => GlobalConfiguration.Configuration)), (Func<IServiceProvider, RouteCollection>)(provider => RouteTable.Routes)).AddScoped<IHttpConfiguration, ServicesConfigurator>(), (Func<IServiceProvider, ConfigurationSettings>)(provider => ServiceProviderServiceExtensions.GetService<ConfigurationReader>(provider).Load())).AddScoped<ConfigurationSettingsAdapter>(), (Func<IServiceProvider, Assembly[]>)(provider => ServiceProviderServiceExtensions.GetService<HttpConfiguration>(provider).Services.GetAssembliesResolver().GetAssemblies().ToArray<Assembly>())), (Func<IServiceProvider, IRuntimeSettings>)(provider => ServiceProviderServiceExtensions.GetService<ConfigurationSettingsAdapter>(provider).ToRuntimeSettings())).AddScoped<RestrictedControllerProvider>().AddScoped<ConfigurationSectionReader>();
   }
    internal static Dictionary<Type, Func<object>> CreateDefaultBuilderMapping() =>
      new Dictionary<Type, Func<object>> {
        {
          typeof(AnonymousUserFilter),
          new Func<object>(ComponentServicesConfigurator.BuildAnonymousUserFilter)
        },
        {
          typeof(SecurityPolicyAuthorisationFilter),
          new Func<object>(ComponentServicesConfigurator.BuildSecurityPolicyAuthorisationFilter)
        }
      };

    private static object BuildAnonymousUserFilter()
    {
      SitecoreServicesCoreDiagnostics.ILogger service = ServiceLocator.GetService<SitecoreServicesCoreDiagnostics.ILogger>();
      return new AnonymousUserFilter(ServiceLocator.GetService<IUserService>(), ServiceLocator.GetService<ConfigurationSettings>(), ServiceLocator.GetService<RestrictedControllerProvider>(), service, ServiceLocator.GetService<IRequestOrigin>(), new ConfiguredOrNullTokenProvider(ServiceLocator.GetService<ConfigurationSettings>(), service, new NullTokenProvider()));
    }

    private static object BuildSecurityPolicyAuthorisationFilter()
    {
      ConfigurationSettings service = ServiceLocator.GetService<ConfigurationSettings>();
      IList<string> allowedControllers = service.SitecoreServices.Security.AllowedControllers;
      SitecoreServicesCoreDiagnostics.ILogger logger = ServiceLocator.GetService<SitecoreServicesCoreDiagnostics.ILogger>();
      return new SecurityPolicyAuthorisationFilter(new ConfigurationSecurityPolicyFactory(service, logger), logger, ServiceLocator.GetService<IRequestOrigin>(), new LoggedTypeAccessor(allowedControllers, logger).Types);
    }
  }
}