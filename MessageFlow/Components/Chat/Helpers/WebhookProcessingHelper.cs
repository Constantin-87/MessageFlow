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

    }
}
