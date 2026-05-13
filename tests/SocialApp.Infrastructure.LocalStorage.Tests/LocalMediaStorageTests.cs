using FluentAssertions;
using SocialApp.Infrastructure.LocalStorage.Gateways;
using SocialApp.Infrastructure.LocalStorage.Options;
using SocialApp.Post.Entities;
using SocialApp.Post.Gateways;
using SocialApp.User.Gateways;
using Xunit;

namespace SocialApp.Infrastructure.LocalStorage.Tests;

public sealed class LocalMediaStorageTests
{
    [Fact]
    public async Task Profile_image_bytes_are_read_from_disk_by_new_gateway_instance()
    {
        var root = NewRoot();
        var first = ProfileImages(root);
        var reservation = first.ReserveUpload(new ReserveProfileImageUpload("@ada", "image/png", 4));
        await first.StoreUploadAsync(reservation.AssetId, new MemoryStream(new byte[] { 1, 2, 3, 4 }));
        first.CompleteUpload(new CompleteReservedProfileImageUpload(reservation.AssetId, "@ada"));

        var second = ProfileImages(root);
        var stored = second.FindStored(reservation.AssetId);

        stored.Should().NotBeNull();
        stored!.ContentType.Should().Be("image/png");
        using var content = new MemoryStream();
        stored.Content.CopyTo(content);
        content.ToArray().Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public void Post_media_completion_requires_file_to_exist_on_disk()
    {
        var storage = PostMedia(NewRoot());
        var reservation = storage.ReserveUpload(new ReservePostMediaUpload(
            "@ada",
            PostMediaKind.Image,
            "image/jpeg",
            4,
            100,
            100,
            null,
            null,
            null));

        var completed = storage.CompleteUpload(new CompleteReservedPostMediaUpload(reservation.AssetId, "@ada"));

        completed.Should().BeNull();
    }

    [Fact]
    public async Task Post_media_bytes_are_completed_from_disk_by_new_gateway_instance()
    {
        var root = NewRoot();
        var first = PostMedia(root);
        var reservation = first.ReserveUpload(new ReservePostMediaUpload(
            "@ada",
            PostMediaKind.Image,
            "image/jpeg",
            4,
            100,
            100,
            null,
            null,
            "diagram"));
        await first.StoreUploadAsync(reservation.AssetId, new MemoryStream(new byte[] { 1, 2, 3, 4 }));

        var second = PostMedia(root);
        var completed = second.CompleteUpload(new CompleteReservedPostMediaUpload(reservation.AssetId, "@ada"));

        completed.Should().NotBeNull();
        completed!.AssetId.Should().Be(reservation.AssetId);
        completed.ContentType.Should().Be("image/jpeg");
        completed.AltText.Should().Be("diagram");
        second.FindCompletedAsset(reservation.AssetId, "@ada").Should().NotBeNull();
    }

    private static string NewRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "socialapp-local-media-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static FileSystemProfileImageStorageGateway ProfileImages(string root) =>
        new(Microsoft.Extensions.Options.Options.Create(new LocalMediaStorageOptions { RootPath = root }));

    private static FileSystemPostMediaStorageGateway PostMedia(string root) =>
        new(Microsoft.Extensions.Options.Options.Create(new LocalMediaStorageOptions { RootPath = root }));
}
