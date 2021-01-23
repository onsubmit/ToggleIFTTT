//-----------------------------------------------------------------------
// <copyright file="Functions.cs" company="Andy Young">
//     Copyright (c) Andy Young. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace ToggleIFTTT
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;

    /// <summary>
    /// Azure functions used to toggle IFTTT maker applets
    /// </summary>
    public static class Functions
    {
        /// <summary>
        /// The application settings key for the IFTTT access token.
        /// </summary>
        private const string TokenAppSettingKey = "IFTTT_TOKEN";

        /// <summary>
        /// The application settings key for the passphrase.
        /// </summary>
        private const string PassphraseSettingKey = "PASSPHRASE";

        /// <summary>
        /// The toggle event url
        /// </summary>
        private const string ToggleEventUrl = "https://maker.ifttt.com/trigger/toggle/with/key";

        /// <summary>
        /// Empty POST data
        /// </summary>
        private static readonly FormUrlEncodedContent EmptyPostContent = new FormUrlEncodedContent(new Dictionary<string, string>());

        /// <summary>
        /// Toggles an IFTTT maker applet
        /// </summary>
        /// <param name="req">The request</param>
        /// <param name="log">The logging mechanism</param>
        /// <param name="context">The execution context</param>
        /// <returns>The result</returns>
        [FunctionName("Toggle")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            Contract.Requires(req != null);
            Contract.Requires(log != null);
            Contract.Requires(context != null);

            IConfigurationRoot appConfig = GetAppConfig(context);

            if (!ValidatePassphrase(appConfig, req))
            {
                return new BadRequestObjectResult("Invalid passphrase");
            }

            using HttpClient client = new HttpClient();

            try
            {
                string requestUri = $"{ToggleEventUrl}/{appConfig[TokenAppSettingKey]}";
                HttpResponseMessage message = await client.PostAsync(requestUri, EmptyPostContent);
                string contents = await message.Content.ReadAsStringAsync();

                if (message.IsSuccessStatusCode)
                {
                    return new OkObjectResult(contents);
                }
                else
                {
                    return new BadRequestObjectResult(contents);
                }
            }
            catch (HttpRequestException hrex)
            {
                return new ExceptionResult(hrex, includeErrorDetail: true);
            }
        }

        /// <summary>
        /// Validates the passphrase is correct
        /// </summary>
        /// <param name="appConfig">The application configuration settings</param>
        /// <param name="req">The request</param>
        /// <returns><c>True</c> if the passphrase is validated successfully, <c>false</c> otherwise</returns>
        private static bool ValidatePassphrase(IConfigurationRoot appConfig, HttpRequest req)
        {
            StringValues passphrase = req.Query["p"];
            if (StringValues.IsNullOrEmpty(passphrase)
                || passphrase.Count != 1)
            {
                return false;
            }

            string expectedPassphrase = appConfig[PassphraseSettingKey];
            if (string.IsNullOrWhiteSpace(expectedPassphrase))
            {
                throw new InvalidOperationException("Unable to retrieve passphrase from app settings.");
            }

            return string.Equals(passphrase, expectedPassphrase);
        }

        /// <summary>
        /// Gets the application configuration settings
        /// </summary>
        /// <param name="context">Execution context</param>
        /// <returns>The application configuration settings</returns>
        internal static IConfigurationRoot GetAppConfig(ExecutionContext context)
        {
            return new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
        }
    }
}
