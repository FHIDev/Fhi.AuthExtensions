using Microsoft.AspNetCore.Components;

namespace BlazorApp.HelseId.Services;

public class NavigationService
{
    private readonly NavigationManager _navigationManager;

    public NavigationService(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

    public void RedirectToLogin()
    {
        var currentUri = new Uri(_navigationManager.Uri);
        var path = currentUri.PathAndQuery;
        _navigationManager.NavigateTo(path, forceLoad: true);
    }
}
