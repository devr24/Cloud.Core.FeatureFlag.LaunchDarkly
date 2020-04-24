using Cloud.Core;
using Cloud.Core.FeatureFlag.LaunchDarkly;
using LaunchDarkly.Client;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Class ServiceCollection extensions.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the Launch Darkly Service using the default config.
        /// Config automatically resolved, looks for "LaunchDarklySdkKey" key from IConfiguration.
        /// </summary>
        /// <param name="serviceCollection">Services Collection to add the singleton to.</param>
        /// <returns>IServiceCollection</returns>
        /// 
        public static IServiceCollection AddLaunchDarklyFeatureFlags(this IServiceCollection serviceCollection)
        {
            return serviceCollection.AddSingleton<IFeatureFlag, LaunchDarklyService>();
        }

        /// <summary>
        /// Add the Launch Darkly Service using the default config.
        /// </summary>
        /// <param name="serviceCollection">Services Collection to add the singleton to.</param>
        /// <param name="ldKey">Launch darkly key</param>
        /// <returns>IServiceCollection</returns>
        /// 
        public static IServiceCollection AddLaunchDarklyFeatureFlags(this IServiceCollection serviceCollection, string ldKey)
        {
            var launchDarklyClient = new LaunchDarklyService(ldKey);

            serviceCollection.AddSingleton<IFeatureFlag>(launchDarklyClient);

            return serviceCollection;
        }

        /// <summary>
        /// Add the Launch Darkly Service with a custom LaunchDarkly client config.
        /// </summary>
        /// <param name="serviceCollection">Services Collection to add the singleton to.</param>
        /// <param name="ldClient">Launch darkly client</param>
        /// <returns>IServiceCollection</returns>
        /// 
        public static IServiceCollection AddLaunchDarklyFeatureFlags(this IServiceCollection serviceCollection, ILdClient ldClient)
        {
            var launchDarklyClient = new LaunchDarklyService(ldClient);

            serviceCollection.AddSingleton<IFeatureFlag>(launchDarklyClient);

            return serviceCollection;
        }
    }
}
