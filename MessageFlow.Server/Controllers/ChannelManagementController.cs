using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MessageFlow.Shared.DTOs;
using MediatR;
using MessageFlow.Server.MediatorComponents.Chat.FacebookProcessing.Queries;
using MessageFlow.Server.MediatorComponents.Chat.FacebookProcessing.Commands;
using MessageFlow.Server.MediatorComponents.Chat.WhatsappProcessing.Commands;
using MessageFlow.Server.MediatorComponents.Chat.WhatsappProcessing.Queries;

namespace MessageFlow.Server.Controllers
{
    [Route("api/channels")]
    [ApiController]
    [Authorize(Roles = "Admin, SuperAdmin")]
    public class ChannelManagementController : ControllerBase
    {

        private readonly IMediator _mediator;

        public ChannelManagementController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/channels/facebook/{companyId}
        [HttpGet("facebook/{companyId}")]
        public async Task<IActionResult> GetFacebookSettings(string companyId)
        {
            var settings = await _mediator.Send(new GetFacebookSettingsQuery(companyId));
            if (settings == null)
                return NotFound();

            return Ok(settings);
        }

        // POST: api/channels/facebook/{companyId}
        [HttpPost("facebook/{companyId}")]
        public async Task<IActionResult> SaveFacebookSettings(string companyId, [FromBody] FacebookSettingsDTO settings)
        {
            var success = await _mediator.Send(new SaveFacebookSettingsCommand(companyId, settings));
            if (!success)
                return BadRequest("Failed to save Facebook settings");

            return Ok();
        }

        // GET: api/channels/whatsapp/{companyId}
        [HttpGet("whatsapp/{companyId}")]
        public async Task<IActionResult> GetWhatsAppSettings(string companyId)
        {
            var settings = await _mediator.Send(new GetWhatsAppSettingsQuery(companyId));
            if (settings == null)
                return NotFound();

            return Ok(settings);
        }

        // POST: api/channels/whatsapp/{companyId}
        [HttpPost("whatsapp/{companyId}")]
        public async Task<IActionResult> SaveWhatsAppSettings(string companyId, [FromBody] WhatsAppSettingsDTO whatsAppSettingsDTO)
        {
            var success = await _mediator.Send(new SaveWhatsAppSettingsCommand(companyId, whatsAppSettingsDTO));
            if (!success)
                return BadRequest("Failed to save WhatsApp settings");

            return Ok();
        }
    }
}
