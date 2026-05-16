using FluentAssertions;
using SocialApp.User.Controllers;
using SocialApp.User.Entities;
using SocialApp.User.Gateways;
using SocialApp.User.Presenters;
using SocialApp.User.ResponseModels;
using SocialApp.User.UseCases;
using Xunit;

namespace SocialApp.User.Tests;

public sealed class UserComponentTests
{
    [Fact]
    public void User_account_requires_valid_handle_and_credentials()
    {
        var account = UserAccount.CreateWithCredentials("Ada Lovelace", "@ada", "ada@example.com", "Correct9!");

        account.Handle.Should().Be("@ada");
        account.Credentials.Should().Be("Correct9!");
        Action invalidCredentials = () => UserAccount.CreateWithCredentials("Ada Lovelace", "@ada", "ada@example.com", "");
        invalidCredentials.Should().Throw<ArgumentException>().WithMessage("*Credentials*");
    }

    [Fact]
    public void User_account_can_be_rehydrated_from_persistence()
    {
        var id = Guid.NewGuid();
        var account = UserAccount.Rehydrate(id, "Ada Lovelace", "@ada", "ada@example.com", "opaque-credentials");

        account.Id.Should().Be(id);
        account.Handle.Should().Be("@ada");
        account.Credentials.Should().Be("opaque-credentials");
    }

    [Fact]
    public void Credentials_can_be_rehydrated_without_rewriting()
    {
        var created = UserAccount.CreateWithCredentials("Ada Lovelace", "@ada", "ada@example.com", "Correct9!");
        var storedCredentials = created.Credentials;

        var account = UserAccount.Rehydrate(Guid.NewGuid(), "Ada Lovelace", "@ada", "ada@example.com", storedCredentials);

        account.Credentials.Should().Be(storedCredentials);
    }

    [Fact]
    public void User_account_can_change_credentials()
    {
        var account = UserAccount.CreateWithCredentials("Ada Lovelace", "@ada", "ada@example.com", "Correct9!");

        account.ChangeCredentials("Changed9!");

        account.Credentials.Should().Be("Changed9!");
    }

    [Fact]
    public void User_account_can_set_replace_and_remove_profile_image_metadata()
    {
        var account = UserAccount.CreateWithCredentials("Ada Lovelace", "@ada", "ada@example.com", "Correct9!");
        var firstImage = new ProfileImage(
            Guid.NewGuid(),
            "profile-images/ada/first.jpg",
            "image/jpeg",
            12_345,
            800,
            600,
            new DateTimeOffset(2026, 5, 11, 21, 0, 0, TimeSpan.Zero));
        var replacementImage = new ProfileImage(
            Guid.NewGuid(),
            "profile-images/ada/replacement.png",
            "image/png",
            23_456,
            null,
            null,
            new DateTimeOffset(2026, 5, 11, 21, 5, 0, TimeSpan.Zero));

        account.ProfileImage.Should().BeNull();

        account.SetProfileImage(firstImage);
        account.ProfileImage.Should().Be(firstImage);

        account.SetProfileImage(replacementImage);
        account.ProfileImage.Should().Be(replacementImage);

        account.RemoveProfileImage();
        account.ProfileImage.Should().BeNull();
    }

    [Fact]
    public void User_account_rehydration_preserves_profile_image_metadata()
    {
        var profileImage = new ProfileImage(
            Guid.NewGuid(),
            "profile-images/ada/avatar.jpg",
            "image/jpeg",
            12_345,
            800,
            600,
            new DateTimeOffset(2026, 5, 11, 21, 0, 0, TimeSpan.Zero));

        var account = UserAccount.Rehydrate(
            Guid.NewGuid(),
            "Ada Lovelace",
            "@ada",
            "ada@example.com",
            "Correct9!",
            profileImage);

        account.ProfileImage.Should().Be(profileImage);
    }

