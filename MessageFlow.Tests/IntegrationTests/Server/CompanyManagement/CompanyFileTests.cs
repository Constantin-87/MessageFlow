using System.Net;
using System.Net.Http.Json;
using MessageFlow.Shared.DTOs;
using MessageFlow.Shared.Enums;
using MessageFlow.Tests.Helpers.Factories;

namespace MessageFlow.Tests.IntegrationTests.Server.CompanyManagement
{
    public class CompanyFileTests : IClassFixture<ServerApiFactory>
    {
        private readonly HttpClient _client;

        public CompanyFileTests(ServerApiFactory factory)
        {
            _client = factory.CreateClientWithSuperAdminAuth();
        }

        [Fact]
        public async Task UploadFiles_ReturnsOk_WhenValidDocxFilesProvided()
        {
            var companyId = "1";
            var formData = new MultipartFormDataContent
            {
                { new StringContent(companyId), "companyId" },
                { new StringContent("Test Description"), "descriptions-test.txt" }
            };

            var testFilePath = Path.Combine(AppContext.BaseDirectory, "TestingFiles", "testFAQ_hasContent.docx");
            var fileContent = new ByteArrayContent(File.ReadAllBytes(testFilePath));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
            formData.Add(fileContent, "files", "testFAQ_hasContent.docx");

            var response = await _client.PostAsync("/api/company/upload-files", formData);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task UploadFiles_ReturnsBadRequest_WhenInvalidFileProvided()
        {
            var companyId = "1";
            var formData = new MultipartFormDataContent
            {
                { new StringContent(companyId), "companyId" }
            };

            var response = await _client.PostAsync("/api/company/upload-files", formData);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetPretrainingFiles_ReturnsFiles_WhenValidCompanyId()
        {
            var companyId = "1";

            var response = await _client.GetAsync($"/api/company/{companyId}/pretraining-files");

            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadFromJsonAsync<List<ProcessedPretrainDataDTO>>();
            Assert.NotNull(content);
            Assert.True(content.Count > 0); // Assuming files are present
        }

        [Fact]
        public async Task GetPretrainingFiles_ReturnsBadRequest_WhenInvalidCompanyId()
        {
            var companyId = "nonexistent-company-id";

            var response = await _client.GetAsync($"/api/company/{companyId}/pretraining-files");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeleteFile_ReturnsOk_WhenValidFile()
        {
            var file = new ProcessedPretrainDataDTO
            {
                Id = "valid-file-id",
                FileDescription = "Test file",
                CompanyId = "1",
                FileUrl = "test-file-url",
                FileType = FileType.FAQFile,
                ProcessedAt = DateTime.UtcNow
            };

            // Send a JSON payload in the DELETE request
            var response = await _client.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri("/api/company/delete-file", UriKind.Relative),
                Content = JsonContent.Create(file)
            });

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task DeleteFile_ReturnsBadRequest_WhenInvalidFile()
        {
            var file = new ProcessedPretrainDataDTO
            {
                FileDescription = "File that doesn't exist",
                CompanyId = "1",
                FileUrl = "invalid-file-url",
                FileType = FileType.FAQFile,
                ProcessedAt = DateTime.UtcNow 
            };

            // Send a JSON payload in the DELETE request
            var response = await _client.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri("/api/company/delete-file", UriKind.Relative),
                Content = JsonContent.Create(file)
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}