namespace MessageFlow.Server.DataTransferObjects.Internal
{
    public class UserQueryResponseDTO
    {
        public bool Answered { get; set; }
        public string RawResponse { get; set; } = "";
        public string? TargetTeamId { get; set; }
        public string? TargetTeamName { get; set; }
    }
}
