# Adding A New Use Case

This guide shows how to add behavior without breaking the component-first architecture.

## 1. Choose The Owning Component

Place the use case in the component that owns the business capability.

Examples:

- `MuteUserPosts` belongs in `SocialApp.Post`.
- `VerifyEmail` belongs in `SocialApp.User`.

Do not create a shared application or use-case project.

## 2. Add Models

Create request and response records inside the owning component:

```csharp
namespace SocialApp.Post.RequestModels;

public sealed record MuteUserPostsRequest(string ReaderHandle, string MutedHandle);
```

```csharp
namespace SocialApp.Post.ResponseModels;

public sealed record MuteUserPostsResponse(bool Succeeded, string Message);
```

Add a view model if callers need formatted output:

```csharp
namespace SocialApp.Post.ViewModels;

public sealed record MuteUserPostsViewModel(bool Succeeded, string Message);
```

## 3. Add Boundaries

Boundaries live in the component's `UseCases` folder:

```csharp
public interface IMuteUserPostsInputBoundary
{
    void Handle(MuteUserPostsRequest request);
}

public interface IMuteUserPostsOutputBoundary
{
    void Present(MuteUserPostsResponse response);
}
```

## 4. Add The Interactor

The interactor executes application business rules and depends on abstractions:

```csharp
public sealed class MuteUserPostsInteractor(
    IPostGateway posts,
    IMuteUserPostsOutputBoundary output) : IMuteUserPostsInputBoundary
{
    public void Handle(MuteUserPostsRequest request)
    {
        posts.Mute(request.ReaderHandle, request.MutedHandle);
        output.Present(new MuteUserPostsResponse(true, "User muted."));
    }
}
```

If a gateway capability is missing, add it to the owning component's gateway interface. Do not introduce `IRepository<T>`.

## 5. Add Controller And Presenter

The controller translates external input into a request model:

```csharp
public sealed class MuteUserPostsController(IMuteUserPostsInputBoundary input)
{
    public void Mute(string readerHandle, string mutedHandle) =>
        input.Handle(new MuteUserPostsRequest(readerHandle, mutedHandle));
}
```

The presenter implements the output boundary:

```csharp
public sealed class MuteUserPostsPresenter : IMuteUserPostsOutputBoundary
{
    public MuteUserPostsViewModel? ViewModel { get; private set; }

    public void Present(MuteUserPostsResponse response) =>
        ViewModel = new MuteUserPostsViewModel(response.Succeeded, response.Message);
}
```

## 6. Test The Request Flow

Write a component test that demonstrates:

```text
Controller -> Input Boundary -> Interactor -> Gateway -> Output Boundary -> Presenter -> ViewModel
```

The test should use real component code and in-memory gateways.

## 7. Add Architecture Tests When Needed

Update `SocialApp.Architecture.Tests` if the use case adds a new rule. Examples:

- a new presenter naming convention
- a new forbidden dependency
- a new component reference rule

Architecture tests should prevent drift automatically.
