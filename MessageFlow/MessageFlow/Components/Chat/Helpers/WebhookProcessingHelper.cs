using System.Text.Json;

namespace MessageFlow.Components.Chat.Helpers
{
    public static class WebhookProcessingHelper
    {
        public static async Task ProcessWebhookEntriesAsync(
            JsonElement body,
            string objectType,
            ILogger logger,
            Func<JsonElement, Task> processEntryFunc)
        {
            if (body.GetProperty("object").GetString() != objectType)
            {
                logger.LogWarning($"Unexpected object type in webhook: {body.GetProperty("object").GetString()}");
                throw new InvalidOperationException($"Unsupported object type: {body.GetProperty("object").GetString()}");
            }

            foreach (var entry in body.GetProperty("entry").EnumerateArray())
            {
                try
                {
                    await processEntryFunc(entry);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error processing entry: {ex.Message}");
                }
            }
        }

        public static async Task<bool> VerifyTokenAsync<T>(
             Func<Task<List<T>>> getAllSettingsFunc,
             Func<T, string> getVerifyTokenFunc,
             string hubMode,
             string hubVerifyToken,
            ILogger logger)
        {
            logger.LogInformation($"Verifying webhook: mode={hubMode}, token={hubVerifyToken}");

            if (hubMode != "subscribe")
            {
                logger.LogWarning("Invalid hub mode.");
                return false;
            }

            var allSettings = await getAllSettingsFunc();
            var isMatched = allSettings.Any(settings => getVerifyTokenFunc(settings) == hubVerifyToken);

            if (isMatched)
            {
                logger.LogInformation("Webhook verified successfully.");
                return true;
            }

            logger.LogWarning("Webhook verification failed.");
            return false;
        }
    }
}
