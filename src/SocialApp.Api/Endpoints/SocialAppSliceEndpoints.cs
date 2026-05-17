using SocialApp.Api.Contracts;
using SocialApp.Post.Controllers;
using SocialApp.Post.Entities;
using SocialApp.Post.Gateways;
using SocialApp.Post.Presenters;
using SocialApp.Post.UseCases;
using SocialApp.Post.ViewModels;
using SocialApp.User.Controllers;
using SocialApp.User.Gateways;
using SocialApp.User.Presenters;
using SocialApp.User.UseCases;
using SocialApp.User.ViewModels;

namespace SocialApp.Api.Endpoints;

public static class SocialAppSliceEndpoints
{
    private const string GetProfileImageRouteName = "GetProfileImage";

    public static IEndpointRouteBuilder MapSocialAppSlice(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/accounts", CreateAccount);
        endpoints.MapPost("/api/registrations", RegisterAccount);
        endpoints.MapPost("/api/registrations/verify", VerifyRegistration);
        endpoints.MapPost("/api/sessions", Login);
        endpoints.MapPost("/api/sessions/device", LoginWithDevice);
        endpoints.MapPost("/api/sessions/device/verify", VerifyDeviceOtp);
        endpoints.MapPost("/api/password-reset-requests", RequestPasswordReset);
        endpoints.MapPost("/api/password-resets", ResetPassword);
        endpoints.MapGet("/api/users/{handle}", ViewUser);
        endpoints.MapPost("/api/users/me/profile-image/upload-sessions", BeginMyProfileImageUploadSession);
        endpoints.MapPost("/api/users/me/profile-image/complete", CompleteMyProfileImageUpload);
        endpoints.MapPost("/api/users/me/profile-image", UploadMyProfileImage);
        endpoints.MapDelete("/api/users/me/profile-image", RemoveMyProfileImage);
        endpoints.MapGet("/api/profile-images/{assetId:guid}", GetProfileImage)
            .WithName(GetProfileImageRouteName);
        endpoints.MapPost("/api/posts/media/upload-sessions", BeginPostMediaUploadSession);
        endpoints.MapPost("/api/posts/media/{assetId:guid}/complete", CompletePostMediaUpload);
        endpoints.MapGet("/api/post-media/{assetId:guid}", GetPostMedia);
        endpoints.MapPut("/api/media/uploads/{target}/{assetId:guid}", StoreMediaUpload);
        endpoints.MapPost("/api/posts", CreatePost);
        endpoints.MapGet("/api/posts/{postId:guid}", GetPost);
        endpoints.MapDelete("/api/posts/{postId:guid}", DeletePost);
        endpoints.MapPost("/api/posts/{postId:guid}/likes", AddLikeToPost);
        endpoints.MapDelete("/api/posts/{postId:guid}/likes", DeleteLikeFromPost);
        endpoints.MapPost("/api/posts/{postId:guid}/replies", ReplyToPost);
        endpoints.MapPost("/api/posts/{postId:guid}/reposts", RepostPost);
        endpoints.MapDelete("/api/posts/{postId:guid}/reposts/mine", DeleteMyRepost);
        endpoints.MapGet("/api/posts/search", SearchPosts);
        endpoints.MapGet("/api/posts/recent", RecentPosts);
        return endpoints;
    }

    private static IResult CreateAccount(CreateAccountHttpRequest request, IUserGateway users, ISessionGateway sessions)
    {
        var presenter = new CreateAccountPresenter();
        var controller = new CreateAccountController(new CreateAccountInteractor(users, sessions, presenter));

        try
        {
            controller.Create(request.DisplayName, request.Handle, request.Email, request.Password);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { succeeded = false, message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { succeeded = false, message = ex.Message });
        }

        return presenter.ViewModel is { Succeeded: true } viewModel
            ? Results.Created($"/api/accounts/{Uri.EscapeDataString(viewModel.Handle!)}", viewModel)
            : Results.BadRequest(presenter.ViewModel);
    }

    private static IResult RegisterAccount(
        RegisterAccountHttpRequest request,
        IUserGateway users,
        IPendingRegistrationGateway registrations,
        IVerificationCodeGateway codes,
        IEmailGateway email)
    {
        var presenter = new RegisterAccountPresenter();
        var controller = new RegisterAccountController(new RegisterAccountInteractor(users, registrations, codes, email, presenter));

        try
        {
            controller.Register(request.DisplayName, request.Handle, request.Email, request.Password);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { succeeded = false, message = ex.Message });
        }

