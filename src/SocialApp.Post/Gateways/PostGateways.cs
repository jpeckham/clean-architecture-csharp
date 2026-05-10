using SocialApp.Post.Entities;

namespace SocialApp.Post.Gateways;

public interface IPostGateway
{
    SocialPost Save(SocialPost post);
    SocialPost? FindById(Guid id);
    IReadOnlyList<SocialPost> ScrollFor(string readerHandle, int limit);
    void Follow(string readerHandle, string followedHandle);
    void Block(string readerHandle, string blockedHandle);
}

public interface IPostSearchGateway
{
    IReadOnlyList<SocialPost> Search(string query);
}

public sealed class InMemoryPostGateway : IPostGateway
{
    private readonly List<SocialPost> _posts = new();
    private readonly Dictionary<string, HashSet<string>> _follows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _blocks = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<SocialPost> AllPosts => _posts;

    public SocialPost Save(SocialPost post)
    {
        _posts.Add(post);
        return post;
    }

    public SocialPost? FindById(Guid id) => _posts.SingleOrDefault(p => p.Id == id);

    public IReadOnlyList<SocialPost> ScrollFor(string readerHandle, int limit)
    {
        var follows = _follows.GetValueOrDefault(readerHandle) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var blocks = _blocks.GetValueOrDefault(readerHandle) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return _posts.Where(p => !p.IsDeleted)
            .Where(p => follows.Count == 0 || follows.Contains(p.AuthorHandle))
            .Where(p => !blocks.Contains(p.AuthorHandle))
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToArray();
    }

    public void Follow(string readerHandle, string followedHandle) => SetFor(_follows, readerHandle).Add(followedHandle);

    public void Block(string readerHandle, string blockedHandle) => SetFor(_blocks, readerHandle).Add(blockedHandle);

    private static HashSet<string> SetFor(Dictionary<string, HashSet<string>> source, string key)
    {
        if (!source.TryGetValue(key, out var values))
        {
            values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            source[key] = values;
        }

        return values;
    }
}

public sealed class InMemoryPostSearchGateway(IPostGateway posts) : IPostSearchGateway
{
    public IReadOnlyList<SocialPost> Search(string query) =>
        ((InMemoryPostGateway)posts).AllPosts
        .Where(p => !p.IsDeleted && p.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
        .ToArray();
}
