//using MessageFlow.DataAccess.Models;
//using MessageFlow.Server.Services;
//using AutoMapper;
//using MessageFlow.DataAccess.Services;
//using MessageFlow.Shared.DTOs;
//using MessageFlow.Infrastructure.Mappings;
//using MessageFlow.Tests.Helpers;

//namespace MessageFlow.Tests.Server.Services
//{
//    public class CompanyManagementServiceTests : IAsyncLifetime
//    {
//        private readonly IUnitOfWork _unitOfWork;
//        private readonly IMapper _mapper;
//        private CompanyManagementService _companyManagementServiceAdmin;
//        private CompanyManagementService _companyManagementServiceSuperAdmin;

//        public CompanyManagementServiceTests()
//        {
//            // Create UnitOfWork
//            var context = TestDbContextFactory.CreateDbContext("CompanyManagementServiceTestsDb");
//            _unitOfWork = TestDbContextFactory.CreateUnitOfWork(context);

//            // Configure AutoMapper with MappingProfile
//            var mapperConfig = new MapperConfiguration(cfg =>
//            {
//                cfg.AddProfile<MappingProfile>();
//            });
//            _mapper = mapperConfig.CreateMapper();
//        }

//        public async Task InitializeAsync()
//        {
//            // Reset the database
//            await _unitOfWork.Context.Database.EnsureDeletedAsync();
//            await _unitOfWork.Context.Database.EnsureCreatedAsync();

//            // Seed database with roles, users, and companies
//            var userManager = TestHelper.CreateUserManager(_unitOfWork);
//            var roleManager = TestHelper.CreateRoleManager(_unitOfWork);
//            await TestDatabaseSeeder.Seed(_unitOfWork, _mapper, userManager, roleManager);

//            // Create CompanyManagementService for Admin and SuperAdmin
//            _companyManagementServiceAdmin = TestHelper.CreateCompanyManagementService(_unitOfWork, _mapper, "3", "Admin");         // Admin of Company A
//            _companyManagementServiceSuperAdmin = TestHelper.CreateCompanyManagementService(_unitOfWork, _mapper, "1", "SuperAdmin"); // SuperAdmin of HeadCompany
//        }
//        public async Task DisposeAsync()
//        {
//            await _unitOfWork.Context.Database.EnsureDeletedAsync();
//            _unitOfWork.Dispose();
//        }

//        [Fact]
//        public async Task SuperAdmin_Can_Create_Company()
//        {
//            // Arrange
//            var newCompany = new Company
//            {
//                CompanyName = "NewCompany",
//                AccountNumber = "NEW-123",
//                Description = "Test company description",
//                IndustryType = "Software",
//                WebsiteUrl = "https://newcompany.test"
//            };

//            var newCompanyDTO = _mapper.Map<CompanyDTO>(newCompany); 

//            // Act
//            var result = await _companyManagementServiceSuperAdmin.CreateCompanyAsync(newCompanyDTO);

//            // Assert
//            Assert.True(result.success);
//            Assert.Equal("Company created successfully", result.errorMessage);

//            var companies = await _unitOfWork.Companies.GetAllAsync();
//            Assert.NotNull(companies.FirstOrDefault(c => c.CompanyName == "NewCompany"));
//        }

//        [Fact]
//        public async Task Admin_Cannot_Create_Company()
//        {
//            // Arrange
//            var newCompany = new Company
//            {
//                CompanyName = "NewCompany",
//                AccountNumber = "NEW-123"
//            };

//            var newCompanyDTO = _mapper.Map<CompanyDTO>(newCompany);

//            // Act
//            var result = await _companyManagementServiceAdmin.CreateCompanyAsync(newCompanyDTO);

//            // Assert
//            Assert.False(result.success);
//            Assert.Equal("Only SuperAdmins can create companies.", result.errorMessage);
//            var companies = await _unitOfWork.Companies.GetAllAsync();
//            Assert.Null(companies.FirstOrDefault(c => c.CompanyName == "NewCompany"));
//        }

//        [Fact]
//        public async Task SuperAdmin_Can_Delete_Company()
//        {
//            // Arrange
//            var companyToDelete = (await _unitOfWork.Companies.GetAllAsync())
//                .First(c => c.CompanyName == "Company A");

//            // Act
//            var result = await _companyManagementServiceSuperAdmin.DeleteCompanyAsync(companyToDelete.Id);

