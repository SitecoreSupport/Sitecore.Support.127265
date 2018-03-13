namespace Sitecore.Support.Services.Infrastructure.Sitecore
{
  using Sitecore.DependencyInjection;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Net.Http;
  using System.Net.Http.Formatting;
  using System.Reflection;
  using System.Runtime.CompilerServices;
  using System.Web.Http;
  using System.Web.Http.Dependencies;
  using System.Web.Http.Dispatcher;
  using System.Web.Http.ExceptionHandling;
  using System.Web.Http.Filters;
  using global::Sitecore.Services.Core;
  using global::Sitecore.Services.Core.Diagnostics;
  using global::Sitecore.Services.Core.OData;
  using global::Sitecore.Services.Core.Reflection;
  using global::Sitecore.Services.Infrastructure;
  using global::Sitecore.Services.Infrastructure.Configuration;
  using global::Sitecore.Services.Infrastructure.Reflection;
  using global::Sitecore.Services.Infrastructure.Web.Http;
  using global::Sitecore.Services.Infrastructure.Web.Http.Dispatcher;
  using SitecoreServicesCoreConfiguration = global::Sitecore.Services.Core.Configuration;

  internal class ConfigurationSettingsAdapter : ConfigurationSettingsBase
  {
    private readonly BuilderMapping _builders;
    private readonly ILogger _logger;
    private readonly global::Sitecore.Services.Core.Configuration.ConfigurationSettings _settings;
    private readonly Assembly[] _siteAssemblies;
    internal static Func<IControllerNameGenerator> ControllerNameGenerator = () => DoControllerNameGenerator();

    public ConfigurationSettingsAdapter(ILogger logger, Assembly[] siteAssemblies, BuilderMapping builders, SitecoreServicesCoreConfiguration.ConfigurationSettings settings, HttpConfiguration httpConfiguration) : base(httpConfiguration)
    {
      if (logger == null)
      {
        throw new ArgumentNullException("logger");
      }
      if (siteAssemblies == null)
      {
        throw new ArgumentNullException("siteAssemblies");
      }
      if (builders == null)
      {
        throw new ArgumentNullException("builders");
      }
      if (settings == null)
      {
        throw new ArgumentNullException("settings");
      }
      this._logger = logger;
      this._siteAssemblies = siteAssemblies;
      this._builders = builders;
      this._settings = settings;
    }

    private IAssembliesResolver AssembliesResolverBuilder(ICollection<Assembly> defaultAssemblies, IEnumerable<string> excludedAssemblies) =>
        new LoggingAssembliesResolver(new FilteredAssembliesResolver(defaultAssemblies, new BlacklistAssemblyFilter(excludedAssemblies)), this._logger);

    private static IControllerNameGenerator DoControllerNameGenerator() =>
        new NamespaceQualifiedUniqueNameGenerator(DefaultHttpControllerSelector.ControllerSuffix);

    public override IRuntimeSettings ToRuntimeSettings()
    {
      IHttpControllerSelector instance = new ConfigurationHttpControllerSelectorFactory(this._settings, this._logger, base.HttpConfiguration).Instance;
      NamespaceHttpControllerSelector httpControllerSelector = new NamespaceHttpControllerSelector(base.HttpConfiguration, ControllerNameGenerator(), instance);
      IAssembliesResolver assemblyResolver = new AssembliesResolverFactory(this._settings, this._logger, () => this.AssembliesResolverBuilder(this._siteAssemblies, this._settings.WebApi.ExcludedAssemblies)).Instance;
      Assembly[] assemblies = assemblyResolver.GetAssemblies().ToArray<Assembly>();
      IMapRoutes routeMapper = new ConfigurationRouteConfigurationFactory(this._settings, this._logger).Instance;
      TypeLoader loader = new TypeLoader(this._logger, this._builders, global::Sitecore.DependencyInjection.ServiceLocator.ServiceProvider);
      IEnumerable<MediaTypeFormatter> formatters = loader.Load<MediaTypeFormatter>(this._settings.WebApi.Formatters);
      IEnumerable<IFilter> filters = loader.Load<IFilter>(new FilterProvider(assemblies, this._logger).Types, this._settings.SitecoreServices.Filters);
      IEnumerable<DelegatingHandler> delegatingHandlers = loader.Load<DelegatingHandler>(new DelegatingHandlerProvider(assemblies, this._logger).Types, this._settings.WebApi.DelegatingHandlers);
      IEnumerable<IExceptionLogger> exceptionLoggers = loader.Load<IExceptionLogger>(new ExceptionLoggerProvider(assemblies, this._logger).Types, this._settings.WebApi.ExceptionLoggers);
      IDependencyResolver dependencyResolver = loader.Load<IDependencyResolver>(new string[] { this._settings.WebApi.DependencyResolver }).First<IDependencyResolver>();
      return new RuntimeSettings(httpControllerSelector, routeMapper, filters, formatters, exceptionLoggers, assemblyResolver, dependencyResolver, loader.Load<IAggregateDescriptor>(new SitecoreServicesTypeProvider(assemblies, this._logger).Types), delegatingHandlers);
    }
  }
}
