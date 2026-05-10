using FluentAssertions;
using SocialApp.User.Controllers;
using SocialApp.User.Entities;
using SocialApp.User.Gateways;
using SocialApp.User.Presenters;
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
    public void Login_rejects_invalid_password_and_does_not_create_session()
    {
        var users = new InMemoryUserGateway();
        users.Save(UserAccount.Create("Grace Hopper", "@grace", "grace@example.com", "NavyCode9!"));
        var sessions = new InMemorySessionGateway();
        var presenter = new LoginPresenter();
        var controller = new LoginController(new LoginInteractor(users, sessions, presenter));

        controller.Login("@grace", "wrong-password");

        presenter.ViewModel.Should().NotBeNull();
        presenter.ViewModel!.Succeeded.Should().BeFalse();
        presenter.ViewModel.Message.Should().Be("Invalid handle or password.");
        sessions.AllSessions.Should().BeEmpty();
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
}
