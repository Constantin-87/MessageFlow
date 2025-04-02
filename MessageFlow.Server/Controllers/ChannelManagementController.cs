using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.Services.Interfaces;

namespace MessageFlow.Server.Controllers
{
    [Route("api/channels")]
    [ApiController]
    [Authorize(Roles = "Admin, SuperAdmin")]
    public class ChannelManagementController : ControllerBase
    {
        private readonly IFacebookService _facebookService;
        private readonly IWhatsAppService _whatsAppService;

        public ChannelManagementController(IFacebookService facebookService, IWhatsAppService whatsAppService)
        {
            _facebookService = facebookService;
            _whatsAppService = whatsAppService;
        }

        // GET: api/channels/facebook/{companyId}
        [HttpGet("facebook/{companyId}")]
        public async Task<IActionResult> GetFacebookSettings(string companyId)
        {
            var settings = await _facebookService.GetFacebookSettingsAsync(companyId);
            if (settings == null)
                return NotFound();

            return Ok(settings);
        }

        // POST: api/channels/facebook/{companyId}
        [HttpPost("facebook/{companyId}")]
        public async Task<IActionResult> SaveFacebookSettings(string companyId, [FromBody] FacebookSettingsDTO settings)
        {
            var success = await _facebookService.SaveFacebookSettingsAsync(companyId, settings);
            if (!success)
                return BadRequest("Failed to save Facebook settings");

            return Ok();
        }

        // GET: api/channels/whatsapp/{companyId}
        [HttpGet("whatsapp/{companyId}")]
        public async Task<IActionResult> GetWhatsAppSettings(string companyId)
        {
            var settings = await _whatsAppService.GetWhatsAppSettingsAsync(companyId);
            if (settings == null)
                return NotFound();

            return Ok(settings);
        }

        // POST: api/channels/whatsapp/{companyId}
        [HttpPost("whatsapp/{companyId}")]
        public async Task<IActionResult> SaveWhatsAppSettings(string companyId, [FromBody] WhatsAppSettingsDTO settings)
        {
            var success = await _whatsAppService.SaveWhatsAppSettingsAsync(companyId, settings);
            if (!success)
                return BadRequest("Failed to save WhatsApp settings");

            return Ok();
        }
    }
}
