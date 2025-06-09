//using BIT.Data.Sync.Client;
//using BIT.Data.Sync.Server;
//using BIT.Data.Sync.Tests.SimpleDatabasesTest;
//using NUnit.Framework;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Http;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using BIT.Data.Sync.Imp;
//using System.IO;
//using System.Runtime.Serialization.Json;
//using System.Net.Http.Headers;
//using System.Net;
//using BIT.Data.Sync.Tests.Infrastructure;

//namespace BIT.Data.Sync.Tests.ProxyHandlerTests
//{
//    /// <summary>
//    /// Integration tests for the ProxyHandler class to ensure it returns responses
//    /// in the format expected by SyncFrameworkHttpClient.
//    /// </summary>
//    public class ProxyHandlerTest : MultiServerBaseTest
//    {
//        private HttpClient _localClient;
//        private ISyncFrameworkServer _syncServer;
//        private ProxyHandler _proxyHandler;
//        private MemoryDeltaStore _memoryDeltaStore;
//        private const string TestNodeId = "TestProxyNode";
//        private const string TestIdentity = "TestDatabase";

//        [SetUp]
//        public void Setup()
//        {
//            // Create a memory delta store for testing
//            _memoryDeltaStore = new MemoryDeltaStore();
            
//            // Set up the server with our test delta store
//            _syncServer = new SyncFrameworkServer();
//            _syncServer.RegisterNodeFunction = (request) =>
//            {
//                var nodeId = request.Options.FirstOrDefault(o => o.Key == "NodeId")?.Value ?? TestNodeId;
//                return new ServerSyncEndpoint(_memoryDeltaStore)
//                {
//                    NodeId = nodeId
//                };
//            };
            
//            // Create a node for testing
//            var registerRequest = new RegisterNodeRequest();
//            registerRequest.Options.Add(new Option("NodeId", TestNodeId));
//            _syncServer.CreateNodeAsync(registerRequest);
            
//            // Create our proxy handler
//            _proxyHandler = new ProxyHandler(_syncServer);
            
//            // Create a client that uses our handler
//            _localClient = new HttpClient(_proxyHandler)
//            {
//                BaseAddress = new Uri("http://localhost/sync/")
//            };
//            _localClient.DefaultRequestHeaders.Add("NodeId", TestNodeId);
//        }

//        [Test]
//        public async Task ProxyHandler_Push_ReturnsValidResponse()
//        {
//            // Arrange
//            // Create a SyncFrameworkHttpClient that uses our test HttpClient with the ProxyHandler
//            ISyncFrameworkClient syncFrameworkClient = new SyncFrameworkHttpClient(_localClient, TestNodeId);
            
//            // Create a test database
//            SimpleDatabase testDb = new SimpleDatabase(TestIdentity, syncFrameworkClient);
            
//            // Add some test records
//            SimpleDatabaseRecord record1 = new SimpleDatabaseRecord { Key = Guid.NewGuid(), Text = "Test1" };
//            SimpleDatabaseRecord record2 = new SimpleDatabaseRecord { Key = Guid.NewGuid(), Text = "Test2" };
//            await testDb.Add(record1);
//            await testDb.Add(record2);
            
//            // Act
//            var pushResult = await testDb.PushAsync();
            
//            // Assert
//            Assert.IsNotNull(pushResult, "Push operation should return a response");
//            Assert.IsTrue(pushResult.Success, "Push operation should be successful");
//            Assert.AreEqual(TestNodeId, pushResult.ServerNodeId, "Server node ID should match");
//        }

//        [Test]
//        public async Task ProxyHandler_Fetch_ReturnsValidResponse()
//        {
//            // Arrange
//            // Create a client with our test handler
//            ISyncFrameworkClient syncFrameworkClient = new SyncFrameworkHttpClient(_localClient, TestNodeId);
            
//            // Create a test database and add records
//            SimpleDatabase testDb = new SimpleDatabase(TestIdentity, syncFrameworkClient);
//            SimpleDatabaseRecord record = new SimpleDatabaseRecord { Key = Guid.NewGuid(), Text = "FetchTest" };
//            await testDb.Add(record);
            
//            // Push data to create deltas in the store
//            await testDb.PushAsync();
            
//            // Act
//            var fetchResult = await syncFrameworkClient.FetchAsync(string.Empty, TestIdentity);
            
//            // Assert
//            Assert.IsNotNull(fetchResult, "Fetch operation should return a response");
//            Assert.IsTrue(fetchResult.Success, "Fetch operation should be successful");
//            Assert.IsNotNull(fetchResult.Deltas, "Deltas collection should be initialized");
//        }

