namespace MessageFlow.Client.Models.ViewModels
{
    public class ApiNotificationResultVM
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;

        public static async Task<ApiNotificationResultVM> FromHttpResponseAsync(HttpResponseMessage response, string? successMessageOverride = null)
        {
            var message = await response.Content.ReadAsStringAsync();
            return new ApiNotificationResultVM
            {
                IsSuccess = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode && successMessageOverride != null ? successMessageOverride : message
            };
        }
    }
}