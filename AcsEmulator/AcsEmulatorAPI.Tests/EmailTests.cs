using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;
using System.Text;
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
        [DataRow(0)]
        [DataRow(2)]
        public async Task AdminEmailsReturnsOriginalAttachmentContents(int attachmentCount)
        {
            using var client = _factory.CreateClient();
            var attachments = new[]
            {
                new
                {
                    name = "email-html.txt",
                    contentType = "text/plain",
                    contentInBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("<p>Hello \u00e9</p>"))
                },
                new
                {
                    name = "binary.bin",
                    contentType = "application/octet-stream",
                    contentInBase64 = Convert.ToBase64String(new byte[] { 0, 1, 2, 127, 128, 255 })
                }
            }.Take(attachmentCount).ToArray();

            using var sendResponse = await client.PostAsJsonAsync("/emails:send?api-version=2023-03-31", new
            {
                senderAddress = "sender@example.com",
                content = new { subject = "Attachments", plainText = "Hello", html = "<p>Hello</p>" },
                recipients = new { to = new[] { new { address = "recipient@example.com" } } },
                attachments
            });
            Assert.AreEqual(HttpStatusCode.Accepted, sendResponse.StatusCode);

            using var response = await client.GetAsync("/admin/emails");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            var emails = result.GetProperty("value").EnumerateArray().ToArray();
            Assert.AreEqual(1, emails.Length);
            var storedAttachments = emails[0].GetProperty("attachments").EnumerateArray().ToArray();
            Assert.AreEqual(attachmentCount, storedAttachments.Length);

            foreach (var attachment in attachments)
            {
                var stored = storedAttachments.Single(item => item.GetProperty("name").GetString() == attachment.name);
                Assert.IsFalse(string.IsNullOrEmpty(stored.GetProperty("id").GetString()));
                Assert.AreEqual(attachment.contentType, stored.GetProperty("type").GetString());
                CollectionAssert.AreEqual(
                    Convert.FromBase64String(attachment.contentInBase64),
                    Convert.FromBase64String(stored.GetProperty("contentBytesBase64").GetString()!));
            }
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
