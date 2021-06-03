using System;
using System.Net.Http;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace CoyoteRepro.Tests
{
    public class CoyoteUnitTest : IClassFixture<TestWebApplicationFactory<Startup>>
    {
        private readonly HttpClient _client;
        private readonly GrpcChannel _channel;

        public CoyoteUnitTest(TestWebApplicationFactory<Startup> factory)
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
        }

        [Fact]
        public async Task Coyote_Should_Not_Throw_NRE()
        {
            var client = new Greeter.GreeterClient(_channel);

            var request = new HelloRequest
            {
                Name = "World!"
            };

            var response = await client.SayHelloAsync(request);

            Assert.Equal("Hello World!", response.Message);
        }

        [Fact]
        public async Task Coyote_Should_Succeed()
        {
            var client = new Greeter.GreeterClient(_channel);

            var request = new HelloRequest
            {
                Name = "World!"
            };

            var unaryCall = client.SayHelloAsync(request);
            var response = await unaryCall.ResponseAsync;

            Assert.Equal("Hello World!", response.Message);
        }
    }
}