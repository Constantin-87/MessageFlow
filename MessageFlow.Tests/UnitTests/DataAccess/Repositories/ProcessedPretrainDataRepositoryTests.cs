using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Implementations;
using MessageFlow.DataAccess.Models;
using MessageFlow.Shared.Enums;
using MessageFlow.Tests.Helpers.Factories;

namespace MessageFlow.Tests.UnitTests.DataAccess.Repositories;

public class ProcessedPretrainDataRepositoryTests
{
    private ApplicationDbContext CreateContext() => UnitTestFactory.CreateInMemoryDbContext();

    [Fact]
    public async Task GetProcessedFilesByCompanyIdAsync_ReturnsMatchingFiles()
    {
        using var context = CreateContext();
        var companyId = "c1";

        context.ProcessedPretrainData.AddRange(
            new ProcessedPretrainData { CompanyId = companyId, FileDescription = "desc1", FileType = FileType.CsvFile, FileUrl = "url1" },
            new ProcessedPretrainData { CompanyId = "c2", FileDescription = "desc2", FileType = FileType.Other, FileUrl = "url2" }
        );
        await context.SaveChangesAsync();

        var repo = new ProcessedPretrainDataRepository(context);
        var result = await repo.GetProcessedFilesByCompanyIdAsync(companyId);

        Assert.Single(result);
        Assert.Equal("desc1", result[0].FileDescription);
    }

    [Fact]
    public async Task GetProcessedFilesByCompanyIdAndTypesAsync_ReturnsFilteredFiles()
    {
        using var context = CreateContext();
        var companyId = "c1";

        context.ProcessedPretrainData.AddRange(
            new ProcessedPretrainData
            {
                CompanyId = companyId,
                FileType = FileType.FAQFile,
                FileDescription = "FAQ 1",
                FileUrl = "url1"
            },
            new ProcessedPretrainData
            {
                CompanyId = companyId,
                FileType = FileType.ExcelFile,
                FileDescription = "Excel 1",
                FileUrl = "url2"
            },
            new ProcessedPretrainData
            {
                CompanyId = "c2",
                FileType = FileType.CsvFile,
                FileDescription = "CSV External",
                FileUrl = "url3"
            }
        );
        await context.SaveChangesAsync();

        var repo = new ProcessedPretrainDataRepository(context);
        var result = await repo.GetProcessedFilesByCompanyIdAndTypesAsync(companyId, new List<FileType> { FileType.FAQFile });

        Assert.Single(result);
        Assert.Equal(FileType.FAQFile, result[0].FileType);
    }

    [Fact]
    public async Task AddProcessedFilesAsync_AddsFilesCorrectly()
    {
        using var context = CreateContext();
        var repo = new ProcessedPretrainDataRepository(context);

        var files = new List<ProcessedPretrainData>
        {
            new() { CompanyId = "c1", FileDescription = "file1", FileType = FileType.CsvFile, FileUrl = "url1" },
            new() { CompanyId = "c1", FileDescription = "file2", FileType = FileType.ExcelFile, FileUrl = "url2" }
        };

        await repo.AddProcessedFilesAsync(files);
        await context.SaveChangesAsync();

        var saved = context.ProcessedPretrainData.ToList();
        Assert.Equal(2, saved.Count);
    }

    [Fact]
    public async Task RemoveProcessedFiles_RemovesCorrectly()
    {
        using var context = CreateContext();
        var toDelete = new ProcessedPretrainData { CompanyId = "c1", FileDescription = "toDelete", FileType = FileType.Other, FileUrl = "url" };
        context.ProcessedPretrainData.Add(toDelete);
        await context.SaveChangesAsync();

        var repo = new ProcessedPretrainDataRepository(context);
        repo.RemoveProcessedFiles(new List<ProcessedPretrainData> { toDelete });
        await context.SaveChangesAsync();

        var all = context.ProcessedPretrainData.ToList();
        Assert.Empty(all);
    }

    [Fact]
    public async Task GetByIdStringAsync_ReturnsCorrectFile()
    {
        using var context = CreateContext();
        var file = new ProcessedPretrainData
        {
            Id = "f1",
            CompanyId = "c1",
            FileUrl = "url",
            FileType = FileType.Other,
            FileDescription = "Test file"
        };

        context.ProcessedPretrainData.Add(file);
        await context.SaveChangesAsync();

        var repo = new ProcessedPretrainDataRepository(context);
        var result = await repo.GetByIdStringAsync("f1");

        Assert.NotNull(result);
        Assert.Equal("f1", result!.Id);
    }
}