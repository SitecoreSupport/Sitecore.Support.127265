namespace Sitecore.Support.Services.Infrastructure.Sitecore
{
  using System;
  using System.Web.Http;
  using global::Sitecore.Services.Infrastructure;

  internal abstract class ConfigurationSettingsBase
  {
    protected readonly System.Web.Http.HttpConfiguration HttpConfiguration;

    protected ConfigurationSettingsBase(System.Web.Http.HttpConfiguration httpConfiguration)
    {
      if (httpConfiguration == null)
      {
        throw new ArgumentNullException("httpConfiguration");
      }
      this.HttpConfiguration = httpConfiguration;
    }

    public abstract IRuntimeSettings ToRuntimeSettings();
  }
}
