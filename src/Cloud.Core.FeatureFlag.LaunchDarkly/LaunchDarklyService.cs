using System;
using LaunchDarkly.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static LaunchDarkly.Client.EvaluationReason;

namespace Cloud.Core.FeatureFlag.LaunchDarkly
{
    /// <summary>
    /// Custom Launch Darkly Service to wrap Launch Darkly calls.
    /// </summary>
    public class LaunchDarklyService : IFeatureFlag
    {
        private ILdClient _ldClient;
        private readonly ILogger _logger;
        private readonly string _key;

        // For testing only
        internal LaunchDarklyService() { }

        /// <summary>
        /// Simple constructor when using custom Launch Darkly Client - should only be used for testing
        /// </summary>
        /// <param name="ldClient">Launch Darkly client</param>
        /// <param name="logger">Optional logger</param>
        internal LaunchDarklyService(ILdClient ldClient, ILogger logger = null)
        {
            _ldClient = ldClient;
            _logger = logger;
        }

        /// <summary>
        /// Internal Launch Darkly Client, Will attempt to create the client when being retrieved if it failed to be initialised at start time
        /// </summary>
        internal ILdClient LdClient
        {
            get
            {
                if (_ldClient == null)
                {
                    try
                    {
                        //If client is not initialised attempt to initialize it with the sdk key before returning.
                        _ldClient = new LdClient(_key);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Exception occured when initialised Launch Darkly Client");
                    }
                }

                return _ldClient;
            }
        }

        /// <summary>
        /// Constructor for automatic resolution using dependency injection.
        /// </summary>
        public LaunchDarklyService(IConfiguration config, ILogger<LaunchDarklyService> logger)
        {
            var ldKey = config.GetValue<string>("LaunchDarklySdkKey");

            if (ldKey.IsNullOrEmpty())
            {
                throw new ArgumentException("Launch Darkly key cannot be resolved from IConfiguration (looking for \"LaunchDarklySdkKey\")");
            }

            _key = ldKey;
            _logger = logger;
        }

        /// <summary>
        /// Simple constructor when using default Launch Darkly Client
        /// </summary>
        /// <param name="ldKey">Launch Darkly key</param>
        /// <param name="logger">Optional logger</param>
        public LaunchDarklyService(string ldKey, ILogger logger = null)
        {
            if (ldKey.IsNullOrEmpty())
            {
                throw new ArgumentException("Launch Darkly key cannot be null or empty");
            }

            _key = ldKey;
            _logger = logger;
        }
        
        /// <summary>
        /// Get boolean feature flag.
        /// </summary>
        /// <param name="key">Feature flag key</param>
        /// <param name="defaultValue">Value if no flag is found or something goes wrong.</param>
        /// <returns>Boolean [true] if flag is enabled, [false] if not.</returns>
        public bool GetFeatureFlag(string key, bool defaultValue)
        {
            if (LdClient == null || !LdClient.Initialized())
            {
                throw new InvalidOperationException("Launch Darkly Client is not initialized.");
            }

            // Ensure the key and user are set before trying to execute.
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(nameof(key), "Feature flag key must be set");
            }

            var featureFlagResponse = LdClient.BoolVariationDetail(key, User.WithKey(AppDomain.CurrentDomain.FriendlyName), defaultValue);

            if (featureFlagResponse.Reason.Kind == EvaluationReasonKind.ERROR)
            {
                var errorResponse = featureFlagResponse.Reason as Error;
                var errorKind = errorResponse?.ErrorKind;
                _logger?.LogError($"Failed to get feature flag. Reason: {errorKind}");

                return defaultValue;
            }

            return featureFlagResponse.Value;
        }
    }
}
