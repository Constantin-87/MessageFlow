//using AutoMapper;
//using MessageFlow.Tests.Helpers;
//using MessageFlow.DataAccess.Services;
//using MessageFlow.DataAccess.Models;
//using MessageFlow.Infrastructure.Mappings;
//using Microsoft.AspNetCore.Identity;
//using MessageFlow.Shared.DTOs;

//namespace MessageFlow.Tests.Server.Services
//{
//    public class TeamsManagementServiceTests : IAsyncLifetime
//    {
//        private readonly IUnitOfWork _unitOfWork;
//        private readonly IMapper _mapper;
//        private ApplicationUser _adminUser;
//        private ApplicationUser _superAdminUser;
//        private ApplicationUser _agentManagerUser;
//        private ApplicationUser _agentUser;
//        private UserManager<ApplicationUser> _userManager;

//        public TeamsManagementServiceTests()
//        {
//            var context = TestDbContextFactory.CreateDbContext("TeamsManagementServiceTestsDb");
//            _unitOfWork = TestDbContextFactory.CreateUnitOfWork(context);

//            var mapperConfig = new MapperConfiguration(cfg =>
//            {
//                cfg.AddProfile<MappingProfile>();
//            });
//            _mapper = mapperConfig.CreateMapper();
//        }

//        public async Task InitializeAsync()
//        {
//            await _unitOfWork.Context.Database.EnsureDeletedAsync();
//            await _unitOfWork.Context.Database.EnsureCreatedAsync();

//            _userManager = TestHelper.CreateUserManager(_unitOfWork);
//            var roleManager = TestHelper.CreateRoleManager(_unitOfWork);
//            await TestDatabaseSeeder.Seed(_unitOfWork, _mapper, _userManager, roleManager);

//            _adminUser = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync("admin@companya.com");
//            _superAdminUser = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync("superadmin@headcompany.com");
//            _agentManagerUser = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync("manager@companya.com");
//            _agentUser = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync("agent@companyb.com");
//        }

//        public async Task DisposeAsync()
//        {
//            await _unitOfWork.Context.Database.EnsureDeletedAsync();
//            _unitOfWork.Dispose();
//        }

//        #region Tests for GetTeamsForCompanyAsync
//        [Fact]
//        public async Task Admin_Can_Get_Teams_For_Own_Company()
//        {
//            // Arrange
//            var user = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync(_adminUser.UserName);
//            Assert.NotNull(user); // user exists in seed data

//            var roles = await _userManager.GetRolesAsync(user);
//            var userRole = roles.FirstOrDefault();

//            var service = TestHelper.CreateTeamsManagementService(_unitOfWork, _mapper, user.Id, userRole);

//            // Act
//            var teams = await service.GetTeamsForCompanyAsync(user.CompanyId);

//            // Assert
//            Assert.NotNull(teams);
//            Assert.All(teams, t => Assert.Equal(user.CompanyId, t.CompanyId));
//        }

//        [Fact]
//        public async Task SuperAdmin_Can_Get_Teams_For_Own_Company()
//        {
//            var user = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync(_superAdminUser.UserName);

//            var roles = await _userManager.GetRolesAsync(user);
//            var userRole = roles.FirstOrDefault();

//            var service = TestHelper.CreateTeamsManagementService(_unitOfWork, _mapper, user.Id, userRole);

//            var teams = await service.GetTeamsForCompanyAsync(user.CompanyId);

//            Assert.NotNull(teams);
//            Assert.All(teams, t => Assert.Equal(user.CompanyId, t.CompanyId));
//        }

//        [Fact]
//        public async Task Admin_Cannot_Get_Teams_For_Other_Company()
//        {
//            var user = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync(_adminUser.UserName);
//            Assert.NotNull(user);

//            var roles = await _userManager.GetRolesAsync(user);
//            var userRole = roles.FirstOrDefault();

//            var service = TestHelper.CreateTeamsManagementService(_unitOfWork, _mapper, user.Id, userRole);

//            // Try accessing Company B’s teams (not admin’s company)
//            var teams = await service.GetTeamsForCompanyAsync("3");

//            Assert.Empty(teams);
//        }

//        [Fact]
//        public async Task SuperAdmin_Cannot_Get_Teams_For_Other_Company()
//        {
//            var user = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync(_superAdminUser.UserName);
//            Assert.NotNull(user);

//            var roles = await _userManager.GetRolesAsync(user);
//            var userRole = roles.FirstOrDefault();

