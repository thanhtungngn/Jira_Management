using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using ProjectManagement.Core.GitHub;
using ProjectManagement.Core.Jira;
using ProjectManagement.Core.Trello;

namespace ProjectManagement.Api.Tests;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TProgram}"/> that replaces all three API clients with
/// Moq mocks so no real credentials or network access are required.
/// </summary>
public class ApiTestFactory : WebApplicationFactory<Program>
{
    public Mock<IJiraClient>   JiraMock   { get; } = new();
    public Mock<ITrelloClient> TrelloMock { get; } = new();
    public Mock<IGitHubClient> GitHubMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // Remove the real typed HttpClient registrations and replace with mocks
            services.RemoveAll<IJiraClient>();
            services.RemoveAll<ITrelloClient>();
            services.RemoveAll<IGitHubClient>();

            services.AddSingleton(JiraMock.Object);
            services.AddSingleton(TrelloMock.Object);
            services.AddSingleton(GitHubMock.Object);
        });
    }
}
