using MongoDB.Driver;
using SocialApp.Infrastructure.CosmosMongo.Documents;
using SocialApp.Post.Entities;
using SocialApp.Post.Gateways;

namespace SocialApp.Infrastructure.CosmosMongo.Gateways;

public sealed class CosmosMongoPostGateway(CosmosMongoCollections collections) : IPostGateway, IPostSearchGateway
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
            .FirstOrDefault(p => string.Equals(p.AuthorHandle, SocialPost.NormalizeHandle(authorHandle), StringComparison.OrdinalIgnoreCase)) is { } document
            ? CosmosMongoMappers.ToEntity(document)
            : null;

    public int CountActiveReposts(Guid originalPostId) =>
        (int)_posts.CountDocuments(p => !p.IsDeleted && p.OriginalPostId == originalPostId);

    public int CountReplies(Guid parentPostId) =>
        (int)_posts.CountDocuments(p => !p.IsDeleted && p.ParentPostId == parentPostId);

    public int CountConversationReplies(Guid rootPostId) =>
        CountConversationReplies(rootPostId, _posts.Find(p => !p.IsDeleted).ToList());

    public IReadOnlyList<SocialPost> RecentReplies(Guid parentPostId, int limit) =>
        _posts.Find(p => !p.IsDeleted && p.ParentPostId == parentPostId).ToList()
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .Select(CosmosMongoMappers.ToEntity)
            .ToArray();

    public IReadOnlyList<SocialPost> ScrollFor(string readerHandle, int limit)
    {
        var normalizedReaderHandle = SocialPost.NormalizeHandle(readerHandle);
        var followedHandles = _follows.Find(f => f.ReaderHandle == normalizedReaderHandle).FirstOrDefault()?.Handles ?? Array.Empty<string>();
        var blockedHandles = _blocks.Find(b => b.ReaderHandle == normalizedReaderHandle).FirstOrDefault()?.Handles ?? Array.Empty<string>();
        var activePosts = _posts.Find(p => !p.IsDeleted).ToList();
        var query = activePosts.Where(p => p.ParentPostId is null);

        return query
            .Where(p => followedHandles.Length == 0 || followedHandles.Contains(p.AuthorHandle, StringComparer.OrdinalIgnoreCase))
            .Where(p => !blockedHandles.Contains(p.AuthorHandle, StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(p => LatestConversationActivityAt(p.Id, activePosts))
            .Take(limit)
            .Select(CosmosMongoMappers.ToEntity)
            .ToArray();
    }

    public IReadOnlyList<SocialPost> RecentByAuthor(string authorHandle, int limit) =>
        _posts.Find(p => !p.IsDeleted).ToList()
            .Where(p => string.Equals(p.AuthorHandle, SocialPost.NormalizeHandle(authorHandle), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .Select(CosmosMongoMappers.ToEntity)
            .ToArray();

    public IReadOnlyList<SocialPost> Search(string query)
    {
        var allPosts = _posts.Find(p => !p.IsDeleted).ToList();
        var postsById = allPosts.ToDictionary(p => p.Id);

        return allPosts
            .Where(p => p.ParentPostId is null)
            .Where(p => MatchesContent(p, query, postsById))
            .OrderByDescending(p => p.CreatedAt)
            .Select(CosmosMongoMappers.ToEntity)
            .ToArray();
    }

    private static bool MatchesContent(PostDocument post, string query, IReadOnlyDictionary<Guid, PostDocument> postsById) =>
        post.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        post.OriginalPostId is { } originalPostId &&
        postsById.TryGetValue(originalPostId, out var originalPost) &&
        originalPost.Content.Contains(query, StringComparison.OrdinalIgnoreCase);

    private static int CountConversationReplies(Guid postId, IReadOnlyList<PostDocument> posts)
    {
        var replies = posts.Where(p => p.ParentPostId == postId).ToArray();
        return replies.Length + replies.Sum(reply => CountConversationReplies(reply.Id, posts));
    }

    private static DateTimeOffset LatestConversationActivityAt(Guid postId, IReadOnlyList<PostDocument> posts)
    {
        var post = posts.Single(p => p.Id == postId);
        var replies = posts.Where(p => p.ParentPostId == postId).ToArray();
        return replies
            .Select(reply => LatestConversationActivityAt(reply.Id, posts))
            .Append(post.CreatedAt)
            .Max();
    }

    public void Follow(string readerHandle, string followedHandle) => AddToSet(_follows, readerHandle, followedHandle);

    public void Block(string readerHandle, string blockedHandle) => AddToSet(_blocks, readerHandle, blockedHandle);

    private static void AddToSet(IMongoCollection<HandleSetDocument> collection, string readerHandle, string handle)
    {
        var normalizedReaderHandle = SocialPost.NormalizeHandle(readerHandle);
        var normalizedHandle = SocialPost.NormalizeHandle(handle);
        var update = Builders<HandleSetDocument>.Update
            .SetOnInsert(d => d.ReaderHandle, normalizedReaderHandle)
            .AddToSet(d => d.Handles, normalizedHandle);
        collection.UpdateOne(
            d => d.ReaderHandle == normalizedReaderHandle,
            update,
            new UpdateOptions { IsUpsert = true });
    }
}
