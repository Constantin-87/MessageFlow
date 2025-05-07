using Moq;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using MessageFlow.Server.Controllers;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.MediatR.CompanyManagement.Queries;
using MessageFlow.Server.MediatR.CompanyManagement.Commands;

namespace MessageFlow.Tests.UnitTests.Server.Controllers;

public class CompanyControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ILogger<CompanyController>> _loggerMock = new();
    private readonly CompanyController _controller;

    public CompanyControllerTests()
    {
        _controller = new CompanyController(_mediatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllCompanies_ReturnsOk()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllCompaniesQuery>(), default))
            .ReturnsAsync(new List<CompanyDTO> { new() { Id = "c1" } });

        var result = await _controller.GetAllCompanies();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Single((List<CompanyDTO>)ok.Value!);
    }

    [Fact]
    public async Task GetCompanyById_ReturnsNotFound_WhenNull()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCompanyByIdQuery>(), default))
            .ReturnsAsync((CompanyDTO?)null);

        var result = await _controller.GetCompanyById("c1");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateCompany_ReturnsBadRequest_WhenFailed()
    {
        var dto = new CompanyDTO();
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateCompanyCommand>(), default))
            .ReturnsAsync((false, "fail"));

        var result = await _controller.CreateCompany(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateCompany_ReturnsOk_WhenSuccess()
    {
        var dto = new CompanyDTO();
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateCompanyDetailsCommand>(), default))
            .ReturnsAsync((true, "updated"));

        var result = await _controller.UpdateCompany(dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("updated", ok.Value);
    }

    [Fact]
    public async Task GetCompanyForUser_ReturnsNotFound_WhenNull()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCompanyForUserQuery>(), default))
            .ReturnsAsync((CompanyDTO?)null);

        var result = await _controller.GetCompanyForUser();

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateCompanyEmails_ReturnsBadRequest_WhenFailed()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateCompanyEmailsCommand>(), default))
            .ReturnsAsync((false, "error"));

        var result = await _controller.UpdateCompanyEmails(new List<CompanyEmailDTO>());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetCompanyMetadata_ReturnsBadRequest_WhenFailed()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCompanyMetadataQuery>(), default))
            .ReturnsAsync((false, null!, "fail"));

        var result = await _controller.GetCompanyMetadata("c1");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("fail", badRequest.Value);
    }

    [Fact]
    public async Task DeleteFile_ReturnsOk_WhenSuccess()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteCompanyFileCommand>(), default))
            .ReturnsAsync(true);

        var result = await _controller.DeleteFile(new ProcessedPretrainDataDTO());

        Assert.IsType<OkObjectResult>(result);
    }
}