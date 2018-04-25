using System.Web.Mvc;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Globalization;
using Sitecore.Services.Infrastructure.Sitecore.Data;
using Sitecore.Services.Infrastructure.Sitecore.Security;
using Sitecore.Shell.Framework;

namespace Sitecore.Support.Services.Infrastructure.Sitecore.Controllers
{
  using System;
  using System.Diagnostics.CodeAnalysis;
  using System.Net;
  using System.Net.Http;
  using System.Runtime.InteropServices;
  using System.Web.Http;
  using System.Web.Mvc;
  using Configuration;
  using DependencyInjection;
  using Exceptions;
  using global::Sitecore.Data;
  using global::Sitecore.Data.Items;
  using global::Sitecore.Services.Core.Diagnostics;
  using global::Sitecore.Services.Core.Extensions;
  using global::Sitecore.Services.Core.Model;
  using global::Sitecore.Services.Infrastructure.Model;
  using global::Sitecore.Services.Infrastructure.Net.Http;
  using global::Sitecore.Services.Infrastructure.Sitecore;
  using global::Sitecore.Services.Infrastructure.Sitecore.Handlers;
  using global::Sitecore.Services.Infrastructure.Sitecore.Handlers.Query;
  using global::Sitecore.Services.Infrastructure.Web.Http;
  using global::Sitecore.Services.Infrastructure.Web.Http.ModelBinding;
  using Globalization;
  using UrlHelper = System.Web.Http.Routing.UrlHelper;

  public sealed class SupportItemServiceController : ServicesApiController
  {
    private readonly IHandlerProvider _handlerProvider;
    private readonly ILogger _logger;
    private const int PageSize = 10;

    [Obsolete("Use constructor ItemServiceController(IHandlerProvider, ILogger)"), ExcludeFromCodeCoverage]
    public SupportItemServiceController() : this(ServiceLocator.GetService<IHandlerProvider>(), ServiceLocator.GetService<ILogger>())
    {
    }

    public SupportItemServiceController(IHandlerProvider handlerProvider, ILogger logger)
    {
      if (handlerProvider == null)
      {
        throw new ArgumentNullException("handlerProvider");
      }
      if (logger == null)
      {
        throw new ArgumentNullException("logger");
      }
      this._handlerProvider = handlerProvider;
      this._logger = logger;
    }

    [System.Web.Http.ActionName("DefaultAction")]
    public HttpResponseMessage Delete(Guid id, string database = "", string language = "")
    {
      DeleteItemCommand query = new DeleteItemCommand
      {
        Id = id,
        Database = database,
        Language = language
      };

      Handlers.HandlerProvider customHandlerProvider = new global::Sitecore.Support.Services.Infrastructure.Sitecore.Handlers.HandlerProvider();

      IItemRequestHandler handler = customHandlerProvider.GetHandler<global::Sitecore.Support.Services.Infrastructure.Sitecore.Handlers.DeleteItemHandler>();
      this.ProcessRequest<object>(new Func<HandlerRequest, object>(handler.Handle), query);
      return new HttpResponseMessage(HttpStatusCode.NoContent);
    }

    [System.Web.Http.ActionName("GetItemByContentPath")]
    public ItemModel Get([System.Web.Http.ModelBinding.ModelBinder(typeof(GetItemByContentPathQueryModelBinder))] GetItemByContentPathQuery query)
    {
      IItemRequestHandler handler = this._handlerProvider.GetHandler<GetItemByContentPathHandler>();
      return (ItemModel)this.ProcessRequest<object>(new Func<HandlerRequest, object>(handler.Handle), query);
    }

    [System.Web.Http.ActionName("DefaultAction")]
    public ItemModel Get([System.Web.Http.ModelBinding.ModelBinder(typeof(GetItemByIdQueryModelBinder))] GetItemByIdQuery query)
    {
      IItemRequestHandler handler = this._handlerProvider.GetHandler<GetItemByIdHandler>();
      return (ItemModel)this.ProcessRequest<object>(new Func<HandlerRequest, object>(handler.Handle), query);
    }

