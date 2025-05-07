using MessageFlow.Shared.DTOs;
using MessageFlow.Tests.Helpers.Factories;
using System.Net;
using System.Net.Http.Json;

namespace MessageFlow.Tests.IntegrationTests.Server.CompanyManagement
{
    public class CompanyContactUpdateTests : IClassFixture<ServerApiFactory>
    {
        private readonly HttpClient _client;

        public CompanyContactUpdateTests(ServerApiFactory factory)
        {
            _client = factory.CreateClientWithSuperAdminAuth();
        }

        [Fact]
        public async Task UpdateCompanyEmails_ReturnsOk_WhenValidEmailsProvided()
        {
            var companyId = "1";

            var emails = new List<CompanyEmailDTO>
            {
                new CompanyEmailDTO
                {
                    Id = Guid.NewGuid().ToString(),
                    EmailAddress = "test@test.com",
                    Description = "Test email",
                    CompanyId = companyId
                }
            };

            var response = await _client.PutAsJsonAsync($"/api/company/update-emails", emails);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task UpdateCompanyEmails_ReturnsBadRequest_WhenInvalidEmailFormat()
        {
            var companyId = "1";

            var emails = new List<CompanyEmailDTO>
            {
                new CompanyEmailDTO
                {
                    EmailAddress = "invalid-email-format",
                    Description = "Test email",
                    CompanyId = companyId
                }
            };

            var response = await _client.PutAsJsonAsync($"/api/company/update-emails", emails);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateCompanyPhoneNumbers_ReturnsOk_WhenValidPhoneNumbersProvided()
        {
            var companyId = "1";

            var phoneNumbers = new List<CompanyPhoneNumberDTO>
            {
                new CompanyPhoneNumberDTO
                {
                    Id = Guid.NewGuid().ToString(),
                    PhoneNumber = "+1234567890",
                    CompanyId = companyId,
                    Description = "Main contact number"
                }
            };

            var response = await _client.PutAsJsonAsync($"/api/company/update-phone-numbers", phoneNumbers);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task UpdateCompanyPhoneNumbers_ReturnsBadRequest_WhenInvalidPhoneNumberFormat()
        {
            var companyId = "1";

            var phoneNumbers = new List<CompanyPhoneNumberDTO>
            {
                new CompanyPhoneNumberDTO { PhoneNumber = "invalid-phone-number", CompanyId = companyId }
            };

            var response = await _client.PutAsJsonAsync($"/api/company/update-phone-numbers", phoneNumbers);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateCompanyPhoneNumbers_ReturnsBadRequest_WhenCompanyDoesNotExist()
        {
            var companyId = "nonexistent-company-id";

            var phoneNumbers = new List<CompanyPhoneNumberDTO>
            {
                new CompanyPhoneNumberDTO { PhoneNumber = "+1234567890", CompanyId = companyId }
            };

            var response = await _client.PutAsJsonAsync($"/api/company/update-phone-numbers", phoneNumbers);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateCompanyEmails_ReturnsBadRequest_WhenEmailIsEmpty()
        {
            var companyId = "1";

            var emails = new List<CompanyEmailDTO>
            {
                new CompanyEmailDTO
                {
                    EmailAddress = "invalid-email",
                    Description = "",
                    CompanyId = companyId
                }
            };

            var response = await _client.PutAsJsonAsync($"/api/company/update-emails", emails);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateCompanyPhoneNumbers_ReturnsBadRequest_WhenPhoneNumberIsEmpty()
        {
            var companyId = "1";

            var phoneNumbers = new List<CompanyPhoneNumberDTO>
            {
                new CompanyPhoneNumberDTO { PhoneNumber = "", CompanyId = companyId }
            };

            var response = await _client.PutAsJsonAsync($"/api/company/update-phone-numbers", phoneNumbers);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}