//            var service = TestHelper.CreateTeamsManagementService(_unitOfWork, _mapper, user.Id, userRole);

//            var teams = await service.GetTeamsForCompanyAsync("3");

//            Assert.Empty(teams);
//        }
//        #endregion

//        #region Tests for GetUsersForTeamAsync
//        [Fact]
//        public async Task Admin_Can_Get_Users_For_Team_In_Own_Company()
//        {
//            var user = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync(_adminUser.UserName);
//            var role = (await _userManager.GetRolesAsync(user)).First();
//            var service = TestHelper.CreateTeamsManagementService(_unitOfWork, _mapper, user.Id, role);

//            var team = (await _unitOfWork.Teams.GetTeamsByCompanyIdAsync(user.CompanyId)).First();

//            var users = await service.GetUsersForTeamAsync(team.Id);

//            Assert.NotNull(users);
//        }

//        [Fact]
//        public async Task Admin_Cannot_Get_Users_For_Team_In_Other_Company()
//        {
//            var user = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync(_adminUser.UserName);
//            var role = (await _userManager.GetRolesAsync(user)).First();
//            var service = TestHelper.CreateTeamsManagementService(_unitOfWork, _mapper, user.Id, role);

//            var team = (await _unitOfWork.Teams.GetTeamsByCompanyIdAsync("3")).First(); // Company B

//            var users = await service.GetUsersForTeamAsync(team.Id);

//            Assert.Empty(users);
//        }

//        [Fact]
//        public async Task SuperAdmin_Can_Get_Users_For_Team_In_Own_Company()
//        {
//            var user = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync(_superAdminUser.UserName);
//            var role = (await _userManager.GetRolesAsync(user)).First();
//            var service = TestHelper.CreateTeamsManagementService(_unitOfWork, _mapper, user.Id, role);

//            var team = (await _unitOfWork.Teams.GetTeamsByCompanyIdAsync(user.CompanyId)).First();

//            var users = await service.GetUsersForTeamAsync(team.Id);

//            Assert.NotNull(users);
//        }

//        [Fact]
//        public async Task SuperAdmin_Cannot_Get_Users_For_Team_In_Other_Company()
//        {
//            var user = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync(_superAdminUser.UserName);
//            var role = (await _userManager.GetRolesAsync(user)).First();
//            var service = TestHelper.CreateTeamsManagementService(_unitOfWork, _mapper, user.Id, role);

//            var team = (await _unitOfWork.Teams.GetTeamsByCompanyIdAsync("3")).First(); // Company B

//            var users = await service.GetUsersForTeamAsync(team.Id);

//            Assert.Empty(users);
//        }
//        #endregion

//        #region Tests for AddTeamToCompanyAsync
//        [Fact]
//        public async Task Admin_Can_Add_Team_To_Own_Company()
//        {
//            var user = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync(_adminUser.UserName);
//            var role = (await _userManager.GetRolesAsync(user)).First();
//            var service = TestHelper.CreateTeamsManagementService(_unitOfWork, _mapper, user.Id, role);

//            var teamDto = new TeamDTO
//            {
//                TeamName = "Admin Test Team",
//                TeamDescription = "Created by Admin",
//                CompanyId = user.CompanyId
//            };

//            var (success, message) = await service.AddTeamToCompanyAsync(teamDto);

//            Assert.True(success);
//            Assert.Equal("Team added successfully.", message);
//        }

//        [Fact]
//        public async Task Admin_Cannot_Add_Team_To_Other_Company()
//        {
//            var user = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync(_adminUser.UserName);
//            var role = (await _userManager.GetRolesAsync(user)).First();
//            var service = TestHelper.CreateTeamsManagementService(_unitOfWork, _mapper, user.Id, role);

//            var teamDto = new TeamDTO
//            {
//                TeamName = "Invalid Team",
//                TeamDescription = "Should be blocked",
//                CompanyId = "3" // Company B
//            };

//            var (success, message) = await service.AddTeamToCompanyAsync(teamDto);

//            Assert.False(success);
//            Assert.Equal("Unauthorized: Cannot manage Teams for other Companies.", message);
//        }

//        [Fact]
//        public async Task SuperAdmin_Can_Add_Team_To_Own_Company()
//        {
//            var user = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync(_superAdminUser.UserName);
//            var role = (await _userManager.GetRolesAsync(user)).First();
//            var service = TestHelper.CreateTeamsManagementService(_unitOfWork, _mapper, user.Id, role);

