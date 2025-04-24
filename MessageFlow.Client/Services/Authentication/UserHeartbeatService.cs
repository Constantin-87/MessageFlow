using MessageFlow.Client.Models.DTOs;
using System.Net.Http.Json;

namespace MessageFlow.Client.Services.Authentication
{
    public class UserHeartbeatService : IDisposable
    {
        private readonly CurrentUserService _currentUser;
        private readonly IHttpClientFactory _httpClientFactory;
        private Timer? _timer;

        public UserHeartbeatService(CurrentUserService currentUser, IHttpClientFactory httpClientFactory)
        {
            _currentUser = currentUser;
            _httpClientFactory = httpClientFactory;
        }

        public void Start()
        {
            _timer = new Timer(async _ =>
            {
                try { await PollUserInfo(); }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Heartbeat] Error: {ex.Message}");
                }
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

        }

        private async Task PollUserInfo()
        {
            if (!_currentUser.IsLoggedIn) return;

            var client = _httpClientFactory.CreateClient("IdentityAPI");
            var response = await client.GetAsync("api/auth/getCurrentUser");

            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<ApplicationUserDTO>();
                if (user != null)
                    _currentUser.SetUser(user); // triggers UI update
            }
        }

        public async Task ForceRefreshAsync()
        {
            try { await PollUserInfo(); }
            catch (Exception ex)
            {
                Console.WriteLine($"[Heartbeat] Forced refresh error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}