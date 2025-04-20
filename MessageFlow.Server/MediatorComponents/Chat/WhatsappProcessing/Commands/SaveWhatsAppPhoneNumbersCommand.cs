using MediatR;
using MessageFlow.Server.DataTransferObjects.Client;

namespace MessageFlow.Server.MediatorComponents.Chat.WhatsappProcessing.Commands
{
    public class SaveWhatsAppPhoneNumbersCommand : IRequest<(bool success, string errorMessage)>
    {
        public List<PhoneNumberInfoDTO> PhoneNumbers { get; set; }

        public SaveWhatsAppPhoneNumbersCommand(List<PhoneNumberInfoDTO> phoneNumbers)
        {
            PhoneNumbers = phoneNumbers;
        }
    }
}