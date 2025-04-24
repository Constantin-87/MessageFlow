using MessageFlow.Client.Models.DTOs;

namespace MessageFlow.Client.Models.Extensions
{
    public static class MessageDTOExtensions
    {
        public static string GetStatusIcon(this MessageDTO message)
        {
            return message.Status switch
            {
                "SentToProvider" => "images/sent.png",
                "sent" => "images/sent.png",
                "delivered" => "images/delivered.png",
                "read" => "images/read.png",
                "error" => "images/error.png",
                _ => "images/unknown.png"
            };
        }

        public static string GetTooltip(this MessageDTO message)
        {
            return message.Status switch
            {
                "SentToProvider" => "Message has been sent to the provider.",
                "sent" => "Message has been sent to user.",
                "delivered" => "Message has been delivered.",
                "read" => "Message has been read.",
                "error" => "An error occurred.",
                _ => "Unknown status."
            };
        }
    }
}