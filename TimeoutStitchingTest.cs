using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using HotChocolate;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace HotChocolateTimeoutRepro;

public class TimeoutStitchingTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public TimeoutStitchingTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private static WebApplication CreateRemoteHost(int port)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services
            .AddGraphQLServer()
            .AddType<Query>()
            .AllowIntrospection(true);

        builder.WebHost.ConfigureKestrel(
            kestrel => { kestrel.Listen(IPAddress.Any, port, listen => listen.Protocols = HttpProtocols.Http1); });
        var app = builder.Build();
        app.MapGraphQL();
        return app;
    }

    [Fact]
    public async void QueryIsCanceled_AfterTimeout_WhenExecutionTakesLongerTime_ForRemoteSchema()
    {
        const int port = 5000;

        await using var host = CreateRemoteHost(port);

        await host.StartAsync();
        var client = new HttpClient();
        client.BaseAddress = new Uri($"http://localhost:{port}/");

        var sc = new ServiceCollection();
        sc.AddHttpClient(
            "remote",
            (sp, httpClient) => { httpClient.BaseAddress = new Uri(client.BaseAddress!, "/graphql"); });

        var response = await sc
            .AddGraphQL()
            .AddRemoteSchema("remote")
            .ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromMilliseconds(1000))
            .ExecuteRequestAsync("{book {title} }");

        _testOutputHelper.WriteLine(response.ToJson());
    }

    [Fact]
    public async void QueryIsCanceled_AfterTimeout_WhenExecutionTakesLongerTime()
    {
        var response = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromMilliseconds(1000))
            .ExecuteRequestAsync("{ book {title} }");

        _testOutputHelper.WriteLine(response.ToJson());
    }
}