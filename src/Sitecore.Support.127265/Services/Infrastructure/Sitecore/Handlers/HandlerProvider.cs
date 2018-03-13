namespace Sitecore.Support.Services.Infrastructure.Sitecore.Handlers
{
  using System;
  using System.Collections.Generic;
  using Data;
  using Diagnostics;
  using global::Sitecore.Services.Infrastructure.Sitecore;
  using global::Sitecore.Services.Infrastructure.Sitecore.Data;
  using global::Sitecore.Services.Infrastructure.Sitecore.Diagnostics;
  using global::Sitecore.Services.Infrastructure.Sitecore.Handlers;

  public class HandlerProvider : IHandlerProvider
  {
    private readonly Dictionary<Type, Func<IItemRequestHandler>> _handlers;

    public HandlerProvider()
    {
      Dictionary<Type, Func<IItemRequestHandler>> dictionary = new Dictionary<Type, Func<IItemRequestHandler>> {
                {
                    typeof(CreateItemHandler),
                    new Func<IItemRequestHandler>(HandlerProvider.BuildCreateItemHandler)
                },
                {
                    typeof(DeleteItemHandler),
                    new Func<IItemRequestHandler>(HandlerProvider.BuildDeleteItemHandler)
                },
                {
                    typeof(FormatItemsHandler),
                    new Func<IItemRequestHandler>(HandlerProvider.BuildFormatItemsHandler)
                },
                {
                    typeof(FormatItemSearchResultsHandler),
                    new Func<IItemRequestHandler>(HandlerProvider.BuildFormatItemSearchResultsHandler)
                },
                {
                    typeof(GetItemByContentPathHandler),
                    new Func<IItemRequestHandler>(HandlerProvider.BuildGetItemByContentPathHandler)
                },
                {
                    typeof(GetItemByIdHandler),
                    new Func<IItemRequestHandler>(HandlerProvider.BuildGetItemByIdHandler)
                },
                {
                    typeof(GetItemChildrenHandler),
                    new Func<IItemRequestHandler>(HandlerProvider.BuildGetItemChildrenHandler)
                },
                {
                    typeof(SearchHandler),
                    new Func<IItemRequestHandler>(HandlerProvider.BuildSearchHandler)
                },
                {
                    typeof(SearchViaItemHandler),
                    new Func<IItemRequestHandler>(HandlerProvider.BuildSearchViaItemHandler)
                },
                {
                    typeof(SitecoreQueryViaItemHandler),
                    new Func<IItemRequestHandler>(HandlerProvider.BuildSitecoreQueryViaItemHandler)
                },
                {
                    typeof(UpdateItemHandler),
                    new Func<IItemRequestHandler>(HandlerProvider.BuildUpdateItemHandler)
                }
            };
      this._handlers = dictionary;
    }

    private static IItemRequestHandler BuildCreateItemHandler() =>
        new CreateItemHandler(ResolveItemRepository());

    private static IItemRequestHandler BuildDeleteItemHandler() =>
        new DeleteItemHandler(ResolveItemRepository());

    private static IItemRequestHandler BuildFormatItemSearchResultsHandler() =>
        new FormatItemSearchResultsHandler(ResolveModelFactory());

    private static IItemRequestHandler BuildFormatItemsHandler() =>
        new FormatItemsHandler(ResolveModelFactory());

    private static IItemRequestHandler BuildGetItemByContentPathHandler() =>
        new GetItemByContentPathHandler(ResolveItemRepository(), ResolveModelFactory());

    private static IItemRequestHandler BuildGetItemByIdHandler() =>
        new GetItemByIdHandler(ResolveItemRepository(), ResolveModelFactory());

    private static IItemRequestHandler BuildGetItemChildrenHandler() =>
        new GetItemChildrenHandler(ResolveItemRepository(), ResolveModelFactory());

    private static IItemRequestHandler BuildSearchHandler() =>
        new SearchHandler(ResolveItemSearch());

    private static IItemRequestHandler BuildSearchViaItemHandler() =>
        new SearchViaItemHandler(ResolveItemRepository(), ResolveItemSearch());

    private static IItemRequestHandler BuildSitecoreQueryViaItemHandler() =>
        new SitecoreQueryViaItemHandler(ResolveItemRepository());

    private static IItemRequestHandler BuildUpdateItemHandler() =>
        new UpdateItemHandler(ResolveItemRepository());

    public IItemRequestHandler GetHandler<T>() where T : class
    {
      Type key = typeof(T);
      if (!this._handlers.ContainsKey(key))
      {
        throw new InvalidOperationException($"Failed to create {key.FullName}");
      }
      return this._handlers[key]();
    }

    private static IItemRepository ResolveItemRepository() =>
        new global::Sitecore.Support.Services.Infrastructure.Sitecore.Data.ItemRepository(new SitecoreLogger());

    private static IItemSearch ResolveItemSearch() =>
        new ItemSearch();

    private static IModelFactory ResolveModelFactory() =>
        new ModelFactory();
  }
}