    [Fact]
    public void Profile_image_upload_flow_reserves_and_completes_owned_image_assets()
    {
        var users = new InMemoryUserGateway();
        users.Save(UserAccount.CreateWithCredentials("Ada Lovelace", "@ada", "ada@example.com", "Correct9!"));
        var storage = new InMemoryProfileImageStorageGateway();

        var beginPresenter = new BeginProfileImageUploadPresenter();
        new BeginProfileImageUploadController(new BeginProfileImageUploadInteractor(storage, beginPresenter))
            .Begin("@ada", "image/jpeg", 12_345);

        beginPresenter.ViewModel!.Succeeded.Should().BeTrue();
        beginPresenter.ViewModel.AssetId.Should().NotBe(Guid.Empty);
        beginPresenter.ViewModel.UploadUrl.Should().NotBeNullOrWhiteSpace();

        var completePresenter = new CompleteProfileImageUploadPresenter();
        new CompleteProfileImageUploadController(new CompleteProfileImageUploadInteractor(users, storage, new MutableClock(new DateTimeOffset(2026, 5, 11, 22, 0, 0, TimeSpan.Zero)), completePresenter))
            .Complete("@ada", "@ada", beginPresenter.ViewModel.AssetId!.Value, 800, 600);

        completePresenter.ViewModel!.Succeeded.Should().BeTrue();
        users.FindByHandle("@ada")!.ProfileImage.Should().NotBeNull();
        users.FindByHandle("@ada")!.ProfileImage!.ContentType.Should().Be("image/jpeg");
        users.FindByHandle("@ada")!.ProfileImage!.Width.Should().Be(800);
    }

    [Fact]
    public void Profile_image_upload_rejects_non_image_content_types()
    {
        var storage = new InMemoryProfileImageStorageGateway();
        var presenter = new BeginProfileImageUploadPresenter();

        Action reserveVideo = () => new BeginProfileImageUploadController(new BeginProfileImageUploadInteractor(storage, presenter))
            .Begin("@ada", "video/mp4", 12_345);

        reserveVideo.Should().Throw<ArgumentException>().WithMessage("*image*");
    }

    [Fact]
    public void Profile_image_completion_and_removal_require_the_current_owner()
    {
        var users = new InMemoryUserGateway();
        users.Save(UserAccount.CreateWithCredentials("Ada Lovelace", "@ada", "ada@example.com", "Correct9!"));
        users.Save(UserAccount.CreateWithCredentials("Grace Hopper", "@grace", "grace@example.com", "NavyCode9!"));
        var storage = new InMemoryProfileImageStorageGateway();
        var beginPresenter = new BeginProfileImageUploadPresenter();
        new BeginProfileImageUploadController(new BeginProfileImageUploadInteractor(storage, beginPresenter))
            .Begin("@ada", "image/png", 10_000);

        Action completeAsGrace = () => new CompleteProfileImageUploadController(new CompleteProfileImageUploadInteractor(users, storage, new MutableClock(DateTimeOffset.UtcNow), new CompleteProfileImageUploadPresenter()))
            .Complete("@grace", "@ada", beginPresenter.ViewModel!.AssetId!.Value, null, null);

        completeAsGrace.Should().Throw<InvalidOperationException>().WithMessage("*own profile image*");

        var completePresenter = new CompleteProfileImageUploadPresenter();
        new CompleteProfileImageUploadController(new CompleteProfileImageUploadInteractor(users, storage, new MutableClock(DateTimeOffset.UtcNow), completePresenter))
            .Complete("@ada", "@ada", beginPresenter.ViewModel!.AssetId!.Value, null, null);
        completePresenter.ViewModel!.Succeeded.Should().BeTrue();

        Action removeAsGrace = () => new RemoveProfileImageController(new RemoveProfileImageInteractor(users, new RemoveProfileImagePresenter()))
            .Remove("@grace", "@ada");

        removeAsGrace.Should().Throw<InvalidOperationException>().WithMessage("*own profile image*");
        users.FindByHandle("@ada")!.ProfileImage.Should().NotBeNull();
    }

    [Fact]
    public void Remove_profile_image_clears_metadata_for_the_owner()
    {
        var users = new InMemoryUserGateway();
        var account = UserAccount.CreateWithCredentials("Ada Lovelace", "@ada", "ada@example.com", "Correct9!");
        account.SetProfileImage(new ProfileImage(Guid.NewGuid(), "profile-images/ada/avatar.jpg", "image/jpeg", 12_345, 800, 600, DateTimeOffset.UtcNow));
        users.Save(account);

        var presenter = new RemoveProfileImagePresenter();
        new RemoveProfileImageController(new RemoveProfileImageInteractor(users, presenter)).Remove("@ada", "@ada");

        presenter.ViewModel!.Succeeded.Should().BeTrue();
        users.FindByHandle("@ada")!.ProfileImage.Should().BeNull();
    }

