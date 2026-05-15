using SocialApp.User.Entities;
using SocialApp.User.Gateways;
using SocialApp.User.RequestModels;
using SocialApp.User.ResponseModels;

namespace SocialApp.User.UseCases;

public sealed class CreateAccountInteractor(IUserGateway users, ISessionGateway sessions, IPasswordGateway passwords, ICreateAccountOutputBoundary output) : ICreateAccountInputBoundary
{
    public void Handle(CreateAccountRequest request)
    {
        var user = UserAccount.CreateWithPasswordHash(request.DisplayName, request.Handle, request.Email, passwords.Hash(request.Password));
        users.Save(user);
        var token = sessions.CreateSession(user);
        output.Present(new CreateAccountResponse(true, UserMessageKeys.AccountCreated, user.Handle, token));
    }
}

public sealed class RegisterAccountInteractor(
    IUserGateway users,
    IPendingRegistrationGateway registrations,
    IVerificationCodeGateway codes,
    IEmailGateway email,
    IPasswordGateway passwords,
    IRegisterAccountOutputBoundary output) : IRegisterAccountInputBoundary
{
    public void Handle(RegisterAccountRequest request)
    {
        if (users.FindByHandle(request.Handle) is not null || users.FindByEmail(request.Email) is not null)
        {
            output.Present(new RegisterAccountResponse(false, UserMessageKeys.AccountAlreadyExists));
            return;
        }

        var pendingAccount = UserAccount.CreateWithPasswordHash(request.DisplayName, request.Handle, request.Email, passwords.Hash(request.Password));
        registrations.Save(new PendingRegistration(pendingAccount.DisplayName, pendingAccount.Handle, pendingAccount.Email, pendingAccount.PasswordHash));
        var code = codes.CreateCode(request.Email, "registration", TimeSpan.FromMinutes(15));
        email.Send(request.Email, "Verify your SocialApp account", $"Your SocialApp verification code is {code}.");
        output.Present(new RegisterAccountResponse(true, UserMessageKeys.VerificationCodeSent));
    }
}

public sealed class VerifyRegistrationInteractor(
    IUserGateway users,
    IPendingRegistrationGateway registrations,
    IVerificationCodeGateway codes,
    IPasswordGateway passwords,
    IVerifyRegistrationOutputBoundary output) : IVerifyRegistrationInputBoundary
{
    public void Handle(VerifyRegistrationRequest request)
    {
        var registration = registrations.FindByEmail(request.Email);
        if (registration is null || !codes.Verify(request.Email, "registration", request.Code))
        {
            output.Present(new VerifyRegistrationResponse(false, UserMessageKeys.VerificationCodeInvalid));
            return;
        }

        users.Save(UserAccount.CreateWithPasswordHash(registration.DisplayName, registration.Handle, registration.Email, passwords.NormalizeStoredPassword(registration.PasswordHash)));
        registrations.Remove(request.Email);
        output.Present(new VerifyRegistrationResponse(true, UserMessageKeys.AccountVerified));
    }
}

public sealed class LoginInteractor(IUserGateway users, ISessionGateway sessions, IPasswordGateway passwords, ILoginOutputBoundary output) : ILoginInputBoundary
{
    public void Handle(LoginRequest request)
    {
        var user = users.FindByEmail(request.Email);
        if (user is null || !passwords.Verify(request.Password, user.PasswordHash))
        {
            output.Present(new LoginResponse(false, UserMessageKeys.InvalidCredentials, null, null));
            return;
        }

        output.Present(new LoginResponse(true, UserMessageKeys.LoggedIn, user.Handle, sessions.CreateSession(user)));
    }
}

public sealed class LoginWithDeviceInteractor(
    IUserGateway users,
    ISessionGateway sessions,
    IRememberedDeviceGateway devices,
    IVerificationCodeGateway codes,
    IEmailGateway email,
    IPasswordGateway passwords,
    ILoginWithDeviceOutputBoundary output) : ILoginWithDeviceInputBoundary
{
    public void Handle(LoginWithDeviceRequest request)
    {
        var user = users.FindByEmail(request.Email);
        if (user is null || !passwords.Verify(request.Password, user.PasswordHash))
        {
            output.Present(new LoginWithDeviceResponse(false, UserMessageKeys.InvalidCredentials, null, null, false));
            return;
        }

        if (devices.IsRemembered(user.Handle, request.DeviceId))
        {
            output.Present(new LoginWithDeviceResponse(true, UserMessageKeys.LoggedIn, user.Handle, sessions.CreateSession(user), false));
            return;
        }

        var code = codes.CreateCode(user.Email, "device", TimeSpan.FromMinutes(10));
        email.Send(user.Email, "Your SocialApp login code", $"Your SocialApp login code is {code}.");
        output.Present(new LoginWithDeviceResponse(true, UserMessageKeys.OneTimeCodeSent, user.Handle, null, true));
    }
}

