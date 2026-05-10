using SocialApp.User.RequestModels;
using SocialApp.User.ResponseModels;

namespace SocialApp.User.UseCases;

public interface ICreateAccountInputBoundary { void Handle(CreateAccountRequest request); }
public interface ICreateAccountOutputBoundary { void Present(CreateAccountResponse response); }
public interface IRegisterAccountInputBoundary { void Handle(RegisterAccountRequest request); }
public interface IRegisterAccountOutputBoundary { void Present(RegisterAccountResponse response); }
public interface IVerifyRegistrationInputBoundary { void Handle(VerifyRegistrationRequest request); }
public interface IVerifyRegistrationOutputBoundary { void Present(VerifyRegistrationResponse response); }
public interface ILoginInputBoundary { void Handle(LoginRequest request); }
public interface ILoginOutputBoundary { void Present(LoginResponse response); }
public interface ILoginWithDeviceInputBoundary { void Handle(LoginWithDeviceRequest request); }
public interface ILoginWithDeviceOutputBoundary { void Present(LoginWithDeviceResponse response); }
public interface IVerifyDeviceOtpInputBoundary { void Handle(VerifyDeviceOtpRequest request); }
public interface IVerifyDeviceOtpOutputBoundary { void Present(VerifyDeviceOtpResponse response); }
public interface IForgotPasswordInputBoundary { void Handle(ForgotPasswordRequest request); }
public interface IForgotPasswordOutputBoundary { void Present(ForgotPasswordResponse response); }
public interface IChangePasswordInputBoundary { void Handle(ChangePasswordRequest request); }
public interface IChangePasswordOutputBoundary { void Present(ChangePasswordResponse response); }
public interface IRequestPasswordResetInputBoundary { void Handle(RequestPasswordResetRequest request); }
public interface IRequestPasswordResetOutputBoundary { void Present(RequestPasswordResetResponse response); }
public interface IResetPasswordInputBoundary { void Handle(ResetPasswordRequest request); }
public interface IResetPasswordOutputBoundary { void Present(ResetPasswordResponse response); }
public interface ISearchUserInputBoundary { void Handle(SearchUserRequest request); }
public interface ISearchUserOutputBoundary { void Present(SearchUserResponse response); }
public interface IViewUserInputBoundary { void Handle(ViewUserRequest request); }
public interface IViewUserOutputBoundary { void Present(ViewUserResponse response); }
