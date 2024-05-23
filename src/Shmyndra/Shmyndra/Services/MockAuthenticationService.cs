using System;
using System.Collections.Generic;

namespace Shmyndra.Services;

public partial record UserContext
{
    public string? Name { get; init; }

    public string? Email { get; init; }

    public string? AccessToken { get; init; }
}

internal class MockAuthenticationService : IAuthenticationService
{
    private UserContext? _user;

    public event EventHandler LoggedOut;

    public string[] Providers => throw new NotImplementedException();

    public async Task<string> GetAccessToken() => _user?.AccessToken ?? string.Empty;

    public async Task<UserContext?> GetCurrentUserAsync() => _user;

    public async Task<UserContext?> AuthenticateAsync(IDispatcher dispatcher)
    {
        _user = new UserContext
        {
            Name = "Foo Bar",
            Email = "foo.bar@gmail.com",
            AccessToken = "MOCK_ACCESS_TOKEN"
        };

        return _user;
    }

    public async Task SignOutAsync()
    {
        _user = null;
    }

    public ValueTask<bool> LoginAsync(IDispatcher? dispatcher, IDictionary<string, string>? credentials = null, string? provider = null, CancellationToken? cancellationToken = null)
    {
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> RefreshAsync(CancellationToken? cancellationToken = null)
    {
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> LogoutAsync(IDispatcher? dispatcher, CancellationToken? cancellationToken = null)
    {
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> IsAuthenticated(CancellationToken? cancellationToken = null)
    {
        return ValueTask.FromResult(true);
    }
}
