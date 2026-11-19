using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace IdP.Infrastructure;

/// <summary>
/// Keeps the ASP.NET Core data protection keys in MongoDB, so cookies, antiforgery tokens, e-mail tokens and the
/// protected signing keys survive restarts and are shared by every instance.
/// </summary>
public sealed class MongoXmlRepository(IMongoCollection<MongoXmlRepository.KeyDocument> keys) : IXmlRepository
{
    public IReadOnlyCollection<XElement> GetAllElements()
        => keys.Find(FilterDefinition<KeyDocument>.Empty).ToList().Select(k => XElement.Parse(k.Xml)).ToList();

    public void StoreElement(XElement element, string friendlyName)
        => keys.ReplaceOne(
            k => k.Id == friendlyName,
            new KeyDocument { Id = friendlyName, Xml = element.ToString(SaveOptions.DisableFormatting), Created = DateTime.UtcNow },
            new ReplaceOptions { IsUpsert = true });

    public sealed class KeyDocument
    {
        public string Id { get; set; } = default!;

        public string Xml { get; set; } = default!;

        public DateTime Created { get; set; }
    }
}