    [Fact]
    public void In_memory_gateway_authenticates_against_credentials()
    {
        var users = new InMemoryUserGateway();
        users.Save("Ada Lovelace", "@ada", "ada@example.com", "Correct9!");

        users.Authenticate("ada@example.com", "Correct9!").Should().NotBeNull();
        users.Authenticate("ada@example.com", "Wrong999!").Should().BeNull();
    }

    [Fact]
    public void Create_account_flow_runs_controller_interactor_gateway_presenter()
    {
        var users = new InMemoryUserGateway();
        var sessions = new InMemorySessionGateway();
        var presenter = new CreateAccountPresenter();
        var interactor = new CreateAccountInteractor(users, sessions, presenter);
        var controller = new CreateAccountController(interactor);

        controller.Create("Grace Hopper", "@grace", "grace@example.com", "NavyCode9!");

        presenter.ViewModel.Should().NotBeNull();
        presenter.ViewModel!.Succeeded.Should().BeTrue();
        presenter.ViewModel.Handle.Should().Be("@grace");
        sessions.FindByToken(presenter.ViewModel.SessionToken!).Should().NotBeNull();
    }

    [Fact]
    public void Registration_requires_email_verification_before_account_exists()
    {
        var users = new InMemoryUserGateway();
        var registrations = new InMemoryPendingRegistrationGateway();
        var codes = new InMemoryVerificationCodeGateway();
        var email = new InMemoryEmailGateway();
        var presenter = new RegisterAccountPresenter();
        var controller = new RegisterAccountController(new RegisterAccountInteractor(users, registrations, codes, email, presenter));

        controller.Register("Ada Lovelace", "@ada", "ada@example.com", "Correct9!");

        presenter.ViewModel!.Succeeded.Should().BeTrue();
        registrations.FindByEmail("ada@example.com")!.Credentials.Should().Be("Correct9!");
        users.FindByHandle("@ada").Should().BeNull();
        email.Sent.Should().ContainSingle(m => m.To == "ada@example.com" && m.Subject.Contains("Verify", StringComparison.OrdinalIgnoreCase));

        var verifyPresenter = new VerifyRegistrationPresenter();
        new VerifyRegistrationController(new VerifyRegistrationInteractor(users, registrations, codes, verifyPresenter))
            .Verify("ada@example.com", codes.FindActiveCode("ada@example.com")!);

        verifyPresenter.ViewModel!.Succeeded.Should().BeTrue();
        users.FindByHandle("@ada").Should().NotBeNull();
        users.Authenticate("ada@example.com", "Correct9!").Should().NotBeNull();
    }

