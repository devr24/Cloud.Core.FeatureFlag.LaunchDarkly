using Cloud.Core.Testing;
using LaunchDarkly.Client;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Linq;
using Xunit;

namespace Cloud.Core.FeatureFlag.LaunchDarkly.Tests
{
    [IsUnit]
    public class IServiceCollectionExtensionsUnitTests
    {
        [Fact]
        public void LaunchDarklyService_AddIFeatureFlagDependencyUsingldKey()
        {
            //Arrange
            IServiceCollection services = new ServiceCollection();

            var featureFlagService = new Mock<IFeatureFlag>();

            //Act
            services.AddLaunchDarklyFeatureFlags("sdk-00000000-0000-0000-0000-000000000000");

            //Assert
            bool featureFlagExists = services.Where(x => x.ServiceType == typeof(IFeatureFlag) && x.Lifetime == ServiceLifetime.Singleton).Count() == 1;

            Assert.True(featureFlagExists);
        }

        [Fact]
        public void LaunchDarklyService_AddIFeatureFlagDependencyUsingILdClient()
        {
            //Arrange
            IServiceCollection services = new ServiceCollection();

            var featureFlagService = new Mock<IFeatureFlag>();
            var ldClient = new Mock<ILdClient>();

            //Act
            services.AddLaunchDarklyFeatureFlags(ldClient.Object);

            //Assert
            bool featureFlagExists = services.Where(x => x.ServiceType == typeof(IFeatureFlag) && x.Lifetime == ServiceLifetime.Singleton).Count() == 1;

            Assert.True(featureFlagExists);
        }
    }
}
