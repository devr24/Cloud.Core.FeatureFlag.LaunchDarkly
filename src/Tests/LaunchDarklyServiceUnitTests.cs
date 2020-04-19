using Cloud.Core.Testing;
using LaunchDarkly.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;
using static LaunchDarkly.Client.EvaluationReason;

namespace Cloud.Core.FeatureFlag.LaunchDarkly.Tests
{
    [IsUnit]
    public class LaunchDarklyServiceUnitTests
    {
        [Fact]
        public void LaunchDarklyService_GetFeatureFlag_KeyArgumentException()
        {
            var mockLdClient = new Mock<ILdClient>();
            var ldService = new LaunchDarklyService(mockLdClient.Object);
            mockLdClient.Setup(m => m.Initialized()).Returns(true);

            Assert.Throws<ArgumentException>(() => ldService.GetFeatureFlag(null, false));
        }

        [Fact]
        public void LaunchDarklyService_Constructor_ThrowsErrorWhenKeyIsNull()
        {
            var mockLogger = new Mock<ILogger>();   
            string nullKey = null;

            Assert.Throws<ArgumentException>(() => new LaunchDarklyService(nullKey, mockLogger.Object));
        }

        [Fact]
        public void LaunchDarklyService_Constructor_DependencyInjectionResolutionException()
        {
            // Act - setup service collection and config.
            var services = new ServiceCollection();
            var configBuilder = new ConfigurationBuilder();

            // Make sure config contains the sdk key for testing with the name it expects.
            configBuilder.AddInMemoryCollection(new List<KeyValuePair<string, string>> {
               new KeyValuePair<string, string>("NoSdkKey", "none")
            });

            var config = configBuilder.Build();

            services.AddSingleton<IConfiguration>(config);
            services.AddLogging(loggingBuilder => loggingBuilder.AddConsole());

            // Add our service singleton for testing.
            services.AddLaunchDarklyFeatureFlags();

            var prov = services.BuildServiceProvider();

            Assert.Throws<ArgumentException>(() => prov.GetService<IFeatureFlag>());
        }

        [Fact]
        public void LaunchDarklyService_Constructor_DependencyInjectionResolution()
        {
            // Act - setup service collection and config.
            var services = new ServiceCollection();
            var configBuilder = new ConfigurationBuilder();

            // Make sure config contains the sdk key for testing with the name it expects.
            configBuilder.AddInMemoryCollection(new List<KeyValuePair<string, string>> {
               new KeyValuePair<string, string>("LaunchDarklySdkKey", "sampleKey")
            });

            var config = configBuilder.Build();

            services.AddSingleton<IConfiguration>(config);
            services.AddLogging(loggingBuilder => loggingBuilder.AddConsole());

            // Add our service singleton for testing.
            services.AddLaunchDarklyFeatureFlags();

            var prov = services.BuildServiceProvider();

            var featureFlagService = prov.GetService<IFeatureFlag>();
            Assert.NotNull(featureFlagService);
        }

        [Fact]
        public void LaunchDarklyService_GetFeatureFlag_NullClientThrowsInvalidOperationException()
        {
            var ldService = new LaunchDarklyService();

            Assert.Throws<InvalidOperationException>(() => ldService.GetFeatureFlag("A Key", false));
        }


        [Fact]
        public void LaunchDarklyService_GetFeatureFlag_NotInitialisedClientThrowsInvalidOperationException()
        {
            var mockLdClient = new Mock<ILdClient>();
            var ldService = new LaunchDarklyService(mockLdClient.Object);
            mockLdClient.Setup(m => m.Initialized()).Returns(false);

            Assert.Throws<InvalidOperationException>(() => ldService.GetFeatureFlag("A Key", false));
        }

        [Fact]
        public void LaunchDarklyService_GetFeatureFlag_ReturnsTrue()
        {
            //Arrange
            var mockLdClient = new Mock<ILdClient>();
            mockLdClient.Setup(m =>
                m.BoolVariationDetail(It.Is<string>(x => x.Equals("Test")), It.IsAny<User>(), It.IsAny<bool>())
            ).Returns(new EvaluationDetail<bool>(true, 0, EvaluationReason.Fallthrough.Instance));
            mockLdClient.Setup(m => m.Initialized()).Returns(true);
            var ldService = new LaunchDarklyService(mockLdClient.Object, null);

            //Act
            var result = ldService.GetFeatureFlag("Test", false);
            //Assert
            Assert.True(result);
        }

        [Fact]
        public void LaunchDarklyService_GetFeatureFlag_ReturnsDefault()
        {
            //Arrange
            var config = new Configuration().WithOffline(true);
            var ldClient = new LdClient(config);
            var ldService = new LaunchDarklyService(ldClient, null);

            //Act
            var result = ldService.GetFeatureFlag("Test", false);

            //Assert
            Assert.False(result);
        }

        [Fact]
        public void LaunchDarklyService_GetFeatureFlag_LogsError()
        {
            //Arrange
            var mockLdClient = new Mock<ILdClient>();
            var mockLogger = new Mock<ITelemetryLogger>();
            mockLdClient.Setup(m =>
                m.BoolVariationDetail(It.Is<string>(x => x.Equals("Test")), It.IsAny<User>(), It.IsAny<bool>())
            ).Returns(new EvaluationDetail<bool>(false, 0, new Error(EvaluationErrorKind.EXCEPTION)));
            mockLdClient.Setup(m => m.Initialized()).Returns(true);
            var ldService = new LaunchDarklyService(mockLdClient.Object, mockLogger.Object);

            //Act
            var result = ldService.GetFeatureFlag("Test", false);

            //Assert
            mockLogger.Verify(m =>
                m.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((object x, Type _) => x.ToString().Contains($"Failed to get feature flag. Reason: {EvaluationErrorKind.EXCEPTION}")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
                )
            );
        }

        [Fact]
        public void LaunchDarklyService_GetFeatureFlag_ErrorWithNoLogger()
        {
            //Arrange
            var mockLdClient = new Mock<ILdClient>();
            var mockLogger = new Mock<ITelemetryLogger>();
            mockLdClient.Setup(m =>
                m.BoolVariationDetail(It.Is<string>(x => x.Equals("Test")), It.IsAny<User>(), It.IsAny<bool>())
            ).Returns(new EvaluationDetail<bool>(false, 0, new Error(EvaluationErrorKind.EXCEPTION)));
            mockLdClient.Setup(m => m.Initialized()).Returns(true);
            var ldService = new LaunchDarklyService(mockLdClient.Object);

            //Act
            var result = ldService.GetFeatureFlag("Test", false);

            //Assert
            mockLogger.Verify(m =>
                m.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((object x, Type _) => x.ToString().Contains($"Failed to get feature flag. Reason: {EvaluationErrorKind.EXCEPTION}")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
                ),
                Times.Never
            );
        }
    }
}