    [Fact]
    public void Login_for_unremembered_device_sends_email_otp_then_verifies_device()
    {
        var users = new InMemoryUserGateway();
        users.Save(UserAccount.CreateWithCredentials("Ada Lovelace", "@ada", "ada@example.com", "Correct9!"));
        var sessions = new InMemorySessionGateway();
        var devices = new InMemoryRememberedDeviceGateway();
        var otps = new InMemoryVerificationCodeGateway();
        var email = new InMemoryEmailGateway();

        var loginPresenter = new LoginWithDevicePresenter();
        new LoginWithDeviceController(new LoginWithDeviceInteractor(users, sessions, devices, otps, email, loginPresenter))
            .Login("ada@example.com", "Correct9!", "browser-1");

        loginPresenter.ViewModel!.OtpRequired.Should().BeTrue();
        loginPresenter.ViewModel.SessionToken.Should().BeNull();

        var otpPresenter = new VerifyDeviceOtpPresenter();
        new VerifyDeviceOtpController(new VerifyDeviceOtpInteractor(users, sessions, devices, otps, otpPresenter))
            .Verify("@ada", "browser-1", otps.FindActiveCode("ada@example.com")!, true);

        otpPresenter.ViewModel!.Succeeded.Should().BeTrue();
        otpPresenter.ViewModel.SessionToken.Should().NotBeNullOrWhiteSpace();

        var rememberedLoginPresenter = new LoginWithDevicePresenter();
        new LoginWithDeviceController(new LoginWithDeviceInteractor(users, sessions, devices, otps, email, rememberedLoginPresenter))
            .Login("ada@example.com", "Correct9!", "browser-1");

        rememberedLoginPresenter.ViewModel!.OtpRequired.Should().BeFalse();
        rememberedLoginPresenter.ViewModel.SessionToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Login_uses_email_address_instead_of_public_handle()
    {
        var users = new InMemoryUserGateway();
        users.Save(UserAccount.CreateWithCredentials("Ada Lovelace", "@ada", "ada@example.com", "Correct9!"));
        var sessions = new InMemorySessionGateway();

        var emailPresenter = new LoginPresenter();
        new LoginController(new LoginInteractor(users, sessions, emailPresenter))
            .Login("ada@example.com", "Correct9!");

        emailPresenter.ViewModel!.Succeeded.Should().BeTrue();
        emailPresenter.ViewModel.Handle.Should().Be("@ada");
        emailPresenter.ViewModel.SessionToken.Should().NotBeNullOrWhiteSpace();

        var handlePresenter = new LoginPresenter();
        new LoginController(new LoginInteractor(users, sessions, handlePresenter))
            .Login("@ada", "Correct9!");

        handlePresenter.ViewModel!.Succeeded.Should().BeFalse();
        handlePresenter.ViewModel.Message.Should().Be("Invalid email or password.");
    }

    [Fact]
    public void Password_reset_link_is_one_time_and_expires_after_five_minutes()
    {
        var now = new MutableClock(DateTimeOffset.UtcNow);
        var users = new InMemoryUserGateway();
        users.Save(UserAccount.CreateWithCredentials("Ada Lovelace", "@ada", "ada@example.com", "Correct9!"));
        var resets = new InMemoryPasswordResetTokenGateway(now);
        var email = new InMemoryEmailGateway();

        var requestPresenter = new RequestPasswordResetPresenter();
        new RequestPasswordResetController(new RequestPasswordResetInteractor(users, resets, email, now, requestPresenter))
            .RequestReset("ada@example.com", "https://localhost/reset-password");

        requestPresenter.ViewModel!.Succeeded.Should().BeTrue();
        email.Sent.Should().ContainSingle(m => m.To == "ada@example.com" && m.Body.Contains("reset-password", StringComparison.OrdinalIgnoreCase));

        var token = resets.FindActiveToken("ada@example.com")!;
        var resetPresenter = new ResetPasswordPresenter();
        new ResetPasswordController(new ResetPasswordInteractor(users, resets, now, resetPresenter))
            .Reset(token, "Changed9!");

        resetPresenter.ViewModel!.Succeeded.Should().BeTrue();
        users.Authenticate("ada@example.com", "Changed9!").Should().NotBeNull();

        var reusedPresenter = new ResetPasswordPresenter();
        new ResetPasswordController(new ResetPasswordInteractor(users, resets, now, reusedPresenter))
            .Reset(token, "Again999!");
        reusedPresenter.ViewModel!.Succeeded.Should().BeFalse();

        new RequestPasswordResetController(new RequestPasswordResetInteractor(users, resets, email, now, new RequestPasswordResetPresenter()))
            .RequestReset("ada@example.com", "https://localhost/reset-password");
        var expiredToken = resets.FindActiveToken("ada@example.com")!;
        now.Advance(TimeSpan.FromMinutes(6));

        var expiredPresenter = new ResetPasswordPresenter();
        new ResetPasswordController(new ResetPasswordInteractor(users, resets, now, expiredPresenter))
            .Reset(expiredToken, "Expired9!");

        expiredPresenter.ViewModel!.Succeeded.Should().BeFalse();
    }

    [Fact]
    public void Password_reset_persists_changed_credentials_through_user_gateway()
    {
        var now = new MutableClock(DateTimeOffset.UtcNow);
        var users = new DetachedUserGateway();
        users.Save(UserAccount.CreateWithCredentials("Ada Lovelace", "@ada", "ada@example.com", "Correct9!"));
        var resets = new InMemoryPasswordResetTokenGateway(now);
        var email = new InMemoryEmailGateway();
        new RequestPasswordResetController(new RequestPasswordResetInteractor(users, resets, email, now, new RequestPasswordResetPresenter()))
            .RequestReset("ada@example.com", "https://localhost/reset-password");

        var token = resets.FindActiveToken("ada@example.com")!;
        var resetPresenter = new ResetPasswordPresenter();
        new ResetPasswordController(new ResetPasswordInteractor(users, resets, now, resetPresenter))
            .Reset(token, "Changed9!");

        resetPresenter.ViewModel!.Succeeded.Should().BeTrue();
        users.Authenticate("ada@example.com", "Changed9!").Should().NotBeNull();
    }

    [Fact]
    public void Login_rejects_invalid_password_and_does_not_create_session()
    {
        var users = new InMemoryUserGateway();
        users.Save(UserAccount.CreateWithCredentials("Grace Hopper", "@grace", "grace@example.com", "NavyCode9!"));
        var sessions = new InMemorySessionGateway();
        var presenter = new LoginPresenter();
        var controller = new LoginController(new LoginInteractor(users, sessions, presenter));

        controller.Login("grace@example.com", "wrong-password");

        presenter.ViewModel.Should().NotBeNull();
        presenter.ViewModel!.Succeeded.Should().BeFalse();
        presenter.ViewModel.Message.Should().Be("Invalid email or password.");
        sessions.AllSessions.Should().BeEmpty();
    }

    [Fact]
    public void Login_interactor_returns_message_key_not_presentation_copy()
    {
        var users = new InMemoryUserGateway();
        var sessions = new InMemorySessionGateway();
        var output = new CapturingLoginOutput();
        var interactor = new LoginInteractor(users, sessions, output);

        interactor.Handle(new("missing@example.com", "wrong-password"));

        output.Response.Should().NotBeNull();
        output.Response!.MessageKey.Should().Be(UserMessageKeys.InvalidCredentials);
        output.Response.MessageKey.Should().NotContain(" ");
        output.Response.MessageKey.Should().NotEndWith(".");
    }

    [Fact]
    public void Login_presenter_translates_message_key_for_view_model()
    {
        var presenter = new LoginPresenter();

        presenter.Present(new(false, UserMessageKeys.InvalidCredentials, null, null));

        presenter.ViewModel!.Message.Should().Be("Invalid email or password.");
    }

    [Fact]
    public void Forgot_and_change_password_use_component_gateways()
    {
        var users = new InMemoryUserGateway();
        var resets = new InMemoryPasswordResetGateway();
        users.Save(UserAccount.CreateWithCredentials("Ada Lovelace", "@ada", "ada@example.com", "Correct9!"));

        var forgotPresenter = new ForgotPasswordPresenter();
        new ForgotPasswordController(new ForgotPasswordInteractor(users, resets, forgotPresenter))
            .RequestReset("ada@example.com");

        forgotPresenter.ViewModel!.Succeeded.Should().BeTrue();
        var token = resets.FindToken("ada@example.com");

        var changePresenter = new ChangePasswordPresenter();
        new ChangePasswordController(new ChangePasswordInteractor(users, resets, changePresenter))
            .ChangePassword(token!, "Changed9!");

        changePresenter.ViewModel!.Succeeded.Should().BeTrue();
        users.Authenticate("ada@example.com", "Changed9!").Should().NotBeNull();
    }

    [Fact]
    public void Search_and_view_user_return_presenter_view_models()
    {
        var users = new InMemoryUserGateway();
        users.Save(UserAccount.CreateWithCredentials("Ada Lovelace", "@ada", "ada@example.com", "Correct9!"));
        users.Save(UserAccount.CreateWithCredentials("Grace Hopper", "@grace", "grace@example.com", "NavyCode9!"));
        var createdAt = new DateTimeOffset(2026, 5, 11, 20, 38, 0, TimeSpan.Zero);

        var searchPresenter = new SearchUserPresenter();
        new SearchUserController(new SearchUserInteractor(users, searchPresenter)).Search("gra");
        searchPresenter.ViewModel!.Users.Should().ContainSingle(u => u.Handle == "@grace");

        var viewPresenter = new ViewUserPresenter();
        new ViewUserController(new ViewUserInteractor(users, new StubUserProfilePostGateway(createdAt), viewPresenter)).View("@ada", "@grace");
        viewPresenter.ViewModel!.DisplayName.Should().Be("Ada Lovelace");
        viewPresenter.ViewModel.ProfileImage.Should().BeNull();
        var post = viewPresenter.ViewModel.Posts.Should().ContainSingle().Subject;
        post.CreatedAt.Should().Be(createdAt);
        post.QuotedPost!.CreatedAt.Should().Be(createdAt.AddMinutes(-10));
    }

    [Fact]
    public void View_user_includes_profile_image_metadata()
    {
        var users = new InMemoryUserGateway();
        var account = UserAccount.CreateWithCredentials("Ada Lovelace", "@ada", "ada@example.com", "Correct9!");
        var assetId = Guid.NewGuid();
        account.SetProfileImage(new ProfileImage(
            assetId,
            "profile-images/ada/avatar.jpg",
            "image/jpeg",
            12_345,
            800,
            600,
            new DateTimeOffset(2026, 5, 11, 22, 0, 0, TimeSpan.Zero)));
        users.Save(account);

        var presenter = new ViewUserPresenter();
        new ViewUserController(new ViewUserInteractor(users, new EmptyUserProfilePostGateway(), presenter)).View("@ada", "@ada");

        presenter.ViewModel!.ProfileImage.Should().NotBeNull();
        presenter.ViewModel.ProfileImage!.AssetId.Should().Be(assetId);
        presenter.ViewModel.ProfileImage.ContentType.Should().Be("image/jpeg");
        presenter.ViewModel.ProfileImage.Width.Should().Be(800);
        presenter.ViewModel.ProfileImage.GetType().GetProperty("ImageUrl").Should().BeNull();
    }

    private sealed class CapturingLoginOutput : ILoginOutputBoundary
    {
        public LoginResponse? Response { get; private set; }

        public void Present(LoginResponse response) => Response = response;
    }

    private sealed class DetachedUserGateway : IUserGateway
    {
        private readonly Dictionary<string, UserAccount> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);
        private readonly ICredentialsGateway _credentials = new InMemoryCredentialsGateway();

        public UserAccount Save(string displayName, string handle, string email, string password)
        {
            var user = UserAccount.CreateWithCredentials(displayName, handle, email, _credentials.Create(password));
            Save(user);
            return user;
        }

        public void Save(UserAccount user)
        {
            _usersByEmail[user.Email] = Copy(user);
        }

        public void ChangePassword(UserAccount user, string password)
        {
            user.ChangeCredentials(_credentials.Create(password));
            Save(user);
        }

        public UserAccount? Authenticate(string email, string password) =>
            FindByEmail(email) is { } user && _credentials.Matches(password, user.Credentials)
                ? user
                : null;

        public UserAccount? FindByHandle(string handle) =>
            _usersByEmail.Values
                .SingleOrDefault(user => string.Equals(user.Handle, handle, StringComparison.OrdinalIgnoreCase)) is { } user
                ? Copy(user)
                : null;

        public UserAccount? FindByEmail(string email) =>
            _usersByEmail.TryGetValue(email, out var user) ? Copy(user) : null;

        public IReadOnlyList<UserAccount> Search(string query) =>
            _usersByEmail.Values
                .Where(user => user.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                               user.Handle.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(Copy)
                .ToArray();

        private static UserAccount Copy(UserAccount user) =>
            UserAccount.Rehydrate(user.Id, user.DisplayName, user.Handle, user.Email, user.Credentials, user.ProfileImage);
    }

    private sealed class EmptyUserProfilePostGateway : IUserProfilePostGateway
    {
        public IReadOnlyList<UserProfilePostSummary> RecentPostsByAuthor(string authorHandle, string readerHandle, int limit) =>
            Array.Empty<UserProfilePostSummary>();
    }

    private sealed class StubUserProfilePostGateway(DateTimeOffset createdAt) : IUserProfilePostGateway
    {
        public IReadOnlyList<UserProfilePostSummary> RecentPostsByAuthor(string authorHandle, string readerHandle, int limit) =>
            new[]
            {
                new UserProfilePostSummary(
                    Guid.NewGuid(),
                    authorHandle,
                    "Profile post",
                    null,
                    Guid.NewGuid(),
                    createdAt,
                    1,
                    true,
                    2,
                    false,
                    new UserProfileQuotedPostSummary(Guid.NewGuid(), "@grace", "Quoted profile post", createdAt.AddMinutes(-10)))
            };
    }
}


