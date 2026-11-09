using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AcsEmulatorAPI.Tests
{
    [TestClass]
    public class EmailTests
    {
        private readonly SqliteConnection _connection;
        private readonly WebApplicationFactory<Program> _factory;

        public EmailTests()
        {
            var connectionString = $"Data Source=email-tests-{Guid.NewGuid()};Mode=Memory;Cache=Shared";
            _connection = new SqliteConnection(connectionString);
            _connection.Open();
            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((context, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:EmulatorDb"] = connectionString,
                        ["DisableHttpsRedirection"] = "true"
                    })));
        }

        [TestCleanup]
        public void Cleanup()
        {
            _factory.Dispose();
            _connection.Dispose();
        }

        [DataTestMethod]
        [DataRow("http://localhost:8025")]
        [DataRow("https://localhost:8026")]
        [DataRow("http://127.0.0.1:8025")]
        [DataRow("http://[::1]:8025")]
        public async Task SendEmailReturnsPollingUrlWithRequestOrigin(string origin)
        {
            using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri(origin),
                AllowAutoRedirect = false
            });
            var operationId = Guid.NewGuid().ToString();
            using var request = new HttpRequestMessage(HttpMethod.Post, "/emails:send?api-version=2025-09-01")
            {
                Content = JsonContent.Create(new
                {
                    senderAddress = "sender@example.com",
                    content = new
                    {
                        subject = "Test",
                        plainText = "Hello",
                        html = "<p>Hello</p>"
                    },
                    recipients = new
                    {
                        to = new[] { new { address = "recipient@example.com" } }
                    }
                })
            };
            request.Headers.Add("Operation-Id", operationId);

            using var response = await client.SendAsync(request);

            Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
            var expectedLocation = $"{origin}/emails/operations/{operationId}";
            Assert.AreEqual(expectedLocation, response.Headers.Location?.AbsoluteUri);
            Assert.AreEqual(expectedLocation, response.Headers.GetValues("Operation-Location").Single());
            Assert.AreEqual(TimeSpan.FromSeconds(2), response.Headers.RetryAfter?.Delta);

            using var pollResponse = await client.GetAsync(expectedLocation);

            Assert.AreEqual(HttpStatusCode.OK, pollResponse.StatusCode);
            var result = await pollResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual(operationId, result.GetProperty("id").GetString());
            var status = result.GetProperty("status").GetString();
            Assert.IsTrue(status is "Running" or "Succeeded");
            if (status == "Running")
            {
                Assert.AreEqual(TimeSpan.FromSeconds(2), pollResponse.Headers.RetryAfter?.Delta);
            }
            else
            {
                Assert.IsNull(pollResponse.Headers.RetryAfter);
            }
        }
    }
}