public sealed class VerifyDeviceOtpInteractor(
    IUserGateway users,
    ISessionGateway sessions,
    IRememberedDeviceGateway devices,
    IVerificationCodeGateway codes,
    IVerifyDeviceOtpOutputBoundary output) : IVerifyDeviceOtpInputBoundary
{
    public void Handle(VerifyDeviceOtpRequest request)
    {
        var user = users.FindByHandle(request.Handle);
        if (user is null || !codes.Verify(user.Email, "device", request.Code))
        {
            output.Present(new VerifyDeviceOtpResponse(false, UserMessageKeys.OneTimeCodeInvalid, null, null));
            return;
        }

        if (request.RememberDevice)
        {
            devices.Remember(user.Handle, request.DeviceId);
        }

        output.Present(new VerifyDeviceOtpResponse(true, UserMessageKeys.LoggedIn, user.Handle, sessions.CreateSession(user)));
    }
}

public sealed class ForgotPasswordInteractor(IUserGateway users, IPasswordResetGateway resets, IForgotPasswordOutputBoundary output) : IForgotPasswordInputBoundary
{
    public void Handle(ForgotPasswordRequest request)
    {
        var user = users.FindByEmail(request.Email);
        if (user is null)
        {
            output.Present(new ForgotPasswordResponse(false, UserMessageKeys.AccountNotFound, null));
            return;
        }

        output.Present(new ForgotPasswordResponse(true, UserMessageKeys.PasswordResetRequested, resets.CreateResetToken(user)));
    }
}

public sealed class ChangePasswordInteractor(IUserGateway users, IPasswordResetGateway resets, IPasswordGateway passwords, IChangePasswordOutputBoundary output) : IChangePasswordInputBoundary
{
    public void Handle(ChangePasswordRequest request)
    {
        var user = resets.FindByToken(request.ResetToken);
        if (user is null)
        {
            output.Present(new ChangePasswordResponse(false, UserMessageKeys.ResetTokenInvalid));
            return;
        }

        if (users.FindByHandle(user.Handle) is null)
        {
            output.Present(new ChangePasswordResponse(false, UserMessageKeys.AccountNotFound));
            return;
        }

        user.ChangePasswordHash(passwords.Hash(request.NewPassword));
        users.Save(user);
        resets.Consume(request.ResetToken);
        output.Present(new ChangePasswordResponse(true, UserMessageKeys.PasswordChanged));
    }
}

public sealed class RequestPasswordResetInteractor(
    IUserGateway users,
    IPasswordResetTokenGateway resets,
    IEmailGateway email,
    IClock clock,
    IRequestPasswordResetOutputBoundary output) : IRequestPasswordResetInputBoundary
{
    public void Handle(RequestPasswordResetRequest request)
    {
        var user = users.FindByEmail(request.Email);
        if (user is not null)
        {
            var token = resets.CreateToken(user.Email, TimeSpan.FromMinutes(5));
            var resetUrl = $"{request.ResetBaseUrl.TrimEnd('/')}?token={Uri.EscapeDataString(token)}";
            email.Send(user.Email, "Reset your SocialApp password", $"Use this one-time link within 5 minutes: {resetUrl}");
        }

        _ = clock.UtcNow;
        output.Present(new RequestPasswordResetResponse(true, UserMessageKeys.PasswordResetLinkSentIfAccountExists));
    }
}

public sealed class ResetPasswordInteractor(
    IUserGateway users,
    IPasswordResetTokenGateway resets,
    IClock clock,
    IPasswordGateway passwords,
    IResetPasswordOutputBoundary output) : IResetPasswordInputBoundary
{
    public void Handle(ResetPasswordRequest request)
    {
        _ = clock.UtcNow;
        var token = resets.Consume(request.ResetToken);
        if (token is null)
        {
            output.Present(new ResetPasswordResponse(false, UserMessageKeys.ResetLinkInvalidOrExpired));
            return;
        }

        var user = users.FindByEmail(token.Email);
        if (user is null)
        {
            output.Present(new ResetPasswordResponse(false, UserMessageKeys.AccountNotFound));
            return;
        }

        user.ChangePasswordHash(passwords.Hash(request.NewPassword));
        users.Save(user);
        output.Present(new ResetPasswordResponse(true, UserMessageKeys.PasswordChanged));
    }
}

public sealed class SearchUserInteractor(IUserGateway users, ISearchUserOutputBoundary output) : ISearchUserInputBoundary
{
    public void Handle(SearchUserRequest request)
    {
        output.Present(new SearchUserResponse(users.Search(request.Query)
            .Select(u => new UserSummaryResponse(u.Handle, u.DisplayName))
            .ToArray()));
    }
}