//        [Test]
//        public async Task ProxyHandler_HandShake_ReturnsValidResponse()
//        {
//            // Arrange
//            ISyncFrameworkClient syncFrameworkClient = new SyncFrameworkHttpClient(_localClient, TestNodeId);
            
//            // Act
//            var handshakeResult = await syncFrameworkClient.HandShake(CancellationToken.None);
            
//            // Assert
//            Assert.IsNotNull(handshakeResult, "HandShake response should not be null");
//            Assert.IsTrue(handshakeResult.Success, "HandShake operation should succeed");
//            Assert.IsNotNull(handshakeResult.Deltas, "Deltas collection should be initialized");
//        }

//        [Test]
//        public async Task ProxyHandler_EncodingTest_VerifyUnicodeEncoding()
//        {
//            // This test verifies that the responses use Unicode encoding as expected by SyncFrameworkHttpClient
            
//            // Create a message handler that will intercept the response and verify encoding
//            var interceptHandler = new ResponseInterceptHandler(_proxyHandler);
//            var interceptClient = new HttpClient(interceptHandler)
//            {
//                BaseAddress = new Uri("http://localhost/sync/")
//            };
//            interceptClient.DefaultRequestHeaders.Add("NodeId", TestNodeId);
            
//            // Create a client with our intercept handler
//            ISyncFrameworkClient syncFrameworkClient = new SyncFrameworkHttpClient(interceptClient, TestNodeId);
            
//            // Execute operations that should generate Unicode-encoded responses
//            await syncFrameworkClient.HandShake(CancellationToken.None);
            
//            // The ResponseInterceptHandler will assert that the encoding is correct
//            Assert.IsTrue(interceptHandler.WasResponseIntercepted, "Response should have been intercepted");
//            Assert.IsTrue(interceptHandler.WasEncodingCorrect, "Response should use Unicode encoding");
//        }

//        [Test]
//        public async Task ProxyHandler_MultiplePushes_ProcessesCorrectly()
//        {
//            // Arrange
//            ISyncFrameworkClient syncFrameworkClient = new SyncFrameworkHttpClient(_localClient, TestNodeId);
//            SimpleDatabase testDb = new SimpleDatabase(TestIdentity, syncFrameworkClient);
            
//            // First push
//            SimpleDatabaseRecord record1 = new SimpleDatabaseRecord { Key = Guid.NewGuid(), Text = "MultiPush1" };
//            await testDb.Add(record1);
//            var firstPushResult = await testDb.PushAsync();
            
//            // Second push
//            SimpleDatabaseRecord record2 = new SimpleDatabaseRecord { Key = Guid.NewGuid(), Text = "MultiPush2" };
//            await testDb.Add(record2);
//            var secondPushResult = await testDb.PushAsync();
            
//            // Assert
//            Assert.IsTrue(firstPushResult.Success, "First push should succeed");
//            Assert.IsTrue(secondPushResult.Success, "Second push should succeed");
            
//            // Verify that the data was actually stored
//            var fetchResult = await syncFrameworkClient.FetchAsync(string.Empty, TestIdentity);
//            Assert.IsTrue(fetchResult.Deltas.Count >= 2, "Should have at least 2 deltas after multiple pushes");
//        }

//        /// <summary>
//        /// Special handler to intercept responses and verify encoding
//        /// </summary>
//        private class ResponseInterceptHandler : DelegatingHandler
//        {
//            public bool WasResponseIntercepted { get; private set; }
//            public bool WasEncodingCorrect { get; private set; }

//            public ResponseInterceptHandler(HttpMessageHandler innerHandler) : base(innerHandler)
//            {
//                WasResponseIntercepted = false;
//                WasEncodingCorrect = false;
//            }

//            protected override async Task<HttpResponseMessage> SendAsync(
//                HttpRequestMessage request, CancellationToken cancellationToken)
//            {
//                // Call the inner handler to get the response
//                var response = await base.SendAsync(request, cancellationToken);
                
//                WasResponseIntercepted = true;
                
//                // Check the encoding
//                if (response.Content is StringContent stringContent)
//                {
//                    MediaTypeHeaderValue contentType = stringContent.Headers.ContentType;
//                    WasEncodingCorrect = contentType.CharSet == "utf-16" || 
//                                         contentType.CharSet?.ToLowerInvariant() == "utf-16le";
//                }
                
//                return response;
//            }
//        }
//    }
//}