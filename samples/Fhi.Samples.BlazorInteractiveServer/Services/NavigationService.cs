using Microsoft.AspNetCore.Components;

namespace Client.BlazorInteractiveServer.Services
{
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
            //_navigationManager.NavigateTo("/login?returnUrl=" + Uri.EscapeDataString(_navigationManager.Uri));
        }
    }

}
