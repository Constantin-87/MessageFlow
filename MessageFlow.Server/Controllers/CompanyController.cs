using Microsoft.AspNetCore.Mvc;
using MessageFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using MessageFlow.Server.MediatR.CompanyManagement.Queries;
using MessageFlow.Server.MediatR.CompanyManagement.Commands;

namespace MessageFlow.Server.Controllers
{
    [Route("api/company")]
    [ApiController]
    [Authorize(Roles = "Admin, SuperAdmin")]
    public class CompanyController : ControllerBase
    {
        private readonly ILogger<CompanyController> _logger;
        private readonly IMediator _mediator;

        public CompanyController(IMediator mediator, ILogger<CompanyController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // Get all companies (SuperAdmin sees all, Admin sees only their own)
        [HttpGet("all")]
        public async Task<ActionResult<List<CompanyDTO>>> GetAllCompanies()
        {
            var companies = await _mediator.Send(new GetAllCompaniesQuery());
            return Ok(companies);
        }

        // Get company details by ID
        [HttpGet("{companyId}")]
        public async Task<ActionResult<CompanyDTO>> GetCompanyById(string companyId)
        {
            var company = await _mediator.Send(new GetCompanyByIdQuery(companyId));
            if (company == null) return NotFound("Company not found.");
            return Ok(company);
        }

        // Create a new company (SuperAdmins only)
        [HttpPost("create")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult> CreateCompany([FromBody] CompanyDTO companyDto)
        {
            var (success, message) = await _mediator.Send(new CreateCompanyCommand(companyDto));
            if (!success) return BadRequest(message);
            return Ok(message);
        }

        // Update company details
        [HttpPut("update")]
        public async Task<ActionResult> UpdateCompany([FromBody] CompanyDTO companyDto)
        {
            var (success, message) = await _mediator.Send(new UpdateCompanyDetailsCommand(companyDto));
            if (!success) return BadRequest(message);
            return Ok(message);
        }

        // Delete company (SuperAdmins only)
        [HttpDelete("delete/{companyId}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult> DeleteCompany(string companyId)
        {
            var (success, message) = await _mediator.Send(new DeleteCompanyCommand(companyId));
            if (!success) return BadRequest(message);
            return Ok(message);
        }

        [HttpGet("user-company")]
        public async Task<ActionResult<CompanyDTO>> GetCompanyForUser()
        {
            var company = await _mediator.Send(new GetCompanyForUserQuery());

            if (company == null)
                return NotFound("Company not found for the user.");

            return Ok(company);
        }

        // Update company emails
        [HttpPut("update-emails")]
        public async Task<ActionResult> UpdateCompanyEmails([FromBody] List<CompanyEmailDTO> emails)
        {
            var (success, message) = await _mediator.Send(new UpdateCompanyEmailsCommand(emails));

            return success ? Ok(message) : BadRequest(message);
        }

        // Update company phone numbers
        [HttpPut("update-phone-numbers")]
        public async Task<ActionResult> UpdateCompanyPhoneNumbers([FromBody] List<CompanyPhoneNumberDTO> phoneNumbers)
        {
            var (success, message) = await _mediator.Send(new UpdateCompanyPhoneNumbersCommand(phoneNumbers));
            return success ? Ok(message) : BadRequest(message);
        }

        // Fetch company metadata
        [HttpGet("{companyId}/metadata")]
        public async Task<ActionResult<string>> GetCompanyMetadata(string companyId)
        {
            var (success, metadata, message) = await _mediator.Send(new GetCompanyMetadataQuery(companyId));
            return success ? Ok(metadata) : BadRequest(message);
        }

        // Generate and upload metadata
        [HttpPost("{companyId}/generate-metadata")]
        public async Task<ActionResult> GenerateAndUploadMetadata(string companyId)
        {
            var (success, message) = await _mediator.Send(new GenerateCompanyMetadataCommand(companyId));
            return success ? Ok(message) : BadRequest(message);
        }

        // Delete metadata
        [HttpDelete("{companyId}/delete-metadata")]
        public async Task<ActionResult> DeleteCompanyMetadata(string companyId)
        {
            var (success, message) = await _mediator.Send(new DeleteCompanyMetadataCommand(companyId));
            return success ? Ok(message) : BadRequest(message);
        }

        // Fetch pretraining files
        [HttpGet("{companyId}/pretraining-files")]
        public async Task<ActionResult<List<ProcessedPretrainDataDTO>>> GetPretrainingFiles(string companyId)
        {
            var (success, files, message) = await _mediator.Send(new GetCompanyPretrainingFilesQuery(companyId));
            return success ? Ok(files) : BadRequest(message);
        }

        // Upload pretraining files
        [HttpPost("upload-files")]
        public async Task<IActionResult> UploadPretrainingFiles()
        {
            var files = Request.Form.Files;
            var companyId = Request.Form["companyId"].ToString();
            var descriptions = Request.Form["descriptions"];

            var fileDtos = new List<PretrainDataFileDTO>();
            var streams = new List<MemoryStream>();

            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                ms.Position = 0;

                // Get description for this file from the keyed field
                var descKey = $"descriptions-{file.FileName}";
                var description = Request.Form[descKey].ToString();

                fileDtos.Add(new PretrainDataFileDTO
                {
                    FileName = file.FileName,
                    FileDescription = description,
                    FileContent = ms,
                    CompanyId = companyId
                });

                streams.Add(ms);
            }

            var result = await _mediator.Send(new UploadCompanyFilesCommand(fileDtos));

            foreach (var stream in streams)
            {
                stream.Dispose();
            }

            return result.success ? Ok(result.errorMessage) : BadRequest(result.errorMessage);
        }

        // Delete a specific file
        [HttpDelete("delete-file")]
        public async Task<ActionResult> DeleteFile([FromBody] ProcessedPretrainDataDTO file)
        {
            var success = await _mediator.Send(new DeleteCompanyFileCommand(file));

            return success ? Ok("File deleted successfully.") : BadRequest("Failed to delete file.");
        }

        // Create Azure AI Search Index
        [HttpPost("{companyId}/create-search-index")]
        public async Task<ActionResult> CreateSearchIndex(string companyId)
        {
            var (success, message) = await _mediator.Send(new CreateSearchIndexCommand(companyId));

            return success ? Ok(message) : BadRequest(message);
        }
    }
}