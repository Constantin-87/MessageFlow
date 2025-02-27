using Newtonsoft.Json;
using System.Text;

namespace MessageFlow.Server.Components.Chat.Helpers
{
    public static class MessageSenderHelper
    {
        public static async Task<HttpResponseMessage> SendMessageAsync(string url, object payload, string accessToken, ILogger logger)
        {
            using var httpClient = new HttpClient();
            var jsonContent = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            logger.LogInformation($"Sending HTTP POST to {url}: {JsonConvert.SerializeObject(payload)}");

            var response = await httpClient.PostAsync(url, jsonContent);
            return response;
        }
    }
}