        return presenter.ViewModel is { Succeeded: true }
            ? Results.Accepted($"/api/registrations/verify?email={Uri.EscapeDataString(request.Email)}", presenter.ViewModel)
            : Results.BadRequest(presenter.ViewModel);
    }

    private static IResult VerifyRegistration(
        VerifyRegistrationHttpRequest request,
        IUserGateway users,
        IPendingRegistrationGateway registrations,
        IVerificationCodeGateway codes)
    {
        var presenter = new VerifyRegistrationPresenter();
        new VerifyRegistrationController(new VerifyRegistrationInteractor(users, registrations, codes, presenter))
            .Verify(request.Email, request.Code);
        return presenter.ViewModel is { Succeeded: true }
            ? Results.Ok(presenter.ViewModel)
            : Results.BadRequest(presenter.ViewModel);
    }

    private static IResult Login(LoginHttpRequest request, IUserGateway users, ISessionGateway sessions)
    {
        var presenter = new LoginPresenter();
        var controller = new LoginController(new LoginInteractor(users, sessions, presenter));
        controller.Login(request.Email, request.Password);

        return presenter.ViewModel is { Succeeded: true }
            ? Results.Ok(presenter.ViewModel)
            : Results.Unauthorized();
    }

    private static IResult LoginWithDevice(
        LoginWithDeviceHttpRequest request,
        IUserGateway users,
        ISessionGateway sessions,
        IRememberedDeviceGateway devices,
        IVerificationCodeGateway codes,
        IEmailGateway email)
    {
        var presenter = new LoginWithDevicePresenter();
        new LoginWithDeviceController(new LoginWithDeviceInteractor(users, sessions, devices, codes, email, presenter))
            .Login(request.Email, request.Password, request.DeviceId);

        return presenter.ViewModel is { Succeeded: true }
            ? Results.Ok(presenter.ViewModel)
            : Results.Unauthorized();
    }

    private static IResult VerifyDeviceOtp(
        VerifyDeviceOtpHttpRequest request,
        IUserGateway users,
        ISessionGateway sessions,
        IRememberedDeviceGateway devices,
        IVerificationCodeGateway codes)
    {
        var presenter = new VerifyDeviceOtpPresenter();
        new VerifyDeviceOtpController(new VerifyDeviceOtpInteractor(users, sessions, devices, codes, presenter))
            .Verify(request.Handle, request.DeviceId, request.Code, request.RememberDevice);

        return presenter.ViewModel is { Succeeded: true }
            ? Results.Ok(presenter.ViewModel)
            : Results.BadRequest(presenter.ViewModel);
    }

    private static IResult RequestPasswordReset(
        RequestPasswordResetHttpRequest request,
        HttpRequest httpRequest,
        IUserGateway users,
        IPasswordResetTokenGateway resets,
        IEmailGateway email,
        IClock clock,
        IConfiguration configuration)
    {
        var baseUrl = configuration["Web:PasswordResetBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = $"{httpRequest.Scheme}://{httpRequest.Host}/reset-password";
        }

        var presenter = new RequestPasswordResetPresenter();
        new RequestPasswordResetController(new RequestPasswordResetInteractor(users, resets, email, clock, presenter))
            .RequestReset(request.Email, baseUrl);
        return Results.Accepted(null, presenter.ViewModel);
    }

    private static IResult ResetPassword(
        ResetPasswordHttpRequest request,
        IUserGateway users,
        IPasswordResetTokenGateway resets,
        IClock clock)
    {
        var presenter = new ResetPasswordPresenter();
        new ResetPasswordController(new ResetPasswordInteractor(users, resets, clock, presenter))
            .Reset(request.Token, request.NewPassword);
        return presenter.ViewModel is { Succeeded: true }
            ? Results.Ok(presenter.ViewModel)
            : Results.BadRequest(presenter.ViewModel);
    }

    private static IResult ViewUser(
        string handle,
        HttpRequest httpRequest,
        IUserGateway users,
        ISessionGateway sessions,
        IUserProfilePostGateway profilePosts,
        LinkGenerator links)
    {
        var token = ReadBearerToken(httpRequest);
        if (string.IsNullOrWhiteSpace(token))
        {
            return Results.Unauthorized();
        }

        var reader = sessions.FindByToken(token);
        if (reader is null)
        {
            return Results.Unauthorized();
        }

        var presenter = new ViewUserPresenter();
        new ViewUserController(new ViewUserInteractor(users, profilePosts, presenter)).View(handle, reader.Handle);
        return presenter.ViewModel is { Succeeded: true } viewModel
            ? Results.Ok(ToHttpResponse(viewModel, users, httpRequest.HttpContext, links))
            : Results.NotFound(ToHttpResponse(presenter.ViewModel!, users, httpRequest.HttpContext, links));
    }

    private static async Task<IResult> UploadMyProfileImage(
        HttpRequest httpRequest,
        IUserGateway users,
        ISessionGateway sessions,
        IProfileImageStorageGateway profileImages,
        IClock clock,
        LinkGenerator links)
    {
        var user = ReadAuthenticatedUser(httpRequest, sessions);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        if (!httpRequest.HasFormContentType)
        {
            return Results.BadRequest(new { succeeded = false, message = "Profile image upload must be multipart form data." });
        }

        var form = await httpRequest.ReadFormAsync();
        var file = form.Files["image"];
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { succeeded = false, message = "Profile image file is required." });
        }

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
        var beginPresenter = new BeginProfileImageUploadPresenter();
        try
        {
            new BeginProfileImageUploadController(new BeginProfileImageUploadInteractor(profileImages, beginPresenter))
                .Begin(user.Handle, contentType, file.Length);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { succeeded = false, message = ex.Message });
        }

        var assetId = beginPresenter.ViewModel!.AssetId!.Value;
        await using (var stream = file.OpenReadStream())
        {
            await profileImages.StoreUploadAsync(assetId, stream, httpRequest.HttpContext.RequestAborted);
        }

        var previousImage = user.ProfileImage;
        var completePresenter = new CompleteProfileImageUploadPresenter();
        new CompleteProfileImageUploadController(new CompleteProfileImageUploadInteractor(users, profileImages, clock, completePresenter))
            .Complete(user.Handle, user.Handle, assetId, null, null);

        if (previousImage is not null)
        {
            profileImages.Remove(previousImage.AssetId);
        }

        return completePresenter.ViewModel is { Succeeded: true } viewModel
            ? Results.Ok(ToHttpResponse(viewModel, httpRequest.HttpContext, links))
            : Results.BadRequest(ToHttpResponse(completePresenter.ViewModel!, httpRequest.HttpContext, links));
    }

    private static IResult BeginMyProfileImageUploadSession(
        BeginProfileImageUploadHttpRequest request,
        HttpRequest httpRequest,
        ISessionGateway sessions,
        IProfileImageStorageGateway profileImages)
    {
        var user = ReadAuthenticatedUser(httpRequest, sessions);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var presenter = new BeginProfileImageUploadPresenter();
        try
        {
            new BeginProfileImageUploadController(new BeginProfileImageUploadInteractor(profileImages, presenter))
                .Begin(user.Handle, request.ContentType, request.ByteLength);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { succeeded = false, message = ex.Message });
        }

        return presenter.ViewModel is { Succeeded: true } viewModel
            ? Results.Accepted(viewModel.UploadUrl, viewModel)
            : Results.BadRequest(presenter.ViewModel);
    }

    private static IResult CompleteMyProfileImageUpload(
        CompleteProfileImageUploadHttpRequest request,
        HttpRequest httpRequest,
        IUserGateway users,
        ISessionGateway sessions,
        IProfileImageStorageGateway profileImages,
        IClock clock,
        LinkGenerator links)
    {
        var user = ReadAuthenticatedUser(httpRequest, sessions);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var previousImage = user.ProfileImage;
        var presenter = new CompleteProfileImageUploadPresenter();
        new CompleteProfileImageUploadController(new CompleteProfileImageUploadInteractor(users, profileImages, clock, presenter))
            .Complete(user.Handle, user.Handle, request.AssetId, request.Width, request.Height);

        if (presenter.ViewModel is { Succeeded: true } viewModel)
        {
            if (previousImage is not null)
            {
                profileImages.Remove(previousImage.AssetId);
            }

            return Results.Ok(ToHttpResponse(viewModel, httpRequest.HttpContext, links));
        }

        return Results.NotFound(ToHttpResponse(presenter.ViewModel!, httpRequest.HttpContext, links));
    }

    private static IResult RemoveMyProfileImage(
        HttpRequest httpRequest,
        IUserGateway users,
        ISessionGateway sessions,
        IProfileImageStorageGateway profileImages)
    {
        var user = ReadAuthenticatedUser(httpRequest, sessions);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var previousImage = user.ProfileImage;
        var presenter = new RemoveProfileImagePresenter();
        new RemoveProfileImageController(new RemoveProfileImageInteractor(users, presenter)).Remove(user.Handle, user.Handle);
        if (previousImage is not null)
        {
            profileImages.Remove(previousImage.AssetId);
        }

        return presenter.ViewModel is { Succeeded: true }
            ? Results.Ok(presenter.ViewModel)
            : Results.NotFound(presenter.ViewModel);
    }

    private static IResult GetProfileImage(Guid assetId, IProfileImageStorageGateway profileImages)
    {
        var image = profileImages.FindStored(assetId);
        return image is null
            ? Results.NotFound()
            : Results.File(image.Content, image.ContentType);
    }

    private static ProfileImageUploadHttpResult ToHttpResponse(
        CompleteProfileImageUploadViewModel viewModel,
        HttpContext httpContext,
        LinkGenerator links) =>
        new(viewModel.Succeeded, viewModel.Message, ToHttpProfileImage(viewModel.ProfileImage, httpContext, links));

    private static UserProfileHttpResult ToHttpResponse(
        ViewUserViewModel viewModel,
        IUserGateway users,
        HttpContext httpContext,
        LinkGenerator links) =>
        new(
            viewModel.Succeeded,
            viewModel.Message,
            viewModel.Handle,
            viewModel.DisplayName,
            ToHttpProfileImage(viewModel.ProfileImage, httpContext, links),
            viewModel.Posts.Select(post => ToHttpUserPostSummary(post, users, httpContext, links)).ToArray());

    private static ProfileImageHttpResult? ToHttpProfileImage(
        ProfileImageViewModel? image,
        HttpContext httpContext,
        LinkGenerator links) =>
        image is null
            ? null
            : new(
                image.AssetId,
                image.StorageKey,
                image.ContentType,
                image.ByteLength,
                image.Width,
                image.Height,
                image.UploadedAt,
                links.GetPathByName(httpContext, GetProfileImageRouteName, new { assetId = image.AssetId })
                    ?? throw new InvalidOperationException("Profile image route is not registered."));

    private static IResult GetPostMedia(Guid assetId, IPostMediaStorageGateway postMedia)
    {
        var media = postMedia.FindStored(assetId);
        return media is null
            ? Results.NotFound()
            : Results.File(media.Content, media.ContentType);
    }

    private static async Task<IResult> StoreMediaUpload(
        string target,
        Guid assetId,
        HttpRequest httpRequest,
        IMediaUploadGatewayResolver uploadGateways)
    {
        var storeUpload = uploadGateways.Resolve(target);
        if (storeUpload is null)
        {
            return Results.NotFound(new { succeeded = false, message = "Upload target was not found." });
        }

        try
        {
            await storeUpload(assetId, httpRequest.Body, httpRequest.HttpContext.RequestAborted);
            return Results.NoContent();
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound(new { succeeded = false, message = "Upload session was not found." });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { succeeded = false, message = ex.Message });
        }
    }

    private static IResult BeginPostMediaUploadSession(
        BeginPostMediaUploadHttpRequest request,
        HttpRequest httpRequest,
        ISessionGateway sessions,
        IPostMediaStorageGateway media)
    {
        var user = ReadAuthenticatedUser(httpRequest, sessions);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        if (!Enum.TryParse<PostMediaKind>(request.Kind, ignoreCase: true, out var kind))
        {
            return Results.BadRequest(new { succeeded = false, message = "Media kind must be Image or Video." });
        }

        var presenter = new BeginPostMediaUploadPresenter();
        try
        {
            new BeginPostMediaUploadController(new BeginPostMediaUploadInteractor(media, presenter))
                .Begin(user.Handle, kind, request.ContentType, request.ByteLength, request.Width, request.Height, request.DurationMs, request.AltText);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { succeeded = false, message = ex.Message });
        }

        return presenter.ViewModel is { Succeeded: true } viewModel
            ? Results.Accepted(viewModel.UploadUrl, viewModel)
            : Results.BadRequest(presenter.ViewModel);
    }

    private static IResult CompletePostMediaUpload(
        Guid assetId,
        HttpRequest httpRequest,
        ISessionGateway sessions,
        IPostMediaStorageGateway media)
    {
        var user = ReadAuthenticatedUser(httpRequest, sessions);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var presenter = new CompletePostMediaUploadPresenter();
        new CompletePostMediaUploadController(new CompletePostMediaUploadInteractor(media, presenter))
            .Complete(assetId, user.Handle);

        return presenter.ViewModel is { Succeeded: true }
            ? Results.Ok(presenter.ViewModel)
            : Results.NotFound(presenter.ViewModel);
    }

    private static IResult CreatePost(CreatePostHttpRequest request, HttpRequest httpRequest, ISessionGateway sessions, IPostGateway posts, IPostMediaStorageGateway media, IAccountHandleGateway accounts)
    {
        var token = ReadBearerToken(httpRequest);
        if (string.IsNullOrWhiteSpace(token))
        {
            return Results.Unauthorized();
        }

        var user = sessions.FindByToken(token);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var presenter = new CreatePostPresenter();
        var controller = new CreatePostController(new CreatePostInteractor(posts, presenter, media, accounts));

        try
        {
            controller.Create(user.Handle, request.Content, request.MediaAssetIds);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { succeeded = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { succeeded = false, message = ex.Message });
        }

        return presenter.ViewModel is { Succeeded: true, Id: not null } viewModel
            ? Results.Created($"/api/posts/{viewModel.Id}", viewModel)
            : Results.BadRequest(presenter.ViewModel);
    }

    private static IResult GetPost(Guid postId, HttpRequest httpRequest, ISessionGateway sessions, IPostGateway posts, IUserGateway users, LinkGenerator links)
    {
        var reader = ReadAuthenticatedUser(httpRequest, sessions);
        if (reader is null)
        {
            return Results.Unauthorized();
        }

        var presenter = new DisplayPostPresenter();
        new DisplayPostController(new DisplayPostInteractor(posts, presenter)).Display(postId, reader.Handle);

        return presenter.ViewModel is { Succeeded: true } viewModel
            ? Results.Ok(ToHttpResponse(viewModel, users, httpRequest.HttpContext, links))
            : Results.NotFound(ToHttpResponse(presenter.ViewModel!, users, httpRequest.HttpContext, links));
    }

    private static IResult DeletePost(Guid postId, HttpRequest httpRequest, ISessionGateway sessions, IPostGateway posts)
    {
        var token = ReadBearerToken(httpRequest);
        if (string.IsNullOrWhiteSpace(token))
        {
            return Results.Unauthorized();
        }

        var user = sessions.FindByToken(token);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var presenter = new DeletePostPresenter();
        var controller = new DeletePostController(new DeletePostInteractor(posts, presenter));

        try
        {
            controller.Delete(postId, user.Handle);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { succeeded = false, message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
        }

        return presenter.ViewModel is { Succeeded: true }
            ? Results.Ok(presenter.ViewModel)
            : Results.NotFound(presenter.ViewModel);
    }

    private static IResult AddLikeToPost(Guid postId, HttpRequest httpRequest, ISessionGateway sessions, IPostGateway posts)
    {
        var token = ReadBearerToken(httpRequest);
        if (string.IsNullOrWhiteSpace(token))
        {
            return Results.Unauthorized();
        }

        var user = sessions.FindByToken(token);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var presenter = new AddLikeToPostPresenter();
        var controller = new AddLikeToPostController(new AddLikeToPostInteractor(posts, presenter));

        try
        {
            controller.AddLike(postId, user.Handle);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { succeeded = false, message = ex.Message });
        }

        return presenter.ViewModel is { Succeeded: true }
            ? Results.Ok(presenter.ViewModel)
            : Results.NotFound(presenter.ViewModel);
    }

    private static IResult DeleteLikeFromPost(Guid postId, HttpRequest httpRequest, ISessionGateway sessions, IPostGateway posts)
    {
        var token = ReadBearerToken(httpRequest);
        if (string.IsNullOrWhiteSpace(token))
        {
            return Results.Unauthorized();
        }

        var user = sessions.FindByToken(token);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var presenter = new DeleteLikeFromPostPresenter();
        var controller = new DeleteLikeFromPostController(new DeleteLikeFromPostInteractor(posts, presenter));

        try
        {
            controller.DeleteLike(postId, user.Handle);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { succeeded = false, message = ex.Message });
        }

        return presenter.ViewModel is { Succeeded: true }
            ? Results.Ok(presenter.ViewModel)
            : Results.NotFound(presenter.ViewModel);
    }

    private static IResult ReplyToPost(ReplyPostHttpRequest request, Guid postId, HttpRequest httpRequest, ISessionGateway sessions, IPostGateway posts, IAccountHandleGateway accounts)
    {
        var token = ReadBearerToken(httpRequest);
        if (string.IsNullOrWhiteSpace(token))
        {
            return Results.Unauthorized();
        }

        var user = sessions.FindByToken(token);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var presenter = new ReplyToPostPresenter();
        var controller = new ReplyToPostController(new ReplyToPostInteractor(posts, presenter, accounts));

        try
        {
            controller.Reply(postId, user.Handle, request.Content ?? string.Empty);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { succeeded = false, message = ex.Message });
        }

        return presenter.ViewModel is { Succeeded: true, Id: not null } viewModel
            ? Results.Created($"/api/posts/{viewModel.Id}", viewModel)
            : Results.NotFound(presenter.ViewModel);
    }

    private static IResult RepostPost(RepostPostHttpRequest request, Guid postId, HttpRequest httpRequest, ISessionGateway sessions, IPostGateway posts, IAccountHandleGateway accounts)
    {
        var token = ReadBearerToken(httpRequest);
        if (string.IsNullOrWhiteSpace(token))
        {
            return Results.Unauthorized();
        }

        var user = sessions.FindByToken(token);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var presenter = new RepostPresenter();
        var controller = new RepostController(new RepostInteractor(posts, presenter, accounts));

        try
        {
            controller.Repost(postId, user.Handle, request.Content ?? string.Empty);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { succeeded = false, message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { succeeded = false, message = ex.Message });
        }

        return presenter.ViewModel is { Succeeded: true, Id: not null } viewModel
            ? Results.Created($"/api/posts/{viewModel.Id}", viewModel)
            : Results.NotFound(presenter.ViewModel);
    }

    private static IResult DeleteMyRepost(Guid postId, HttpRequest httpRequest, ISessionGateway sessions, IPostGateway posts)
    {
        var token = ReadBearerToken(httpRequest);
        if (string.IsNullOrWhiteSpace(token))
        {
            return Results.Unauthorized();
        }

        var user = sessions.FindByToken(token);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var requestedPost = posts.FindById(postId);
        if (requestedPost is null)
        {
            var notFoundPresenter = new DeletePostPresenter();
            notFoundPresenter.Present(new(false, SocialApp.Post.ResponseModels.PostMessageKeys.OriginalPostNotFound));
            return Results.NotFound(notFoundPresenter.ViewModel);
        }

        var targetPostId = requestedPost.OriginalPostId ?? requestedPost.Id;
        var repost = posts.FindActiveRepost(targetPostId, user.Handle);
        if (repost is null)
        {
            var notFoundPresenter = new DeletePostPresenter();
            notFoundPresenter.Present(new(false, SocialApp.Post.ResponseModels.PostMessageKeys.RepostNotFound));
            return Results.NotFound(notFoundPresenter.ViewModel);
        }

        repost.DeleteBy(user.Handle);
        posts.Save(repost);

        var presenter = new DeletePostPresenter();
        presenter.Present(new(true, SocialApp.Post.ResponseModels.PostMessageKeys.RepostDeleted));
        return Results.Ok(presenter.ViewModel);
    }

    private static IResult RecentPosts(HttpRequest httpRequest, int? limit, ISessionGateway sessions, IPostGateway posts, IUserGateway users, LinkGenerator links)
    {
        var reader = ReadAuthenticatedUser(httpRequest, sessions);
        if (reader is null)
        {
            return Results.Unauthorized();
        }

        var presenter = new ScrollPostsPresenter();
        var controller = new ScrollPostsController(new ScrollPostsInteractor(posts, presenter));
        controller.Scroll(reader.Handle, Math.Clamp(limit ?? 20, 1, 100));
        return Results.Ok(ToHttpResponse(presenter.ViewModel!, users, httpRequest.HttpContext, links));
    }

    private static IResult SearchPosts(HttpRequest httpRequest, string query, ISessionGateway sessions, IPostGateway posts, IPostSearchGateway search, IUserGateway users, LinkGenerator links)
    {
        var reader = ReadAuthenticatedUser(httpRequest, sessions);
        if (reader is null)
        {
            return Results.Unauthorized();
        }

        var presenter = new SearchPostsPresenter();
        new SearchPostsInteractor(search, presenter, posts).Handle(new(query, reader.Handle));
        return Results.Ok(ToHttpResponse(presenter.ViewModel!, users, httpRequest.HttpContext, links));
    }

    private static SocialApp.User.Entities.UserAccount? ReadAuthenticatedUser(HttpRequest request, ISessionGateway sessions)
    {
        var token = ReadBearerToken(request);
        return string.IsNullOrWhiteSpace(token) ? null : sessions.FindByToken(token);
    }

    private static string? ReadBearerToken(HttpRequest request)
    {
        var authorization = request.Headers.Authorization.ToString();
        const string bearerPrefix = "Bearer ";
        return authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? authorization[bearerPrefix.Length..].Trim()
            : null;
    }

    private sealed record ProfileImageHttpResult(
        Guid AssetId,
        string StorageKey,
        string ContentType,
        long ByteLength,
        int? Width,
        int? Height,
        DateTimeOffset UploadedAt,
        string ImageUrl);

    private sealed record ProfileImageUploadHttpResult(
        bool Succeeded,
        string Message,
        ProfileImageHttpResult? ProfileImage);

    private sealed record UserProfileHttpResult(
        bool Succeeded,
        string Message,
        string? Handle,
        string? DisplayName,
        ProfileImageHttpResult? ProfileImage,
        IReadOnlyList<UserPostSummaryHttpResult> Posts);

    private static PostsHttpResult ToHttpResponse(ScrollPostsViewModel viewModel, IUserGateway users, HttpContext httpContext, LinkGenerator links) =>
        new(viewModel.Posts.Select(post => ToHttpPostSummary(post, users, httpContext, links)).ToArray());

    private static PostsHttpResult ToHttpResponse(SearchPostsViewModel viewModel, IUserGateway users, HttpContext httpContext, LinkGenerator links) =>
        new(viewModel.Posts.Select(post => ToHttpPostSummary(post, users, httpContext, links)).ToArray());

    private static IndividualPostHttpResult ToHttpResponse(DisplayPostViewModel viewModel, IUserGateway users, HttpContext httpContext, LinkGenerator links) =>
        new(viewModel.Succeeded, viewModel.Message, viewModel.Post is null ? null : ToHttpPostSummary(viewModel.Post, users, httpContext, links));

    private static PostSummaryHttpResult ToHttpPostSummary(PostSummaryViewModel post, IUserGateway users, HttpContext httpContext, LinkGenerator links) =>
        new(
            post.Id,
            post.AuthorHandle,
            ProfileImageUrlFor(users, post.AuthorHandle, httpContext, links),
            post.ContentSegments.Select(s => new PostContentSegmentHttpResult(s.Sequence, s.Text, s.MentionHandle, s.HashtagText)).ToArray(),
            post.ParentPostId,
            post.OriginalPostId,
            post.CreatedAt,
            post.LikeCount,
            post.LikedByCurrentReader,
            post.RepostCount,
            post.RepostedByCurrentReader,
            post.ReplyCount,
            post.RecentReplies.Select(reply => ToHttpPostSummary(reply, users, httpContext, links)).ToArray(),
            post.ReplyTarget is null ? null : ToHttpQuotedPostSummary(post.ReplyTarget, users, httpContext, links),
            post.QuotedPost is null ? null : ToHttpQuotedPostSummary(post.QuotedPost, users, httpContext, links),
            post.Media?.Select(ToHttpPostMedia).ToArray() ?? Array.Empty<PostMediaSummaryHttpResult>());

    private static UserPostSummaryHttpResult ToHttpUserPostSummary(ViewUserPostSummaryViewModel post, IUserGateway users, HttpContext httpContext, LinkGenerator links) =>
        new(
            post.Id,
            post.AuthorHandle,
            ProfileImageUrlFor(users, post.AuthorHandle, httpContext, links),
            post.ContentSegments.Select(s => new PostContentSegmentHttpResult(s.Sequence, s.Text, s.MentionHandle, s.HashtagText)).ToArray(),
            post.ParentPostId,
            post.OriginalPostId,
            post.CreatedAt,
            post.LikeCount,
            post.LikedByCurrentReader,
            post.RepostCount,
            post.RepostedByCurrentReader,
            post.QuotedPost is null ? null : ToHttpUserQuotedPostSummary(post.QuotedPost, users, httpContext, links),
            post.Media?.Select(ToHttpUserPostMedia).ToArray() ?? Array.Empty<PostMediaSummaryHttpResult>());

    private static QuotedPostSummaryHttpResult ToHttpQuotedPostSummary(QuotedPostSummaryViewModel post, IUserGateway users, HttpContext httpContext, LinkGenerator links) =>
        new(
            post.Id,
            post.AuthorHandle,
            ProfileImageUrlFor(users, post.AuthorHandle, httpContext, links),
            post.Content,
            post.CreatedAt,
            post.Media?.Select(ToHttpPostMedia).ToArray());

    private static QuotedPostSummaryHttpResult ToHttpUserQuotedPostSummary(ViewUserQuotedPostSummaryViewModel post, IUserGateway users, HttpContext httpContext, LinkGenerator links) =>
        new(
            post.Id,
            post.AuthorHandle,
            ProfileImageUrlFor(users, post.AuthorHandle, httpContext, links),
            post.Content,
            post.CreatedAt,
            post.Media?.Select(ToHttpUserPostMedia).ToArray());

    private static string? ProfileImageUrlFor(IUserGateway users, string authorHandle, HttpContext httpContext, LinkGenerator links)
    {
        var profileImage = users.FindByHandle(authorHandle)?.ProfileImage;
        return profileImage is null
            ? null
            : links.GetPathByName(httpContext, GetProfileImageRouteName, new { assetId = profileImage.AssetId })
                ?? throw new InvalidOperationException("Profile image route is not registered.");
    }

    private static PostMediaSummaryHttpResult ToHttpPostMedia(PostMediaSummaryViewModel media) =>
        new(
            media.AssetId,
            media.Kind,
            media.ContentType,
            media.ByteLength,
            media.Width,
            media.Height,
            media.DurationMs,
            media.SortOrder,
            media.ThumbnailKey,
            media.AltText,
            media.MediaUrl ?? $"/api/post-media/{media.AssetId}");

    private static PostMediaSummaryHttpResult ToHttpUserPostMedia(ViewUserPostMediaSummaryViewModel media) =>
        new(
            media.AssetId,
            media.Kind,
            media.ContentType,
            media.ByteLength,
            media.Width,
            media.Height,
            media.DurationMs,
            media.SortOrder,
            media.ThumbnailKey,
            media.AltText,
            media.MediaUrl ?? $"/api/post-media/{media.AssetId}");

    private sealed record PostContentSegmentHttpResult(int Sequence, string Text, string? MentionHandle, string? HashtagText = null);
    private sealed record PostsHttpResult(IReadOnlyList<PostSummaryHttpResult> Posts);
    private sealed record IndividualPostHttpResult(bool Succeeded, string Message, PostSummaryHttpResult? Post);
    private sealed record QuotedPostSummaryHttpResult(Guid Id, string AuthorHandle, string? AuthorProfileImageUrl, string Content, DateTimeOffset CreatedAt, IReadOnlyList<PostMediaSummaryHttpResult>? Media = null);
    private sealed record PostMediaSummaryHttpResult(Guid AssetId, string Kind, string ContentType, long ByteLength, int? Width, int? Height, long? DurationMs, int SortOrder, string? ThumbnailKey, string? AltText, string? MediaUrl);
    private sealed record PostSummaryHttpResult(Guid Id, string AuthorHandle, string? AuthorProfileImageUrl, IReadOnlyList<PostContentSegmentHttpResult> ContentSegments, Guid? ParentPostId, Guid? OriginalPostId, DateTimeOffset CreatedAt, int LikeCount, bool LikedByCurrentReader, int RepostCount, bool RepostedByCurrentReader, int ReplyCount, IReadOnlyList<PostSummaryHttpResult> RecentReplies, QuotedPostSummaryHttpResult? ReplyTarget, QuotedPostSummaryHttpResult? QuotedPost, IReadOnlyList<PostMediaSummaryHttpResult> Media);
    private sealed record UserPostSummaryHttpResult(Guid Id, string AuthorHandle, string? AuthorProfileImageUrl, IReadOnlyList<PostContentSegmentHttpResult> ContentSegments, Guid? ParentPostId, Guid? OriginalPostId, DateTimeOffset CreatedAt, int LikeCount, bool LikedByCurrentReader, int RepostCount, bool RepostedByCurrentReader, QuotedPostSummaryHttpResult? QuotedPost, IReadOnlyList<PostMediaSummaryHttpResult> Media);
}