public sealed class BeginProfileImageUploadInteractor(IProfileImageStorageGateway storage, IBeginProfileImageUploadOutputBoundary output) : IBeginProfileImageUploadInputBoundary
{
    public void Handle(BeginProfileImageUploadRequest request)
    {
        var reservation = storage.ReserveUpload(new(request.OwnerHandle, request.ContentType, request.ByteLength));
        output.Present(new(
            true,
            UserMessageKeys.ProfileImageUploadReserved,
            new(reservation.AssetId, reservation.StorageKey, reservation.UploadUrl)));
    }
}

public sealed class CompleteProfileImageUploadInteractor(
    IUserGateway users,
    IProfileImageStorageGateway storage,
    IClock clock,
    ICompleteProfileImageUploadOutputBoundary output) : ICompleteProfileImageUploadInputBoundary
{
    public void Handle(CompleteProfileImageUploadRequest request)
    {
        EnsureOwnProfile(request.CurrentUserHandle, request.ProfileHandle);

        var user = users.FindByHandle(request.ProfileHandle);
        if (user is null)
        {
            output.Present(new(false, UserMessageKeys.UserNotFound, null));
            return;
        }

        var upload = storage.CompleteUpload(new(request.AssetId, request.CurrentUserHandle));
        if (upload is null)
        {
            output.Present(new(false, UserMessageKeys.ProfileImageUploadNotFound, null));
            return;
        }

        var profileImage = new ProfileImage(
            upload.AssetId,
            upload.StorageKey,
            upload.ContentType,
            upload.ByteLength,
            request.Width,
            request.Height,
            clock.UtcNow);
        user.SetProfileImage(profileImage);
        users.Save(user);
        output.Present(new(true, UserMessageKeys.ProfileImageUploadCompleted, UserResponseMapping.ToProfileImageResponse(profileImage)));
    }

    private static void EnsureOwnProfile(string currentUserHandle, string profileHandle)
    {
        if (!string.Equals(currentUserHandle, profileHandle, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Users can edit only their own profile image.");
        }
    }
}

public sealed class RemoveProfileImageInteractor(IUserGateway users, IRemoveProfileImageOutputBoundary output) : IRemoveProfileImageInputBoundary
{
    public void Handle(RemoveProfileImageRequest request)
    {
        if (!string.Equals(request.CurrentUserHandle, request.ProfileHandle, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Users can edit only their own profile image.");
        }

        var user = users.FindByHandle(request.ProfileHandle);
        if (user is null)
        {
            output.Present(new(false, UserMessageKeys.UserNotFound));
            return;
        }

        user.RemoveProfileImage();
        users.Save(user);
        output.Present(new(true, UserMessageKeys.ProfileImageRemoved));
    }
}

public sealed class ViewUserInteractor(IUserGateway users, IUserProfilePostGateway posts, IViewUserOutputBoundary output) : IViewUserInputBoundary
{
    public void Handle(ViewUserRequest request)
    {
        var user = users.FindByHandle(request.Handle);
        output.Present(user is null
            ? new ViewUserResponse(false, UserMessageKeys.UserNotFound, null, null, null, Array.Empty<ViewUserPostSummaryResponse>())
            : new ViewUserResponse(
                true,
                UserMessageKeys.UserFound,
                user.Handle,
                user.DisplayName,
                user.ProfileImage is null ? null : UserResponseMapping.ToProfileImageResponse(user.ProfileImage),
                posts.RecentPostsByAuthor(user.Handle, request.ReaderHandle, request.RecentPostLimit)
                    .Select(ToResponse)
                    .ToArray()));
    }

    private static ViewUserPostSummaryResponse ToResponse(UserProfilePostSummary post) =>
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
                : new ViewUserQuotedPostSummaryResponse(post.QuotedPost.Id, post.QuotedPost.AuthorHandle, post.QuotedPost.Content, post.QuotedPost.CreatedAt),
            post.Media?.Select(m => new ViewUserPostMediaSummaryResponse(
                m.AssetId,
                m.Kind,
                m.ContentType,
                m.ByteLength,
                m.Width,
                m.Height,
                m.DurationMs,
                m.SortOrder,
                m.ThumbnailKey,
                m.AltText)).ToArray() ?? Array.Empty<ViewUserPostMediaSummaryResponse>());
}

internal static class UserResponseMapping
{
    public static ProfileImageResponse ToProfileImageResponse(ProfileImage image) =>
        new(
            image.AssetId,
            image.StorageKey,
            image.ContentType,
            image.ByteLength,
            image.Width,
            image.Height,
            image.UploadedAt);
}
