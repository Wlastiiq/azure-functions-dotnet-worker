﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Tests.Features;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class FunctionContextExtensionTests
    {
        DefaultFunctionContext _defaultFunctionContext;
        IServiceScopeFactory _serviceScopeFactory;
        IServiceProvider _serviceProvider;
        InvocationFeatures features;

        public FunctionContextExtensionTests()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConverterContextFactory, DefaultConverterContextFactory>();

            _serviceProvider = serviceCollection.BuildServiceProvider();
            _serviceScopeFactory = _serviceProvider.GetService<IServiceScopeFactory>();

            features = new InvocationFeatures(Enumerable.Empty<IInvocationFeatureProvider>());

            var invocation = new Mock<FunctionInvocation>(MockBehavior.Strict).Object;
            features.Set<FunctionInvocation>(invocation);
        }

        private RpcHttp CreateRpcHttp()
        {
            var rpcHttp = new RpcHttp
            {
                Url = "https://m.sn",
                Method = "GET"
            };
            rpcHttp.Headers.Add("foo", "b");

            return rpcHttp;
        }

        [Fact]
        public async Task BindInputAsyncWorks()
        {           
            // Arrange
            var definition = new TestFunctionDefinition(parameters: new FunctionParameter[]
            {
                new FunctionParameter("req", typeof(HttpRequestData))
            });
            features.Set<FunctionDefinition>(definition);

            _defaultFunctionContext = new DefaultFunctionContext(_serviceScopeFactory, features);
            var grpcHttpReq = new GrpcHttpRequestData(CreateRpcHttp(), _defaultFunctionContext);
            var functionBindings = new TestFunctionBindingsFeature
            {
                InputData = new ReadOnlyDictionary<string, object>(new Dictionary<string, object> { { "req", grpcHttpReq } })
            };
            features.Set<IFunctionBindingsFeature>(functionBindings);

            // Mock input conversion feature to return a successfully converted value.
            var conversionFeature = new Mock<IInputConversionFeature>(MockBehavior.Strict);
            conversionFeature.Setup(a => a.ConvertAsync(It.IsAny<ConverterContext>())).ReturnsAsync(ConversionResult.Success(grpcHttpReq));
            features.Set<IInputConversionFeature>(conversionFeature.Object);

            // Act
            var actual = await _defaultFunctionContext.BindInputAsync<HttpRequestData>();
            
            // Assert
            var httpReqData = TestUtility.AssertIsTypeAndConvert<HttpRequestData>(actual);
            Assert.NotNull(httpReqData);
        }
    }
}