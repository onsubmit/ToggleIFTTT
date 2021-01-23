namespace ToggleIFTTT
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;

    public static class Functions
    {
        /// <summary>
        /// The application settings key for the IFTTT access token.
        /// </summary>
        private const string TokenAppSettingKey = "IFTTT_TOKEN";

        /// <summary>
        /// Empty POST data
        /// </summary>
        private static readonly FormUrlEncodedContent EmptyPostContent = new FormUrlEncodedContent(new Dictionary<string, string>());

        /// <summary>
        /// The toggle event url
        /// </summary>
        private const string ToggleEventUrl = "https://maker.ifttt.com/trigger/toggle/with/key";

        [FunctionName("Toggle")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            Contract.Requires(req != null);
            Contract.Requires(log != null);
            Contract.Requires(context != null);

            using HttpClient client = new HttpClient();

            try
            {
                HttpResponseMessage message = await client.PostAsync($"{ToggleEventUrl}/{GetAccessToken(context)}", EmptyPostContent);
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
        /// Gets the IFTTT Access Token from the application settings
        /// </summary>
        /// <param name="context">Execution context</param>
        /// <returns>The access token</returns>
        internal static string GetAccessToken(ExecutionContext context)
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            return config[TokenAppSettingKey];
        }
    }
}