    public ItemModel[] GetChildren([System.Web.Http.ModelBinding.ModelBinder(typeof(GetItemChildrenQueryModelBinder))] GetItemChildrenQuery query)
    {
      IItemRequestHandler handler = this._handlerProvider.GetHandler<GetItemChildrenHandler>();
      return (ItemModel[])this.ProcessRequest<object>(new Func<HandlerRequest, object>(handler.Handle), query);
    }

    [System.Web.Http.ActionName("DefaultAction")]
    public HttpResponseMessage Post(string path, [FromBody] ItemModel itemModel, string database = "", string language = "")
    {
      CreateItemCommand query = new CreateItemCommand
      {
        Path = path,
        ItemModel = itemModel,
        Database = database,
        Language = language
      };
      IItemRequestHandler handler = this._handlerProvider.GetHandler<CreateItemHandler>();
      CreateItemResponse response = (CreateItemResponse)this.ProcessRequest<object>(new Func<HandlerRequest, object>(handler.Handle), query);
      HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.Created);
      string str = new UrlHelper(base.Request).Link("ItemService", new
      {
        id = response.ItemId,
        Database = response.Database,
        Language = response.Language
      });
      if (!string.IsNullOrEmpty(str))
      {
        message.Headers.Add("Location", str);
      }
      return message;
    }

    private T ProcessRequest<T>(Func<HandlerRequest, T> handler, HandlerRequest query)
    {
      DoAuthorization();

      T local;
      try
      {
        local = handler(query);
      }
      catch (ItemNotFoundException exception)
      {
        throw new ApiControllerException(HttpStatusCode.NotFound, "Item Not Found", exception.Message);
      }
      catch (ArgumentException exception2)
      {
        throw new ApiControllerException(HttpStatusCode.BadRequest, exception2.Message, "");
      }
      catch (ApplicationException exception3)
      {
        throw new ApiControllerException(HttpStatusCode.ServiceUnavailable, exception3.Message, "");
      }
      catch (Exception exception4)
      {
        if (exception4.IsAccessViolation())
        {
          this._logger.Warn($"Access Denied: {exception4.Message} Request from { base.Request.GetClientIpAddress()}");
          throw new ApiControllerException(HttpStatusCode.Forbidden);
        }
        this._logger.Error(exception4.ToString());
        throw new ApiControllerException(HttpStatusCode.InternalServerError);
      }
      return local;
    }

    [System.Web.Http.HttpGet]
    public HttpResponseMessage QueryViaItem(Guid id, bool includeStandardTemplateFields = false, string fields = "", int page = 0, int pageSize = 10, string database = "", string language = "", string version = "")
    {
      if (pageSize < 1)
      {
        pageSize = 10;
      }
      SitecoreQueryViaItemQuery query = new SitecoreQueryViaItemQuery
      {
        Id = id,
        Database = database,
        Language = language,
        Version = version
      };
      IItemRequestHandler handler = this._handlerProvider.GetHandler<SitecoreQueryViaItemHandler>();
      Item[] itemArray = (Item[])this.ProcessRequest<object>(new Func<HandlerRequest, object>(handler.Handle), query);
      FormatItemsQuery query2 = new FormatItemsQuery
      {
        Items = itemArray,
        Fields = fields,
        IncludeStandardTemplateFields = includeStandardTemplateFields,
        Page = page,
        PageSize = pageSize,
        RequestMessage = base.Request,
        Controller = "ItemService-QueryViaItem",
        RouteValues = new
        {
          includeStandardTemplateFields = includeStandardTemplateFields,
          fields = fields,
          database = database,
          id = id
        }
      };
      IItemRequestHandler handler2 = this._handlerProvider.GetHandler<FormatItemsHandler>();
      object obj2 = this.ProcessRequest<object>(new Func<HandlerRequest, object>(handler2.Handle), query2);
      return base.Request.CreateResponse<object>(HttpStatusCode.OK, obj2);
    }

