using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Coyote;
using Microsoft.Coyote.SystematicTesting;
using Xunit;
using Xunit.Abstractions;

namespace CoyoteRepro.Tests
{
    public class CoyoteUnitTest : IClassFixture<TestWebApplicationFactory<Startup>>
    {
        private readonly HttpClient _client;
        private readonly GrpcChannel _channel;
        private readonly ITestOutputHelper _outputHelper;

        public CoyoteUnitTest(TestWebApplicationFactory<Startup> factory,
            ITestOutputHelper outputHelper)
        {
            _client = factory
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(service =>
                    {
                    });
                })
                .CreateDefaultClient();

            _channel = GrpcChannel.ForAddress("http://localhost:5004", new GrpcChannelOptions
            {
                HttpClient = _client
            });

            _outputHelper = outputHelper;
        }

        [Fact]
        public void Coyote_Should_Not_Throw_NRE()
        {
            RunCoyoteTest(_outputHelper, async () =>
            {
                var client = new Greeter.GreeterClient(_channel);

                var request = new HelloRequest
                {
                    Name = "World!"
                };

                var response = await client.SayHelloAsync(request);

                Assert.Equal("Hello World!", response.Message);
            });
        }

        [Fact]
        public void Coyote_Should_Succeed()
        {
            RunCoyoteTest(_outputHelper, async () =>
            {
                var client = new Greeter.GreeterClient(_channel);

                var request = new HelloRequest
                {
                    Name = "World!"
                };

                var unaryCall = client.SayHelloAsync(request);
                var response = await unaryCall.ResponseAsync;

                Assert.Equal("Hello World!", response.Message);
            });
        }

        private void RunCoyoteTest(ITestOutputHelper output, Func<Task> test, [CallerMemberName] string testName = null)
        {
            var config = Configuration.Create()
                .WithTestingIterations(1000)
                .WithNoBugTraceRepro();

            TestingEngine engine = TestingEngine.Create(config, test);

            engine.Run();

            var report = engine.TestReport;
            output.WriteLine("Coyote found {0} bug(s).", report.NumOfFoundBugs);

            if (report.BugReports.Count > 0)
            {
                output.WriteLine("Bug Reports: {0}", string.Join("\n", report.BugReports));
            }

            Assert.True(report.NumOfFoundBugs == 0, $"Coyote found {report.NumOfFoundBugs} bug(s) in {testName}.");
        }
    }
}