using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.UserManagement.Commands
{
    public class UpdateUserCommand : IRequest<(bool success, string errorMessage)>
    {
        public ApplicationUserDTO UserDto { get; }

        public UpdateUserCommand(ApplicationUserDTO userDto)
        {
            UserDto = userDto;
        }
    }
}