    [System.Web.Http.HttpGet]
    public HttpResponseMessage Search(string term, bool includeStandardTemplateFields = false, string fields = "", int page = 0, int pageSize = 10, string database = "", string language = "", string sorting = "", string facet = "")
    {
      if (pageSize < 1)
      {
        pageSize = 10;
      }
      SearchQuery query = new SearchQuery
      {
        Term = term,
        Database = database,
        Language = language,
        Sorting = sorting,
        Page = page,
        PageSize = pageSize,
        Facet = facet
      };
      IItemRequestHandler handler = this._handlerProvider.GetHandler<SearchHandler>();
      ItemSearchResults results = (ItemSearchResults)this.ProcessRequest<object>(new Func<HandlerRequest, object>(handler.Handle), query);
      IItemRequestHandler handler2 = this._handlerProvider.GetHandler<FormatItemSearchResultsHandler>();
      FormatItemSearchResultsQuery query2 = new FormatItemSearchResultsQuery
      {
        ItemSearchResults = results,
        Fields = fields,
        IncludeStandardTemplateFields = includeStandardTemplateFields,
        Page = page,
        PageSize = pageSize,
        RequestMessage = base.Request,
        Controller = "ItemService-Search",
        RouteValues = new
        {
          includeStandardTemplateFields = includeStandardTemplateFields,
          fields = fields,
          database = database,
          sorting = sorting,
          term = term,
          facet = facet
        }
      };
      object obj2 = this.ProcessRequest<object>(new Func<HandlerRequest, object>(handler2.Handle), query2);
      return base.Request.CreateResponse<object>(HttpStatusCode.OK, obj2);
    }

    [System.Web.Http.HttpGet]
    public HttpResponseMessage SearchViaItem([ModelBinder(typeof(SearchViaItemQueryModelBinder))] SearchViaItemQuery query, int page = 0, int pageSize = 10)
    {
      if (pageSize < 1)
      {
        pageSize = 10;
      }
      IItemRequestHandler handler = this._handlerProvider.GetHandler<SearchViaItemHandler>();
      SearchViaItemQueryResponse response = (SearchViaItemQueryResponse)this.ProcessRequest<object>(new Func<HandlerRequest, object>(handler.Handle), query);
      IItemRequestHandler handler2 = this._handlerProvider.GetHandler<FormatItemSearchResultsHandler>();
      FormatItemSearchResultsQuery query2 = new FormatItemSearchResultsQuery
      {
        ItemSearchResults = response.ItemSearchResults,
        Fields = response.SearchRequest.Fields,
        IncludeStandardTemplateFields = response.SearchRequest.IncludeStandardTemplateFields,
        Page = page,
        PageSize = pageSize,
        RequestMessage = base.Request,
        Controller = "ItemService-SearchViaItem",
        RouteValues = new
        {
          includeStandardTemplateFields = response.SearchRequest.IncludeStandardTemplateFields,
          fields = response.SearchRequest.Fields,
          database = response.SearchRequest.Database,
          sorting = response.SearchRequest.Sorting,
          Term = query.Term,
          facet = response.SearchRequest.Facet
        }
      };
      object obj2 = this.ProcessRequest<object>(new Func<HandlerRequest, object>(handler2.Handle), query2);
      return base.Request.CreateResponse<object>(HttpStatusCode.OK, obj2);
    }

    [System.Web.Http.ActionName("DefaultAction"), System.Web.Http.HttpPatch]
    public HttpResponseMessage Update(Guid id, [FromBody] ItemModel itemModel, string database = "", string language = "", string version = "")
    {
      UpdateItemCommand query = new UpdateItemCommand
      {
        Id = id,
        ItemModel = itemModel,
        Database = database,
        Language = language,
        Version = version
      };
      IItemRequestHandler handler = this._handlerProvider.GetHandler<UpdateItemHandler>();
      this.ProcessRequest<object>(new Func<HandlerRequest, object>(handler.Handle), query);
      return new HttpResponseMessage(HttpStatusCode.NoContent);
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
