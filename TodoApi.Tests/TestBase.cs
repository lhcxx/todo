using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR.Client;
using TodoApi.Data;
using TodoApi.Models;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace TodoApi.Tests
{
    public abstract class TestBase : IDisposable
    {
        protected WebApplicationFactory<Program> Factory { get; }
        protected AppDbContext DbContext { get; }
        protected HttpClient Client { get; }
        private IServiceScope _scope;

        protected TestBase()
        {
            Factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        // Replace the database with in-memory database
                        var descriptor = services.SingleOrDefault(
                            d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                        if (descriptor != null)
                        {
                            services.Remove(descriptor);
                        }

                        services.AddDbContext<AppDbContext>(options =>
                        {
                            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                        });
                    });
                });

            Client = Factory.CreateClient();
            
            // Get the DbContext from the factory and seed data
            _scope = Factory.Services.CreateScope();
            DbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Ensure the database is created
            DbContext.Database.EnsureCreated();

            // Seed test data
            SeedTestData(DbContext);
        }

        protected virtual void SeedTestData(AppDbContext context)
        {
            // Create test users
            var user1 = new User { Username = "testuser1", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123") };
            var user2 = new User { Username = "testuser2", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123") };
            
            context.Users.AddRange(user1, user2);
            context.SaveChanges();

            // Create test team
            var team = new Team { Name = "Test Team", Description = "Test Team Description", OwnerId = user1.Id };
            context.Teams.Add(team);
            context.SaveChanges();

            // Add team members
            var teamMember1 = new TeamMember { TeamId = team.Id, UserId = user1.Id, Role = TeamRole.Owner };
            var teamMember2 = new TeamMember { TeamId = team.Id, UserId = user2.Id, Role = TeamRole.Member };
            context.TeamMembers.AddRange(teamMember1, teamMember2);
            context.SaveChanges();
        }

        protected string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSuperSecretKeyThatIsAtLeast32CharactersLongForHS256Algorithm"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        protected async Task<SignalRClient> CreateSignalRClientAsync(string? token = null)
        {
            var hubUrl = new Uri(Factory.Server.BaseAddress, "/todohub");
            var connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    if (!string.IsNullOrEmpty(token))
                    {
                        options.AccessTokenProvider = () => Task.FromResult(token);
                    }
                    // Use the test server's HTTP client
                    options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
                })
                .Build();

            await connection.StartAsync();
            return new SignalRClient(connection);
        }

        protected async Task<User> GetTestUserAsync(string username = "testuser1")
        {
            return await DbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        protected async Task<Team> GetTestTeamAsync()
        {
            return await DbContext.Teams.FirstOrDefaultAsync();
        }

        public void Dispose()
        {
            Client?.Dispose();
            _scope?.Dispose();
            Factory?.Dispose();
        }
    }

    public class SignalRClient : IDisposable
    {
        public HubConnection Connection { get; }
        public List<object> ReceivedMessages { get; } = new();

        public SignalRClient(HubConnection connection)
        {
            Connection = connection;
        }

        public async Task<T> WaitForMessageAsync<T>(string methodName, TimeSpan timeout = default)
        {
            if (timeout == default)
                timeout = TimeSpan.FromSeconds(10);

            var tcs = new TaskCompletionSource<T>();
            var cts = new CancellationTokenSource(timeout);

            IDisposable handler = null;
            handler = Connection.On<T>(methodName, message =>
            {
                tcs.TrySetResult(message);
                handler?.Dispose();
            });

            cts.Token.Register(() => tcs.TrySetCanceled());

            try
            {
                return await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Timeout waiting for SignalR message: {methodName}");
            }
        }

        public async Task<List<T>> WaitForMultipleMessagesAsync<T>(string methodName, int count, TimeSpan timeout = default)
        {
            if (timeout == default)
                timeout = TimeSpan.FromSeconds(10);

            var messages = new List<T>();
            var tcs = new TaskCompletionSource<List<T>>();
            var cts = new CancellationTokenSource(timeout);

            IDisposable handler = null;
            handler = Connection.On<T>(methodName, message =>
            {
                messages.Add(message);
                if (messages.Count >= count)
                {
                    tcs.TrySetResult(messages);
                    handler?.Dispose();
                }
            });

            cts.Token.Register(() => tcs.TrySetCanceled());

            try
            {
                return await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Timeout waiting for {count} SignalR messages: {methodName}");
            }
        }

        public void Dispose()
        {
            Connection?.DisposeAsync();
        }
    }
} 