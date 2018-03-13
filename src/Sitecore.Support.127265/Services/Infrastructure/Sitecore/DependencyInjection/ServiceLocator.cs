namespace Sitecore.Support.Services.Infrastructure.Sitecore.DependencyInjection
{
  using Sitecore.DependencyInjection;
  using System;
  using System.Runtime.CompilerServices;

  internal static class ServiceLocator
  {
    // ReSharper disable once MemberCanBePrivate.Global
    public static Func<IServiceProvider> ServiceProvider
      = () =>
        global::Sitecore.DependencyInjection
          .ServiceLocator
          .ServiceProvider;

    public static T GetService<T>() where T : class
    {
      if (ServiceProvider == null)
      {
        throw new ObjectDisposedException(typeof(Func<IServiceProvider>).Name);
      }
      T service = ServiceProvider().GetService(typeof(T)) as T;
      if (service == null)
      {
        throw new ObjectDisposedException($"Failed to get dependency {typeof(T).FullName}");
      }
      return service;
    }
  }
}