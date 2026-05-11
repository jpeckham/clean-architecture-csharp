using MongoDB.Driver;
using SocialApp.Infrastructure.CosmosMongo.Documents;
using SocialApp.Post.Entities;
using SocialApp.Post.Gateways;

namespace SocialApp.Infrastructure.CosmosMongo.Gateways;

public sealed class CosmosMongoPostGateway(CosmosMongoCollections collections) : IPostGateway
{
    private readonly IMongoCollection<PostDocument> _posts = collections.Posts;
    private readonly IMongoCollection<HandleSetDocument> _follows = collections.Follows;
    private readonly IMongoCollection<HandleSetDocument> _blocks = collections.Blocks;

    public SocialPost Save(SocialPost post)
    {
        _posts.ReplaceOne(p => p.Id == post.Id, CosmosMongoMappers.ToDocument(post), new ReplaceOptions { IsUpsert = true });
        return post;
    }

    public SocialPost? FindById(Guid id) =>
        _posts.Find(p => p.Id == id).FirstOrDefault() is { } document
            ? CosmosMongoMappers.ToEntity(document)
            : null;

    public SocialPost? FindActiveRepost(Guid originalPostId, string authorHandle) =>
        _posts.Find(p => !p.IsDeleted && p.OriginalPostId == originalPostId)
            .ToList()
            .FirstOrDefault(p => string.Equals(p.AuthorHandle, authorHandle, StringComparison.OrdinalIgnoreCase)) is { } document
            ? CosmosMongoMappers.ToEntity(document)
            : null;

    public int CountActiveReposts(Guid originalPostId) =>
        (int)_posts.CountDocuments(p => !p.IsDeleted && p.OriginalPostId == originalPostId);

    public IReadOnlyList<SocialPost> ScrollFor(string readerHandle, int limit)
    {
        var followedHandles = _follows.Find(f => f.ReaderHandle == readerHandle).FirstOrDefault()?.Handles ?? Array.Empty<string>();
        var blockedHandles = _blocks.Find(b => b.ReaderHandle == readerHandle).FirstOrDefault()?.Handles ?? Array.Empty<string>();
        var query = _posts.Find(p => !p.IsDeleted).ToList();

        return query
            .Where(p => followedHandles.Length == 0 || followedHandles.Contains(p.AuthorHandle, StringComparer.OrdinalIgnoreCase))
            .Where(p => !blockedHandles.Contains(p.AuthorHandle, StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .Select(CosmosMongoMappers.ToEntity)
            .ToArray();
    }

    public void Follow(string readerHandle, string followedHandle) => AddToSet(_follows, readerHandle, followedHandle);

    public void Block(string readerHandle, string blockedHandle) => AddToSet(_blocks, readerHandle, blockedHandle);

    private static void AddToSet(IMongoCollection<HandleSetDocument> collection, string readerHandle, string handle)
    {
        var update = Builders<HandleSetDocument>.Update
            .SetOnInsert(d => d.ReaderHandle, readerHandle)
            .AddToSet(d => d.Handles, handle);
        collection.UpdateOne(
            d => d.ReaderHandle == readerHandle,
            update,
            new UpdateOptions { IsUpsert = true });
    }
}
