using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatR.UserManagement.Commands
{
    public class CreateUserCommand : IRequest<(bool success, string errorMessage)>
    {
        public ApplicationUserDTO UserDto { get; }

        public CreateUserCommand(ApplicationUserDTO userDto)
        {
            UserDto = userDto;
        }
    }
}
