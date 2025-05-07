using MediatR;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.AiBotProcessing.Commands;
using MessageFlow.Shared.DTOs;
using OpenAI.Chat;
using System.Text.Json;
using MessageFlow.DataAccess.Models;
using MessageFlow.Server.DataTransferObjects.Internal;
using MessageFlow.AzureServices.Interfaces;
using MessageFlow.Server.MediatR.Chat.AiBotProcessing.Helpers;

namespace MessageFlow.Server.MediatR.Chat.AiBotProcessing.CommandHandlers
{
    public class HandleUserQueryHandler : IRequestHandler<HandleUserQueryCommand, UserQueryResponseDTO>
    {
        private readonly IAzureSearchQueryService _searchService;
        private readonly IAzureOpenAIClientService _openAiClientService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly string _deployment;
        private readonly ILogger<HandleUserQueryHandler> _logger;

        public HandleUserQueryHandler(
            IAzureSearchQueryService searchService,
            IAzureOpenAIClientService openAiClientService,
            IConfiguration config,
            IUnitOfWork unitOfWork,
            ILogger<HandleUserQueryHandler> logger)
        {
            _searchService = searchService;
            _openAiClientService = openAiClientService;
            _unitOfWork = unitOfWork;
            _logger = logger;

            var endpoint = config["azure-gbt-endpoint"];
            var key = config["azure-gbt-deployment-key"];
            _deployment = "MessageFlowGptDeployment";
        }

        public async Task<UserQueryResponseDTO> Handle(HandleUserQueryCommand req, CancellationToken ct)
        {
            var searchResults = await _searchService.QueryIndexAsync(req.UserQuery, req.CompanyId);
            var fallbackTeams = searchResults.Any()
                ? new List<SearchResultDTO>()
                : await _searchService.QueryIndexAsync("company teams", req.CompanyId);

            var chatHistory = await _unitOfWork.Messages.GetMessagesByConversationIdAsync(req.ConversationId, 5);
            var historyStr = FormatChatHistory(chatHistory);

            var sysPrompt = await BuildSystemPrompt(historyStr, searchResults, fallbackTeams, req.CompanyId);

            var result = await CallGptAsync(sysPrompt, req.UserQuery, ct);
            return result;
        }

        private async Task<UserQueryResponseDTO> CallGptAsync(string systemPrompt, string userQuery, CancellationToken ct)
        {
            var chatClient = _openAiClientService.GetAzureClient().GetChatClient(_deployment);
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage($"User Query: {userQuery}")
            };
            
            try
            {
                ChatCompletion completion = await chatClient.CompleteChatAsync(messages, new ChatCompletionOptions
                {
                    Temperature = 0.7f,
                    MaxOutputTokenCount = 500
                });

                var content = completion.Content?.FirstOrDefault()?.Text ?? "";
                var (redirect, teamId, teamName) = TryExtractRedirect(content, _logger);
                return new UserQueryResponseDTO
                {
                    Answered = true,
                    RawResponse = content,
                    TargetTeamId = redirect ? teamId : null,
                    TargetTeamName = redirect ? teamName : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GPT error while handling query '{UserQuery}'", userQuery);
                return new UserQueryResponseDTO { Answered = false, RawResponse = "" };
            }
        }

        private async Task<string> BuildSystemPrompt(string history, List<SearchResultDTO> results, List<SearchResultDTO> fallback, string companyId)
        {
            var company = await _unitOfWork.Companies.GetByIdStringAsync(companyId);
            var companyName = company.CompanyName;
            return results.Any()
                ? PromptBuilder.WithResults(history, results, companyName)
                : PromptBuilder.NoResults(history, fallback, companyName);
        }

        private static (bool Redirect, string? TeamId, string? TeamName) TryExtractRedirect(string input, ILogger logger)
        {
            try
            {
                // Attempt to locate a JSON object in the input
                int start = input.IndexOf('{');
                int end = input.LastIndexOf('}');
                if (start == -1 || end == -1 || end <= start)
                    return (false, null, null);

                var json = input.Substring(start, end - start + 1);

                var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (parsed != null && parsed.TryGetValue("redirect", out var redirectFlag) && redirectFlag.GetBoolean())
                {
                    var id = parsed.TryGetValue("teamId", out var idElement) ? idElement.GetString() : null;
                    var name = parsed.TryGetValue("teamName", out var nameElement) ? nameElement.GetString() : null;
                    return (true, id, name);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GPT error while parsing redirect JSON block");
            }

            return (false, null, null);
        }

        private static string FormatChatHistory(IEnumerable<Message> msgs)
        {
            if (!msgs.Any()) return "No prior conversation history.";
            return string.Join("\n", msgs.OrderBy(m => m.SentAt)
                .Select(m => $"[{m.SentAt:HH:mm}] {m.Username}: {m.Content}"));
        }
    }
}