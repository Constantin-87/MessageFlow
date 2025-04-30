using MessageFlow.Client.Models.DTOs;

namespace MessageFlow.Client.Models.ViewModels
{
    public class ConversationViewModel
    {
        public ConversationDTO Conversation { get; set; }
        public bool IsActiveTab { get; set; } = false;

        public ConversationViewModel(ConversationDTO conversation)
        {
            Conversation = conversation;
        }

        public string GetSourceIcon()
        {
            return Conversation.Source switch
            {
                "Facebook" => "images/facebook.svg",
                "WhatsApp" => "images/whatsapp.svg",
                "Gateway" => "images/sms.svg",
                _ => "icons/red-dot.svg"
            };
        }

        public string GetSourceAltText()
        {
            return Conversation.Source switch
            {
                "Facebook" => "Facebook",
                "WhatsApp" => "WhatsApp",
                "Gateway" => "Gateway",
                _ => "Unknown"
            };
        }
    }
}