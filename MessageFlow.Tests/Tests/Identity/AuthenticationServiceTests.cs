//using Microsoft.AspNetCore.Identity;
//using MessageFlow.DataAccess.Models;
//using AutoMapper;
//using MessageFlow.DataAccess.Services;
//using MessageFlow.Server.Mappings;

//namespace MessageFlow.Tests.MessageFlowServer.Services
//{
//    public class AuthenticationServiceTests : IAsyncLifetime
//    {
//        private readonly IUnitOfWork _unitOfWork;
//        private readonly UserManager<ApplicationUser> _userManager;
//        private readonly SignInManager<ApplicationUser> _signInManager;
//        private readonly IMapper _mapper;

//        public AuthenticationServiceTests()
//        {
//            // ✅ Create UnitOfWork
//            _unitOfWork = TestDbContextFactory.CreateUnitOfWork("AuthenticationServiceTestsDb");

//            // ✅ Configure AutoMapper using MappingProfile
//            var mapperConfig = new MapperConfiguration(cfg =>
//            {
//                cfg.AddProfile<MappingProfile>();
//            });
//            _mapper = mapperConfig.CreateMapper();

//            // ✅ Initialize UserManager and SignInManager using TestHelper
//            _userManager = TestHelper.CreateUserManager(_unitOfWork);
//            _signInManager = TestHelper.CreateSignInManager(_userManager);
//        }

//        public async Task InitializeAsync()
//        {
//            // ✅ Reset the database
//            await _unitOfWork.Context.Database.EnsureDeletedAsync();
//            await _unitOfWork.Context.Database.EnsureCreatedAsync();

//            // ✅ Seed database with users, roles, and companies
//            var roleManager = TestHelper.CreateRoleManager(_unitOfWork);
//            await TestDatabaseSeeder.Seed(_unitOfWork, _mapper, _userManager, roleManager);
//        }

//        public async Task DisposeAsync()
//        {
//            // ✅ Clean up after tests
//            await _unitOfWork.Context.Database.EnsureDeletedAsync();
//            _unitOfWork.Dispose();
//        }

//        [Fact]
//        public async Task Login_ValidCredentials_ReturnsSuccess()
//        {
//            // Arrange
//            var email = "admin@companya.com"; // Updated to match seeded data
//            var password = "Admin@123";       // Updated to match seeded password
//            var user = await _userManager.FindByEmailAsync(email);

//            // Act
//            var result = await _signInManager.PasswordSignInAsync(user, password, false, true);

//            // Assert
//            Assert.True(result.Succeeded);
//        }

//        [Fact]
//        public async Task Login_InvalidCredentials_ReturnsFailed()
//        {
//            // Arrange
//            var email = "admin@companya.com"; // Updated to match seeded data
//            var password = "InvalidPassword";
//            var user = await _userManager.FindByEmailAsync(email);

//            // Act
//            var result = await _signInManager.PasswordSignInAsync(user, password, false, true);

//            // Assert
//            Assert.False(result.Succeeded);
//        }

//        [Fact]
//        public async Task Login_LockedOutAccount_ReturnsLockedOut()
//        {
//            // Arrange
//            var email = "manager@companya.com"; // Updated to match seeded data
//            var password = "Manager@123";       // Updated to match seeded password
//            var user = await _userManager.FindByEmailAsync(email);

//            // Enable lockout for the user
//            user.LockoutEnabled = true;
//            await _userManager.UpdateEntityAsync(user);

//            // Simulate failed login attempts to trigger lockout
//            for (int i = 0; i < 5; i++)
//            {
//                await _userManager.AccessFailedAsync(user);
//            }

//            // Reload the user to ensure the lockout state is updated
//            user = await _userManager.FindByEmailAsync(email);

//            // Act
//            var result = await _signInManager.PasswordSignInAsync(user, password, false, true);

//            // Assert
//            Assert.True(result.IsLockedOut);
//        }

//    }
//}
