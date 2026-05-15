using SocialApp.User.ResponseModels;
using SocialApp.User.UseCases;
using SocialApp.User.ViewModels;

namespace SocialApp.User.Presenters;

public sealed class CreateAccountPresenter : ICreateAccountOutputBoundary
{
    public CreateAccountViewModel? ViewModel { get; private set; }
    public void Present(CreateAccountResponse response) => ViewModel = new(response.Succeeded, UserMessages.For(response.MessageKey), response.Handle, response.SessionToken);
}

public sealed class RegisterAccountPresenter : IRegisterAccountOutputBoundary
{
    public RegisterAccountViewModel? ViewModel { get; private set; }
    public void Present(RegisterAccountResponse response) => ViewModel = new(response.Succeeded, UserMessages.For(response.MessageKey));
}

public sealed class VerifyRegistrationPresenter : IVerifyRegistrationOutputBoundary
{
    public VerifyRegistrationViewModel? ViewModel { get; private set; }
    public void Present(VerifyRegistrationResponse response) => ViewModel = new(response.Succeeded, UserMessages.For(response.MessageKey));
}

public sealed class LoginPresenter : ILoginOutputBoundary
{
    public LoginViewModel? ViewModel { get; private set; }
    public void Present(LoginResponse response) => ViewModel = new(response.Succeeded, UserMessages.For(response.MessageKey), response.Handle, response.SessionToken);
}

public sealed class LoginWithDevicePresenter : ILoginWithDeviceOutputBoundary
{
    public LoginWithDeviceViewModel? ViewModel { get; private set; }
    public void Present(LoginWithDeviceResponse response) => ViewModel = new(response.Succeeded, UserMessages.For(response.MessageKey), response.Handle, response.SessionToken, response.OtpRequired);
}

public sealed class VerifyDeviceOtpPresenter : IVerifyDeviceOtpOutputBoundary
{
    public VerifyDeviceOtpViewModel? ViewModel { get; private set; }
    public void Present(VerifyDeviceOtpResponse response) => ViewModel = new(response.Succeeded, UserMessages.For(response.MessageKey), response.Handle, response.SessionToken);
}

public sealed class ForgotPasswordPresenter : IForgotPasswordOutputBoundary
{
    public ForgotPasswordViewModel? ViewModel { get; private set; }
    public void Present(ForgotPasswordResponse response) => ViewModel = new(response.Succeeded, UserMessages.For(response.MessageKey), response.ResetToken);
}

public sealed class ChangePasswordPresenter : IChangePasswordOutputBoundary
{
    public ChangePasswordViewModel? ViewModel { get; private set; }
    public void Present(ChangePasswordResponse response) => ViewModel = new(response.Succeeded, UserMessages.For(response.MessageKey));
}

public sealed class RequestPasswordResetPresenter : IRequestPasswordResetOutputBoundary
{
    public RequestPasswordResetViewModel? ViewModel { get; private set; }
    public void Present(RequestPasswordResetResponse response) => ViewModel = new(response.Succeeded, UserMessages.For(response.MessageKey));
}

public sealed class ResetPasswordPresenter : IResetPasswordOutputBoundary
{
    public ResetPasswordViewModel? ViewModel { get; private set; }
    public void Present(ResetPasswordResponse response) => ViewModel = new(response.Succeeded, UserMessages.For(response.MessageKey));
}

public sealed class SearchUserPresenter : ISearchUserOutputBoundary
{
    public SearchUserViewModel? ViewModel { get; private set; }
    public void Present(SearchUserResponse response) => ViewModel = new(response.Users.Select(u => new UserSummaryViewModel(u.Handle, u.DisplayName)).ToArray());
}

public sealed class BeginProfileImageUploadPresenter : IBeginProfileImageUploadOutputBoundary
{
    public BeginProfileImageUploadViewModel? ViewModel { get; private set; }
    public void Present(BeginProfileImageUploadResponse response) =>
        ViewModel = new(response.Succeeded, UserMessages.For(response.MessageKey), response.Upload?.AssetId, response.Upload?.UploadUrl);
}

public sealed class CompleteProfileImageUploadPresenter : ICompleteProfileImageUploadOutputBoundary
{
    public CompleteProfileImageUploadViewModel? ViewModel { get; private set; }
    public void Present(CompleteProfileImageUploadResponse response) =>
        ViewModel = new(response.Succeeded, UserMessages.For(response.MessageKey), UserPresenterMapping.ToProfileImageViewModel(response.ProfileImage));
}