//            var teamDto = new TeamDTO
//            {
//                TeamName = "SuperAdmin HQ Team",
//                TeamDescription = "Created by SuperAdmin",
//                CompanyId = user.CompanyId
//            };

//            var (success, message) = await service.AddTeamToCompanyAsync(teamDto);

//            Assert.True(success);
//            Assert.Equal("Team added successfully.", message);
//        }

//        [Fact]
//        public async Task SuperAdmin_Cannot_Add_Team_To_Other_Company()
//        {
//            var user = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync(_superAdminUser.UserName);
//            var role = (await _userManager.GetRolesAsync(user)).First();
//            var service = TestHelper.CreateTeamsManagementService(_unitOfWork, _mapper, user.Id, role);

//            var teamDto = new TeamDTO
//            {
//                TeamName = "Unauthorized SuperAdmin Team",
//                TeamDescription = "Should fail",
//                CompanyId = "2"
//            };

//            var (success, message) = await service.AddTeamToCompanyAsync(teamDto);

//            Assert.False(success);
//            Assert.Equal("Unauthorized: Cannot manage Teams for other Companies.", message);
//        }
//        #endregion

//        #region Tests for DeleteTeamByIdAsync
//        [Fact]
//        public async Task Admin_Can_Delete_Team_In_Own_Company()
//        {
//            var user = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync(_adminUser.UserName);
//            var role = (await _userManager.GetRolesAsync(user)).First();
//            var service = TestHelper.CreateTeamsManagementService(_unitOfWork, _mapper, user.Id, role);

//            var teamId = (await _unitOfWork.Teams.GetTeamsByCompanyIdAsync(user.CompanyId)).First().Id;

//            var (success, message) = await service.DeleteTeamByIdAsync(teamId);

//            Assert.True(success);
//            Assert.Equal("Team deleted successfully.", message);
//        }

//        [Fact]
//        public async Task Admin_Cannot_Delete_Team_In_Other_Company()
//        {
//            var user = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync(_adminUser.UserName);
//            var role = (await _userManager.GetRolesAsync(user)).First();
//            var service = TestHelper.CreateTeamsManagementService(_unitOfWork, _mapper, user.Id, role);

//            var teamId = (await _unitOfWork.Teams.GetTeamsByCompanyIdAsync("3")).First().Id;

//            var (success, message) = await service.DeleteTeamByIdAsync(teamId);

//            Assert.False(success);
//            Assert.Equal("Unauthorized: Cannot manage Teams for other Companies.", message);
//        }

//        [Fact]
//        public async Task SuperAdmin_Can_Delete_Team_In_Own_Company()
//        {
//            var user = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync(_superAdminUser.UserName);
//            var role = (await _userManager.GetRolesAsync(user)).First();
//            var service = TestHelper.CreateTeamsManagementService(_unitOfWork, _mapper, user.Id, role);

//            var teamId = (await _unitOfWork.Teams.GetTeamsByCompanyIdAsync(user.CompanyId)).First().Id;

//            var (success, message) = await service.DeleteTeamByIdAsync(teamId);

//            Assert.True(success);
//            Assert.Equal("Team deleted successfully.", message);
//        }

//        [Fact]
//        public async Task SuperAdmin_Cannot_Delete_Team_In_Other_Company()
//        {
//            var user = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync(_superAdminUser.UserName);
//            var role = (await _userManager.GetRolesAsync(user)).First();
//            var service = TestHelper.CreateTeamsManagementService(_unitOfWork, _mapper, user.Id, role);

//            var teamId = (await _unitOfWork.Teams.GetTeamsByCompanyIdAsync("2")).First().Id;

//            var (success, message) = await service.DeleteTeamByIdAsync(teamId);

//            Assert.False(success);
//            Assert.Equal("Unauthorized: Cannot manage Teams for other Companies.", message);
//        }
//        #endregion

//        #region Tests for DeleteTeamsByCompanyIdAsync
//        [Fact]
//        public async Task SuperAdmin_Can_Delete_All_Teams_For_Own_Company()
//        {
//            var user = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync(_superAdminUser.UserName);
//            var role = (await _userManager.GetRolesAsync(user)).First();
//            var service = TestHelper.CreateTeamsManagementService(_unitOfWork, _mapper, user.Id, role);

//            var companyId = user.CompanyId;

//            // Act
//            await service.DeleteTeamsByCompanyIdAsync(companyId);

