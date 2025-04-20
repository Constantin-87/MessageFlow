using AutoMapper;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Infrastructure.Mappings;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatorComponents.CompanyManagement.QueryHandlers;
using MessageFlow.Server.MediatorComponents.CompanyManagement.Queries;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageFlow.Tests.Tests.Server.CompanyManagement.Queries;

public class GetCompanyPretrainingFilesQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IAuthorizationHelper> _authHelperMock = new();
    private readonly Mock<ILogger<GetCompanyPretrainingFilesQueryHandler>> _loggerMock = new();
    private readonly IMapper _mapper;

    public GetCompanyPretrainingFilesQueryHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
    }

    [Fact]
    public async Task Handle_AuthorizedWithFiles_ReturnsSuccess()
    {
        var companyId = "c1";
        var handler = new GetCompanyPretrainingFilesQueryHandler(
            _unitOfWorkMock.Object, _mapper, _loggerMock.Object, _authHelperMock.Object);

        _authHelperMock.Setup(x => x.CompanyAccess(companyId)).ReturnsAsync((true, null, false, ""));
        _unitOfWorkMock.Setup(u => u.ProcessedPretrainData.GetProcessedFilesByCompanyIdAsync(companyId))
            .ReturnsAsync(new List<ProcessedPretrainData>
            {
                new() { Id = "f1", CompanyId = companyId }
            });

        var result = await handler.Handle(new GetCompanyPretrainingFilesQuery(companyId), default);

        Assert.True(result.success);
        Assert.Single(result.files);
        Assert.Equal("Files retrieved successfully.", result.errorMessage);
    }

    [Fact]
    public async Task Handle_Unauthorized_ReturnsError()
    {
        var handler = new GetCompanyPretrainingFilesQueryHandler(
            _unitOfWorkMock.Object, _mapper, _loggerMock.Object, _authHelperMock.Object);

        _authHelperMock.Setup(x => x.CompanyAccess("unauth")).ReturnsAsync((false, null, false, "Access denied"));

        var result = await handler.Handle(new GetCompanyPretrainingFilesQuery("unauth"), default);

        Assert.False(result.success);
        Assert.Equal("Access denied", result.errorMessage);
        Assert.Empty(result.files);
    }

    [Fact]
    public async Task Handle_NoFilesFound_ReturnsError()
    {
        var companyId = "empty";
        var handler = new GetCompanyPretrainingFilesQueryHandler(
            _unitOfWorkMock.Object, _mapper, _loggerMock.Object, _authHelperMock.Object);

        _authHelperMock.Setup(x => x.CompanyAccess(companyId)).ReturnsAsync((true, null, false, ""));
        _unitOfWorkMock.Setup(u => u.ProcessedPretrainData.GetProcessedFilesByCompanyIdAsync(companyId))
            .ReturnsAsync(new List<ProcessedPretrainData>());

        var result = await handler.Handle(new GetCompanyPretrainingFilesQuery(companyId), default);

        Assert.False(result.success);
        Assert.Equal("No pretraining files found for this company.", result.errorMessage);
    }
}