public sealed class RemoveProfileImagePresenter : IRemoveProfileImageOutputBoundary
{
    public RemoveProfileImageViewModel? ViewModel { get; private set; }
    public void Present(RemoveProfileImageResponse response) => ViewModel = new(response.Succeeded, UserMessages.For(response.MessageKey));
}

public sealed class ViewUserPresenter : IViewUserOutputBoundary
{
    public ViewUserViewModel? ViewModel { get; private set; }
    public void Present(ViewUserResponse response) =>
        ViewModel = new(
            response.Succeeded,
            UserMessages.For(response.MessageKey),
            response.Handle,
            response.DisplayName,
            UserPresenterMapping.ToProfileImageViewModel(response.ProfileImage),
            response.Posts.Select(ToViewModel).ToArray());

    private static ViewUserPostSummaryViewModel ToViewModel(ViewUserPostSummaryResponse post) =>
        new(
            post.Id,
            post.AuthorHandle,
            post.Content,
            post.ParentPostId,
            post.OriginalPostId,
            post.CreatedAt,
            post.LikeCount,
            post.LikedByCurrentReader,
            post.RepostCount,
            post.RepostedByCurrentReader,
            post.QuotedPost is null
                ? null
                : new ViewUserQuotedPostSummaryViewModel(post.QuotedPost.Id, post.QuotedPost.AuthorHandle, post.QuotedPost.Content, post.QuotedPost.CreatedAt),
            post.Media?.Select(m => new ViewUserPostMediaSummaryViewModel(
                m.AssetId,
                m.Kind,
                m.ContentType,
                m.ByteLength,
                m.Width,
                m.Height,
                m.DurationMs,
                m.SortOrder,
                m.ThumbnailKey,
                m.AltText,
                m.MediaUrl)).ToArray() ?? Array.Empty<ViewUserPostMediaSummaryViewModel>());
}

internal static class UserPresenterMapping
{
    public static ProfileImageViewModel? ToProfileImageViewModel(ProfileImageResponse? image) =>
        image is null
            ? null
            : new ProfileImageViewModel(
                image.AssetId,
                image.StorageKey,
                image.ContentType,
                image.ByteLength,
                image.Width,
                image.Height,
                image.UploadedAt);
}

internal static class UserMessages
{
    private static readonly IReadOnlyDictionary<string, string> Messages = new Dictionary<string, string>
    {
        [UserMessageKeys.AccountCreated] = "Account created.",
        [UserMessageKeys.AccountAlreadyExists] = "Account already exists.",
        [UserMessageKeys.VerificationCodeSent] = "Verification code sent.",
        [UserMessageKeys.VerificationCodeInvalid] = "Verification code is invalid.",
        [UserMessageKeys.AccountVerified] = "Account verified.",
        [UserMessageKeys.InvalidCredentials] = "Invalid email or password.",
        [UserMessageKeys.LoggedIn] = "Logged in.",
        [UserMessageKeys.OneTimeCodeSent] = "One-time code sent.",
        [UserMessageKeys.OneTimeCodeInvalid] = "One-time code is invalid.",
        [UserMessageKeys.AccountNotFound] = "Account not found.",
        [UserMessageKeys.PasswordResetRequested] = "Password reset requested.",
        [UserMessageKeys.ResetTokenInvalid] = "Reset token is invalid.",
        [UserMessageKeys.PasswordChanged] = "Password changed.",
        [UserMessageKeys.PasswordResetLinkSentIfAccountExists] = "If the account exists, a reset link was sent.",
        [UserMessageKeys.ResetLinkInvalidOrExpired] = "Reset link is invalid or expired.",
        [UserMessageKeys.UserNotFound] = "User not found.",
        [UserMessageKeys.UserFound] = "User found.",
        [UserMessageKeys.ProfileImageUploadReserved] = "Profile image upload reserved.",
        [UserMessageKeys.ProfileImageUploadCompleted] = "Profile image upload completed.",
        [UserMessageKeys.ProfileImageUploadNotFound] = "Profile image upload not found.",
        [UserMessageKeys.ProfileImageRemoved] = "Profile image removed."
    };

    public static string For(string key) => Messages.TryGetValue(key, out var message) ? message : key;
}