//            var remainingTeams = await _unitOfWork.Teams.GetTeamsByCompanyIdAsync(companyId);

//            // Assert
//            Assert.Empty(remainingTeams);
//        }

//        [Fact]
//        public async Task SuperAdmin_Cannot_Delete_Teams_For_Other_Company()
//        {
//            var user = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync(_superAdminUser.UserName);
//            var role = (await _userManager.GetRolesAsync(user)).First();
//            var service = TestHelper.CreateTeamsManagementService(_unitOfWork, _mapper, user.Id, role);

//            var companyId = "2"; // Company A

//            // Act
//            await service.DeleteTeamsByCompanyIdAsync(companyId);

//            var remainingTeams = await _unitOfWork.Teams.GetTeamsByCompanyIdAsync(companyId);

//            // Assert
//            Assert.NotEmpty(remainingTeams);
//        }
//        #endregion

//        #region Tests for UpdateTeamAsync
//        [Fact]
//        public async Task Admin_Can_Update_Team_In_Own_Company()
//        {
//            var user = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync(_adminUser.UserName);
//            var role = (await _userManager.GetRolesAsync(user)).First();
//            var service = TestHelper.CreateTeamsManagementService(_unitOfWork, _mapper, user.Id, role);

//            var team = (await _unitOfWork.Teams.GetTeamsByCompanyIdAsync(user.CompanyId)).First();

//            var teamDto = new TeamDTO
//            {
//                Id = team.Id,
//                CompanyId = user.CompanyId,
//                TeamName = "Updated Name",
//                TeamDescription = "Updated Description"
//            };

//            var (success, message) = await service.UpdateTeamAsync(teamDto);

//            Assert.True(success);
//            Assert.Equal("Team updated successfully.", message);
//        }

//        [Fact]
//        public async Task SuperAdmin_Can_Update_Team_In_Own_Company()
//        {
//            var user = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync(_superAdminUser.UserName);
//            var role = (await _userManager.GetRolesAsync(user)).First();
//            var service = TestHelper.CreateTeamsManagementService(_unitOfWork, _mapper, user.Id, role);

//            var team = (await _unitOfWork.Teams.GetTeamsByCompanyIdAsync(user.CompanyId)).First();

//            var teamDto = new TeamDTO
//            {
//                Id = team.Id,
//                CompanyId = user.CompanyId,
//                TeamName = "Updated HQ Team",
//                TeamDescription = "Updated HQ Description"
//            };

//            var (success, message) = await service.UpdateTeamAsync(teamDto);

//            Assert.True(success);
//            Assert.Equal("Team updated successfully.", message);
//        }

//        [Fact]
//        public async Task Admin_Cannot_Update_Team_In_Other_Company()
//        {
//            var user = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync(_adminUser.UserName);
//            var role = (await _userManager.GetRolesAsync(user)).First();
//            var service = TestHelper.CreateTeamsManagementService(_unitOfWork, _mapper, user.Id, role);

//            var team = (await _unitOfWork.Teams.GetTeamsByCompanyIdAsync("3")).First();

//            var teamDto = new TeamDTO
//            {
//                Id = team.Id,
//                CompanyId = "3",
//                TeamName = "Should Fail",
//                TeamDescription = "Blocked"
//            };

//            var (success, message) = await service.UpdateTeamAsync(teamDto);

//            Assert.False(success);
//            Assert.Equal("Unauthorized: Cannot manage Teams for other Companies.", message);
//        }

//        [Fact]
//        public async Task SuperAdmin_Cannot_Update_Team_In_Other_Company()
//        {
//            var user = await _unitOfWork.ApplicationUsers.GetUserByUsernameAsync(_superAdminUser.UserName);
//            var role = (await _userManager.GetRolesAsync(user)).First();
//            var service = TestHelper.CreateTeamsManagementService(_unitOfWork, _mapper, user.Id, role);

//            var team = (await _unitOfWork.Teams.GetTeamsByCompanyIdAsync("2")).First();

//            var teamDto = new TeamDTO
//            {
//                Id = team.Id,
//                CompanyId = "2",
//                TeamName = "Not Allowed",
//                TeamDescription = "Blocked"
//            };

//            var (success, message) = await service.UpdateTeamAsync(teamDto);

//            Assert.False(success);
//            Assert.Equal("Unauthorized: Cannot manage Teams for other Companies.", message);
//        }
//        #endregion


//    }
//}
