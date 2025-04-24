using MessageFlow.Client.Models.DTOs;

namespace MessageFlow.Client.Services.Authentication
{
    public class CurrentUserService
    {
        public event Action? OnChange;

        private ApplicationUserDTO? _user;
        public ApplicationUserDTO? User => _user;

        public void SetUser(ApplicationUserDTO user)
        {
            _user = user;
            NotifyStateChanged();
        }

        public void Clear()
        {
            _user = null;
            NotifyStateChanged();
        }

        public bool IsLoggedIn => _user != null;
        public bool IsSuperAdmin => _user?.Role?.Contains("SuperAdmin") == true;
        public bool IsAdmin => _user?.Role?.Contains("Admin") == true;
        public string? UserId => _user!.Id;
        public string? CompanyId => _user?.CompanyId;
        public string? CompanyName => _user?.CompanyDTO?.CompanyName;
        public string? Username => _user!.UserName;
        public DateTime? LastActivity => _user?.LastActivity;

        private void NotifyStateChanged()
        {
            OnChange?.Invoke();
        }
    }
}