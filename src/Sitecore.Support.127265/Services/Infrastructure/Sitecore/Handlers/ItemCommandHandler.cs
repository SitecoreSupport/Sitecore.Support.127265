namespace Sitecore.Support.Services.Infrastructure.Sitecore.Handlers
{
  using System;
  using global::Sitecore.Services.Infrastructure.Sitecore.Data;
  using global::Sitecore.Services.Infrastructure.Sitecore.Handlers;

  public abstract class ItemCommandHandler<T> : ItemRequestHandler<T> where T : class
  {
    protected readonly IItemRepository ItemRepository;

    protected ItemCommandHandler(IItemRepository itemRepository)
    {
      this.ItemRepository = itemRepository;
    }
  }
}
