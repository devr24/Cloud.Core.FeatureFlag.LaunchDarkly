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
        /// <summary>Ensure get feature flag throws argument exception when null key is passed.</summary>
        [Fact]
        public void Test_LaunchDarklyService_GetFeatureFlag_KeyArgumentException()
        {
            // Arrange
            var mockLdClient = new Mock<ILdClient>();
            var ldService = new LaunchDarklyService(mockLdClient.Object);
            mockLdClient.Setup(m => m.Initialized()).Returns(true);

            // Act/Assert
            Assert.Throws<ArgumentException>(() => ldService.GetFeatureFlag(null, false));
        }

        /// <summary>Ensure constructor throws error when no sdk key is passed.</summary>
        [Fact]
        public void Test_LaunchDarklyService_Constructor_ThrowsErrorWhenKeyIsNull()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();   
            string nullKey = null;

            // Act/Assert
            Assert.Throws<ArgumentException>(() => new LaunchDarklyService(nullKey, mockLogger.Object));
        }

        /// <summary>Ensure exception thrown after service is resolved and key is sought, when no sdk key was setup during DI setup.</summary>
        [Fact]
        public void Test_LaunchDarklyService_Constructor_DependencyInjectionResolutionException()
        {
            // Arrange - setup service collection and config.
            var services = new ServiceCollection();
            var configBuilder = new ConfigurationBuilder();

            // Make sure config contains the sdk key for testing with the name it expects.
            configBuilder.AddInMemoryCollection(new List<KeyValuePair<string, string>> {
               new KeyValuePair<string, string>("NoSdkKey", "none")
            });

            var config = configBuilder.Build();

            services.AddSingleton<IConfiguration>(config);
            services.AddLogging(loggingBuilder => loggingBuilder.AddConsole());
            services.AddLaunchDarklyFeatureFlags();

            // Act/Assert - Add our service singleton for testing.
            var prov = services.BuildServiceProvider();
            Assert.Throws<ArgumentException>(() => prov.GetService<IFeatureFlag>());
        }

        /// <summary>Ensure get feature flag service is setup correctly for DI.</summary>
        [Fact]
        public void Test_LaunchDarklyService_Constructor_DependencyInjectionResolution()
        {
            // Arrange
            var services = new ServiceCollection();
            var configBuilder = new ConfigurationBuilder();

            // Make sure config contains the sdk key for testing with the name it expects.
            configBuilder.AddInMemoryCollection(new List<KeyValuePair<string, string>> {
               new KeyValuePair<string, string>("LaunchDarklySdkKey", "sampleKey")
            });

            var config = configBuilder.Build();

            services.AddSingleton<IConfiguration>(config);
            services.AddLogging(loggingBuilder => loggingBuilder.AddConsole());
            services.AddLaunchDarklyFeatureFlags();

            // Act - Add our service singleton for testing.
            var prov = services.BuildServiceProvider();
            var featureFlagService = prov.GetService<IFeatureFlag>();
            
            // Assert
            Assert.NotNull(featureFlagService);
        }

        /// <summary>Ensure error is thrown when Ld client isn't resolved.</summary>
        [Fact]
        public void Test_LaunchDarklyService_GetFeatureFlag_NullClientThrowsInvalidOperationException()
        {
            // Arrange
            var ldService = new LaunchDarklyService();

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() => ldService.GetFeatureFlag("A Key", false));
        }

        /// <summary>Ensure error is thrown when Initialised returns false.</summary>
        [Fact]
        public void Test_LaunchDarklyService_GetFeatureFlag_NotInitialisedClientThrowsInvalidOperationException()
        {
            // Arrange
            var mockLdClient = new Mock<ILdClient>();
            var ldService = new LaunchDarklyService(mockLdClient.Object);
            mockLdClient.Setup(m => m.Initialized()).Returns(false);

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() => ldService.GetFeatureFlag("A Key", false));
        }

        /// <summary>Ensure retrieving the feature flag returns true as expected.</summary>
        [Fact]
        public void Test_LaunchDarklyService_GetFeatureFlag_ReturnsTrue()
        {
            // Arrange
            var mockLdClient = new Mock<ILdClient>();
            mockLdClient.Setup(m =>
                m.BoolVariationDetail(It.Is<string>(x => x.Equals("Test")), It.IsAny<User>(), It.IsAny<bool>())
            ).Returns(new EvaluationDetail<bool>(true, 0, Fallthrough.Instance));
            mockLdClient.Setup(m => m.Initialized()).Returns(true);
            var ldService = new LaunchDarklyService(mockLdClient.Object, null);

            // Act
            var result = ldService.GetFeatureFlag("Test", false);

            // Assert
            Assert.True(result);
        }

        /// <summary>Ensure retrieving the feature flag returns false as expected.</summary>
        [Fact]
        public void Test_LaunchDarklyService_GetFeatureFlag_ReturnsDefault()
        {
            // Arrange
            var config = new Configuration().WithOffline(true);
            var ldClient = new LdClient(config);
            var ldService = new LaunchDarklyService(ldClient, null);

            // Act
            var result = ldService.GetFeatureFlag("Test", false);

            // Assert
            Assert.False(result);
        }

        /// <summary>Ensure error is captured in logger.</summary>
        [Fact]
        public void Test_LaunchDarklyService_GetFeatureFlag_LogsError()
        {
            // Arrange
            var mockLdClient = new Mock<ILdClient>();
            var mockLogger = new Mock<ITelemetryLogger>();
            mockLdClient.Setup(m =>
                m.BoolVariationDetail(It.Is<string>(x => x.Equals("Test")), It.IsAny<User>(), It.IsAny<bool>())
            ).Returns(new EvaluationDetail<bool>(false, 0, new Error(EvaluationErrorKind.EXCEPTION)));
            mockLdClient.Setup(m => m.Initialized()).Returns(true);
            var ldService = new LaunchDarklyService(mockLdClient.Object, mockLogger.Object);

            // Act
            var result = ldService.GetFeatureFlag("Test", false);

            // Assert
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

        /// <summary>Ensure error is captured when no logger is setup.</summary>
        [Fact]
        public void Test_LaunchDarklyService_GetFeatureFlag_ErrorWithNoLogger()
        {
            // Arrange
            var mockLdClient = new Mock<ILdClient>();
            var mockLogger = new Mock<ITelemetryLogger>();
            mockLdClient.Setup(m =>
                m.BoolVariationDetail(It.Is<string>(x => x.Equals("Test")), It.IsAny<User>(), It.IsAny<bool>())
            ).Returns(new EvaluationDetail<bool>(false, 0, new Error(EvaluationErrorKind.EXCEPTION)));
            mockLdClient.Setup(m => m.Initialized()).Returns(true);
            var ldService = new LaunchDarklyService(mockLdClient.Object);

            // Act
            var result = ldService.GetFeatureFlag("Test", false);

            // Assert
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
