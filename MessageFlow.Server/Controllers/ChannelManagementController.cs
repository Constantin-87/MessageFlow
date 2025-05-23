﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MessageFlow.Server.DataTransferObjects.Client;
using MediatR;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.Queries;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Queries;

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

        [HttpGet("facebook/{companyId}")]
        public async Task<IActionResult> GetFacebookSettings(string companyId)
        {
            var settings = await _mediator.Send(new GetFacebookSettingsQuery(companyId));
            if (settings == null)
            {
                settings = new Shared.DTOs.FacebookSettingsDTO { CompanyId = companyId };
            }
            return Ok(settings);

        }

        [HttpPost("facebook/{companyId}")]
        public async Task<IActionResult> SaveFacebookSettings(string companyId, [FromBody] Shared.DTOs.FacebookSettingsDTO settings)
        {
            var (success, message) = await _mediator.Send(new SaveFacebookSettingsCommand(companyId, settings));
            return success ? Ok(message) : BadRequest(message);
        }

        [HttpGet("whatsapp/{companyId}")]
        public async Task<IActionResult> GetWhatsAppSettings(string companyId)
        {
            var settings = await _mediator.Send(new GetWhatsAppSettingsQuery(companyId));
            if (settings == null)
            {
                settings = new Shared.DTOs.WhatsAppSettingsDTO { CompanyId = companyId };
            }
            return Ok(settings);
        }

        [HttpPost("whatsapp/settings")]
        public async Task<IActionResult> SaveCoreSettings([FromBody] WhatsAppCoreSettingsDTO settings)
        {
            var (success, message) = await _mediator.Send(new SaveWhatsAppCoreSettingsCommand
            {
                CompanyId = settings.CompanyId,
                AccessToken = settings.AccessToken,
                BusinessAccountId = settings.BusinessAccountId
            });
            return success ? Ok(message) : BadRequest(message);
        }

        [HttpPost("whatsapp/numbers")]
        public async Task<IActionResult> SavePhoneNumbers([FromBody] List<PhoneNumberInfoDTO> phoneNumbers)
        {
            var (success, message) = await _mediator.Send(new SaveWhatsAppPhoneNumbersCommand(phoneNumbers));
            return success ? Ok(message) : BadRequest(message);
        }
    }
}