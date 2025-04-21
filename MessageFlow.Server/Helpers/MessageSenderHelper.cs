using MessageFlow.Server.Helpers.Interfaces;
using Newtonsoft.Json;
using System.Text;

namespace MessageFlow.Server.Helpers
{
    public class MessageSenderHelper : IMessageSenderHelper
    {
        public async Task<HttpResponseMessage> SendMessageAsync(string url, object payload, string accessToken, ILogger logger)
        {
            using var httpClient = new HttpClient();
            var jsonContent = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var response = await httpClient.PostAsync(url, jsonContent);
            return response;
        }
    }
}
