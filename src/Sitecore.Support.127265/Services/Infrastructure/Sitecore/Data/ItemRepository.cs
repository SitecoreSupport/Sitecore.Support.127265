using Sitecore.Services.Infrastructure.Sitecore.Security;
using Sitecore.StringExtensions;

namespace Sitecore.Support.Services.Infrastructure.Sitecore.Data
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using Configuration;
  using Exceptions;
  using SitecoreData = global::Sitecore.Data;
  using global::Sitecore.Data.Items;
  using global::Sitecore.Services.Core.Diagnostics;
  using global::Sitecore.Services.Core.Model;
  using global::Sitecore.Services.Infrastructure.Sitecore.Data;
  using Globalization;

  public class ItemRepository : ItemDataBase, IItemRepository
  {
    private readonly ILogger _logger;

    public ItemRepository(ILogger logger)
    {
      DoAuthorization();
      this._logger = logger;
    }

    public Guid Add(ItemCreateRequest request, string databaseName, string language)
    {
      DoAuthorization();

      SitecoreData.Database database = GetDatabase(databaseName);
      Language language2 = ItemDataBase.GetLanguage(language);
      Item item = database.GetItem(request.ParentPath, language2);
      if (item == null)
      {
        throw new ArgumentException(ItemDataBase.InvalidParameterMessage("Path", request.ParentPath));
      }
      TemplateItem template = database.GetTemplate(new SitecoreData.ID(request.Template));
      if (template == null)
      {
        throw new ArgumentException(ItemDataBase.InvalidParameterMessage("Template", request.Template));
      }
      Item item3 = item.Add(request.Name, template);
      using (new EditContext(item3))
      {
        this.UpdateFields(request.Fields, item3);
      }
      return item3.ID.Guid;
    }

    public void Delete(Guid id, string databaseName, string language)
    {
      DoAuthorization();

      SitecoreData.Database database = GetDatabase(databaseName);

      Item item;

      if (language.IsNullOrEmpty())
      {
        item = database.GetItem(new SitecoreData.ID(id));
      }
      else
      {
        Language language2 = ItemDataBase.GetLanguage(language);
        item = database.GetItem(new SitecoreData.ID(id), language2);
      }

      if (item == null)
      {
        throw new ItemNotFoundException(id.ToString());
      }
      #region Bug 127265
      if (Settings.RecycleBinActive)
      {
        if (language.IsNullOrEmpty())
        {
          item.Recycle();
        }
        else
        {
          var languageVersions = item.Versions;

          foreach (var languageVersion in languageVersions.GetVersions(false))
          {
            languageVersion.RecycleVersion();
          }
        }
      }
      else
      {
        if (language.IsNullOrEmpty())
        {
          item.Delete();
        }
        else
        {
          var languageVersions = item.Versions;

          languageVersions.RemoveAll(false);
        }
      }
      #endregion
    }

    private static Item FilterItemByVersion(Item item, SitecoreData.Version itemVersion)
    {
      if (item == null)
      {
        return null;
      }
      if (!(itemVersion == SitecoreData.Version.Latest) && !item.Versions.GetVersionNumbers().Contains<SitecoreData.Version>(itemVersion))
      {
        return null;
      }
      return item;
    }

    public Item FindById(Guid id, string databaseName, string language, string version)
    {
      DoAuthorization();

      SitecoreData.Database database = GetDatabase(databaseName);
      Language language2 = ItemDataBase.GetLanguage(language);
      SitecoreData.Version itemVersion = this.GetItemVersion(version);
      return FilterItemByVersion(database.GetItem(new SitecoreData.ID(id), language2, itemVersion), itemVersion);
    }

    public Item FindByPath(string path, string databaseName, string language, string version)
    {
      DoAuthorization();

      SitecoreData.Database database = GetDatabase(databaseName);
      Language language2 = ItemDataBase.GetLanguage(language);
      SitecoreData.Version itemVersion = this.GetItemVersion(version);
      return FilterItemByVersion(database.GetItem(path, language2, itemVersion), itemVersion);
    }

    private static SitecoreData.Database GetDatabase(string databaseName)
    {
      SitecoreData.Database database2;
      try
      {
        SitecoreData.Database database = Factory.GetDatabase(ItemDataBase.GetDatabaseName(databaseName));
        if (database == null)
        {
          throw new ArgumentException(ItemDataBase.InvalidParameterMessage("Database", databaseName));
        }
        database2 = database;
      }
      catch (InvalidOperationException)
      {
        throw new ArgumentException(ItemDataBase.InvalidParameterMessage("Database", databaseName));
      }
      return database2;
    }

    private SitecoreData.Version GetItemVersion(string versionNumber)
    {
      DoAuthorization();

      SitecoreData.Version version;
      if (!string.IsNullOrEmpty(versionNumber) && (SitecoreData.Version.TryParse(versionNumber, out version) && (version.Number > 0)))
      {
        return version;
      }
      return SitecoreData.Version.Latest;
    }

    private static void MoveItemTo(Guid destinationParentID, Item item, SitecoreData.Database database)
    {
      Item destination = database.GetItem(new SitecoreData.ID(destinationParentID));
      if (destination != null)
      {
        item.MoveTo(destination);
      }
    }

    public Item[] RunQuery(string term, string databaseName) =>
        GetDatabase(databaseName).SelectItems(term);

    public void Update(ItemUpdateRequest request, string databaseName, string language, string version)
    {
      DoAuthorization();

      SitecoreData.Database database = GetDatabase(databaseName);
      Language language2 = ItemDataBase.GetLanguage(language);
      SitecoreData.Version itemVersion = this.GetItemVersion(version);
      Item item = database.GetItem(new SitecoreData.ID(request.ItemId), language2, itemVersion);
      if (item == null)
      {
        throw new ItemNotFoundException(request.ItemId.ToString());
      }
      using (new EditContext(item))
      {
        this.UpdateFields(request.Fields, item);
        if (request.Fields.ContainsKey("ItemName") && (item.Name != ((string)request.Fields["ItemName"])))
        {
          item.Name = (string)request.Fields["ItemName"];
        }
      }
      if (request.Fields.ContainsKey("ParentID") && (item.ParentID.Guid != new Guid((string)request.Fields["ParentID"])))
      {
        MoveItemTo(new Guid((string)request.Fields["ParentID"]), item, database);
      }
    }

    private void UpdateFields(FieldDictionary fields, Item itemToUpdate)
    {
      DoAuthorization();

      foreach (KeyValuePair<string, object> pair in fields)
      {
        if (itemToUpdate.Fields[pair.Key] == null)
        {
          this._logger.Info($"Ignoring update of {pair.Key} on Item {itemToUpdate.ID}");
        }
        else
        {
          itemToUpdate.Fields[pair.Key].Value = (string)pair.Value;
        }
      }
    }

    private void DoAuthorization()
    {
      bool allowAnonymousUser =
        System.Convert.ToBoolean(Settings.GetSetting("Sitecore.Services.AllowAnonymousUser", "false"));

      string anonymousUser = Settings.GetSetting("Sitecore.Services.AnonymousUser", string.Empty);

      UserService userService = new UserService();

      if (userService.IsAnonymousUser)
      {
        if (allowAnonymousUser && userService.UserExists(anonymousUser))
        {
          userService.SwitchToUser(anonymousUser);
        }
      }
    }
  }
}
