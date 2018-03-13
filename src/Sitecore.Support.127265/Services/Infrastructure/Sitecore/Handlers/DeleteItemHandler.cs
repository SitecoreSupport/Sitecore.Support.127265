namespace Sitecore.Support.Services.Infrastructure.Sitecore.Handlers
{
  using System;
  using global::Sitecore.Services.Infrastructure.Model;
  using global::Sitecore.Services.Infrastructure.Sitecore.Data;
  using global::Sitecore.Services.Infrastructure.Sitecore.Handlers;

  public class DeleteItemHandler : ItemCommandHandler<DeleteItemCommand>
  {
    public DeleteItemHandler(IItemRepository itemRepository) : base(itemRepository)
    {
    }

    protected override object HandleRequest(DeleteItemCommand request)
    {
      string databaseName = string.IsNullOrEmpty(request.Database) ? "master" : request.Database;
      ItemRepository.Delete(request.Id, databaseName, request.Language);
      return null;
    }
  }
}
