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
    public void User_account_requires_valid_handle_and_password()
    {
        var account = UserAccount.Create("Ada Lovelace", "@ada", "ada@example.com", "Correct9!");

        account.Handle.Should().Be("@ada");
        account.CheckPassword("Correct9!").Should().BeTrue();
        Action weakPassword = () => UserAccount.Create("Ada", "@ada2", "ada2@example.com", "weak");
        weakPassword.Should().Throw<ArgumentException>().WithMessage("*Password*");
    }

    [Fact]
    public void User_account_can_be_rehydrated_from_persistence()
    {
        var id = Guid.NewGuid();
        var account = UserAccount.Rehydrate(id, "Ada Lovelace", "@ada", "ada@example.com", "Correct9!");

        account.Id.Should().Be(id);
        account.Handle.Should().Be("@ada");
        account.CheckPassword("Correct9!").Should().BeTrue();
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
        users.FindByHandle("@ada").Should().BeNull();
        email.Sent.Should().ContainSingle(m => m.To == "ada@example.com" && m.Subject.Contains("Verify", StringComparison.OrdinalIgnoreCase));

        var verifyPresenter = new VerifyRegistrationPresenter();
        new VerifyRegistrationController(new VerifyRegistrationInteractor(users, registrations, codes, verifyPresenter))
            .Verify("ada@example.com", codes.FindActiveCode("ada@example.com")!);

        verifyPresenter.ViewModel!.Succeeded.Should().BeTrue();
        users.FindByHandle("@ada").Should().NotBeNull();
    }

    [Fact]
    public void Login_for_unremembered_device_sends_email_otp_then_verifies_device()
    {
        var users = new InMemoryUserGateway();
        users.Save(UserAccount.Create("Ada Lovelace", "@ada", "ada@example.com", "Correct9!"));
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
        users.Save(UserAccount.Create("Ada Lovelace", "@ada", "ada@example.com", "Correct9!"));
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
        users.Save(UserAccount.Create("Ada Lovelace", "@ada", "ada@example.com", "Correct9!"));
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
        users.FindByHandle("@ada")!.CheckPassword("Changed9!").Should().BeTrue();

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
    public void Login_rejects_invalid_password_and_does_not_create_session()
    {
        var users = new InMemoryUserGateway();
        users.Save(UserAccount.Create("Grace Hopper", "@grace", "grace@example.com", "NavyCode9!"));
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
        users.Save(UserAccount.Create("Ada Lovelace", "@ada", "ada@example.com", "Correct9!"));

        var forgotPresenter = new ForgotPasswordPresenter();
        new ForgotPasswordController(new ForgotPasswordInteractor(users, resets, forgotPresenter))
            .RequestReset("ada@example.com");

        forgotPresenter.ViewModel!.Succeeded.Should().BeTrue();
        var token = resets.FindToken("ada@example.com");

        var changePresenter = new ChangePasswordPresenter();
        new ChangePasswordController(new ChangePasswordInteractor(users, resets, changePresenter))
            .ChangePassword(token!, "Changed9!");

        changePresenter.ViewModel!.Succeeded.Should().BeTrue();
        users.FindByHandle("@ada")!.CheckPassword("Changed9!").Should().BeTrue();
    }

    [Fact]
    public void Search_and_view_user_return_presenter_view_models()
    {
        var users = new InMemoryUserGateway();
        users.Save(UserAccount.Create("Ada Lovelace", "@ada", "ada@example.com", "Correct9!"));
        users.Save(UserAccount.Create("Grace Hopper", "@grace", "grace@example.com", "NavyCode9!"));

        var searchPresenter = new SearchUserPresenter();
        new SearchUserController(new SearchUserInteractor(users, searchPresenter)).Search("gra");
        searchPresenter.ViewModel!.Users.Should().ContainSingle(u => u.Handle == "@grace");

        var viewPresenter = new ViewUserPresenter();
        new ViewUserController(new ViewUserInteractor(users, viewPresenter)).View("@ada");
        viewPresenter.ViewModel!.DisplayName.Should().Be("Ada Lovelace");
    }

    private sealed class CapturingLoginOutput : ILoginOutputBoundary
    {
        public LoginResponse? Response { get; private set; }

        public void Present(LoginResponse response) => Response = response;
    }
}
