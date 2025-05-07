using MessageFlow.AzureServices.Helpers;
using MessageFlow.AzureServices.Interfaces;
using MessageFlow.Shared.DTOs;
using Moq;
using OfficeOpenXml;

namespace MessageFlow.Tests.UnitTests.AzureServices.Helpers;
public class CompanyDataHelperTests
{
    private readonly CompanyDataHelper _helper = new();
    private readonly Mock<IDocumentProcessingService> _docServiceMock = new();

    [Fact]
    public void GenerateStructuredCompanyMetadata_ReturnsExpectedFiles()
    {
        var company = new CompanyDTO
        {
            Id = "c1",
            CompanyName = "TestCorp",
            Description = "Desc",
            IndustryType = "Tech",
            WebsiteUrl = "https://test.com",
            CompanyEmails = [new() { EmailAddress = "test@test.com", Description = "Support" }],
            CompanyPhoneNumbers = [new() { PhoneNumber = "123", Description = "Main" }],
            Teams = [new() { Id = "t1", TeamName = "Sales", TeamDescription = "Sales dept" }]
        };

        var (files, jsons) = _helper.GenerateStructuredCompanyMetadata(company);

        Assert.Equal(4, files.Count);
        Assert.Equal(4, jsons.Count);
    }

    [Fact]
    public async Task ProcessUploadedFilesAsync_UsesDocumentServiceForUnknownTypes()
    {
        var fileContent = new MemoryStream([.. System.Text.Encoding.UTF8.GetBytes("Q: What?\nA: Answer")]);
        var files = new List<PretrainDataFileDTO>
        {
            new() { FileName = "test.txt", FileContent = fileContent, FileDescription = "txt", CompanyId = "c1" }
        };

        _docServiceMock.Setup(x => x.ExtractTextFromDocumentAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync("\nQ: What?\nA: Answer");

        var (processed, jsons) = await _helper.ProcessUploadedFilesAsync(files, _docServiceMock.Object);

        Assert.Single(processed);
        Assert.Single(jsons);
        Assert.Contains("question", jsons[0]);
    }

    [Fact]
    public async Task ProcessUploadedFilesAsync_HandlesCsvFiles()
    {
        var csvContent = "Name,Role\nAlice,Engineer";
        var file = new PretrainDataFileDTO
        {
            FileName = "data.csv",
            FileDescription = "CSV Desc",
            CompanyId = "c1",
            FileContent = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent))
        };

        var (processed, jsons) = await _helper.ProcessUploadedFilesAsync([file], _docServiceMock.Object);

        Assert.Single(processed);
        Assert.Single(jsons);
        Assert.Contains("Alice", jsons[0]);
    }

    [Fact]
    public async Task ProcessUploadedFilesAsync_HandlesExcelFiles()
    {
        using var stream = new MemoryStream();
        using (var package = new ExcelPackage(stream))
        {
            var sheet = package.Workbook.Worksheets.Add("Sheet1");
            sheet.Cells[1, 1].Value = "Name";
            sheet.Cells[1, 2].Value = "Role";
            sheet.Cells[2, 1].Value = "Bob";
            sheet.Cells[2, 2].Value = "Manager";
            package.Save();
        }
        stream.Position = 0;

        var file = new PretrainDataFileDTO
        {
            FileName = "data.xlsx",
            FileDescription = "Excel Desc",
            CompanyId = "c1",
            FileContent = stream
        };

        var (processed, jsons) = await _helper.ProcessUploadedFilesAsync([file], _docServiceMock.Object);

        Assert.Single(processed);
        Assert.Single(jsons);
        Assert.Contains("Bob", jsons[0]);
    }

    [Fact]
    public async Task ProcessUploadedFilesAsync_HandlesTextWithoutFaq()
    {
        var file = new PretrainDataFileDTO
        {
            FileName = "doc.txt",
            FileDescription = "Desc",
            CompanyId = "c1",
            FileContent = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("General information document"))
        };

        _docServiceMock.Setup(x => x.ExtractTextFromDocumentAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync("This is a general text without FAQ format.");

        var (processed, jsons) = await _helper.ProcessUploadedFilesAsync([file], _docServiceMock.Object);

        Assert.Single(processed);
        Assert.Single(jsons);
        Assert.Contains("TextDocument", jsons[0]);
    }

    [Fact]
    public async Task ProcessUploadedFilesAsync_SkipsEmptyExcelSheets()
    {
        using var stream = new MemoryStream();
        using (var package = new ExcelPackage(stream))
        {
            package.Workbook.Worksheets.Add("EmptySheet");
            package.Save();
        }
        stream.Position = 0;

        var file = new PretrainDataFileDTO
        {
            FileName = "empty.xlsx",
            FileDescription = "Excel Empty",
            CompanyId = "c1",
            FileContent = stream
        };

        var (processed, jsons) = await _helper.ProcessUploadedFilesAsync([file], _docServiceMock.Object);

        Assert.Empty(processed);
        Assert.Empty(jsons);
    }

    [Fact]
    public void ExtractFAQs_IgnoresInvalidBlocks()
    {
        var text = "\nQ: Missing answer\nQ: Complete\nA: Answer";
        var result = InvokeExtractFAQs(text);

        Assert.Single(result);
        Assert.Equal("Q: Complete", result[0].Key);
        Assert.Equal("Answer", result[0].Value);
    }

    private List<KeyValuePair<string, string>> InvokeExtractFAQs(string input)
    {
        var method = typeof(CompanyDataHelper).GetMethod("ExtractFAQs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        return (List<KeyValuePair<string, string>>)method.Invoke(null, [input])!;
    }
}