//            // Assert
//            Assert.True(result.success);
//            Assert.Equal("Company and all associated data deleted successfully.", result.errorMessage);
//            Assert.Null(await _unitOfWork.Companies.GetByIdStringAsync(companyToDelete.Id));
//        }

//        [Fact]
//        public async Task Admin_Cannot_Delete_Company()
//        {
//            // Arrange
//            var companyToDelete = (await _unitOfWork.Companies.GetAllAsync())
//                .First(c => c.CompanyName == "Company A");

//            // Act
//            var result = await _companyManagementServiceAdmin.DeleteCompanyAsync(companyToDelete.Id);

//            // Assert
//            Assert.False(result.success);
//            Assert.Equal("Only SuperAdmins can delete companies.", result.errorMessage);
//            Assert.NotNull(await _unitOfWork.Companies.GetByIdStringAsync(companyToDelete.Id));
//        }

//        [Fact]
//        public async Task Admin_Can_Update_Own_Company_Details()
//        {
//            // Arrange
//            var adminCompany = (await _unitOfWork.Companies.GetAllAsync())
//                .First(c => c.CompanyName == "Company A");
//            adminCompany.CompanyName = "Updated Company A";

//            var adminCompanyDTO = _mapper.Map<CompanyDTO>(adminCompany);
//            // Act
//            var result = await _companyManagementServiceAdmin.UpdateCompanyDetailsAsync(adminCompanyDTO);

//            // Assert
//            Assert.True(result.success);
//            Assert.Equal("Company details updated successfully.", result.errorMessage);

//            var updatedCompany = await _unitOfWork.Companies.GetByIdStringAsync(adminCompany.Id);
//            Assert.Equal("Updated Company A", updatedCompany.CompanyName);
//        }

//        [Fact]
//        public async Task Admin_Cannot_Update_Other_Company_Details()
//        {
//            // Arrange
//            var otherCompany = (await _unitOfWork.Companies.GetAllAsync())
//                .First(c => c.CompanyName == "Company B");
//            var originalName = otherCompany.CompanyName;

//            var otherCompanyDTO = _mapper.Map<CompanyDTO>(otherCompany);
//            otherCompanyDTO.CompanyName = "Updated Company B";

//            // Act
//            var result = await _companyManagementServiceAdmin.UpdateCompanyDetailsAsync(otherCompanyDTO);

//            var unchangedCompany = await _unitOfWork.Companies.GetByIdStringAsync(otherCompany.Id);

//            // Assert
//            Assert.False(result.success);
//            Assert.Equal("Unauthorized for this company.", result.errorMessage);
//            Assert.Equal(originalName, unchangedCompany.CompanyName);
//        }


//        [Fact]
//        public async Task SuperAdmin_Can_Update_Own_Company_Details()
//        {
//            // Arrange
//            var superAdminCompany = (await _unitOfWork.Companies.GetAllAsync())
//                .First(c => c.CompanyName == "HeadCompany");
//            superAdminCompany.CompanyName = "Updated HeadCompany";

//            var superAdminCompanyDTO = _mapper.Map<CompanyDTO>(superAdminCompany);
//            // Act
//            var result = await _companyManagementServiceSuperAdmin.UpdateCompanyDetailsAsync(superAdminCompanyDTO);

//            // Assert
//            Assert.True(result.success);
//            Assert.Equal("Company details updated successfully.", result.errorMessage);

//            var updatedCompany = await _unitOfWork.Companies.GetByIdStringAsync(superAdminCompany.Id);
//            Assert.Equal("Updated HeadCompany", updatedCompany.CompanyName);
//        }

//        [Fact]
//        public async Task SuperAdmin_Can_Update_Other_Company_Details()
//        {
//            // Arrange
//            var otherCompany = (await _unitOfWork.Companies.GetAllAsync())
//               .First(c => c.CompanyName == "Company B");
//            otherCompany.CompanyName = "Updated Company B";

//            var otherCompanyDTO = _mapper.Map<CompanyDTO>(otherCompany);
//            // Act
//            var result = await _companyManagementServiceSuperAdmin.UpdateCompanyDetailsAsync(otherCompanyDTO);

//            // Assert
//            Assert.True(result.success);
//            Assert.Equal("Company details updated successfully.", result.errorMessage);

//            var updatedCompany = await _unitOfWork.Companies.GetByIdStringAsync(otherCompany.Id);
//            Assert.Equal("Updated Company B", updatedCompany.CompanyName);
//        }
//    }
//}
