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

        public string GetSourceLabel() => Conversation.Source switch
        {
            "Facebook" => "FB",
            "WhatsApp" => "WA",
            "Gateway" => "GW",
            _ => "UNK"
        };
    }
}
