using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SellerAuthCallback
{
    public static class LWACallback
    {
        [FunctionName("LWACallback")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string selling_partner_id = req.Query["selling_partner_id"];
            string mws_auth_token = req.Query["mws_auth_token"];
            string spapi_oauth_code = req.Query["spapi_oauth_code"];
            string state = req.Query["state"];

            log.LogInformation($"LWACallback function processed a request. selling_partner_id: {selling_partner_id}, " +
                               $"mws_auth_token: {mws_auth_token}, spapi_oauth_code: {spapi_oauth_code}, state: {state}");

            var connectionString = Environment.GetEnvironmentVariable("DbConnectionString");

            int? customer_ident;
            await using (SqlConnection myConnection = new SqlConnection(connectionString))
            {
                await using (SqlCommand cmd = new SqlCommand("GetSpAuthCustomerId", myConnection))
                {
                    myConnection.Open();
                    cmd.CommandType = CommandType.StoredProcedure;
                    customer_ident = (int?)await cmd.ExecuteScalarAsync();
                    log.LogInformation($"called  GetSpAuthCustomerId. customer_ident = {customer_ident}.");
                }

                if (customer_ident != null)
                {
                    await using (SqlConnection myConnection1 = new SqlConnection(connectionString))
                    {
                        await using (SqlCommand cmd = new SqlCommand("SetSpAuthGranted", myConnection1))
                        {
                            myConnection1.Open();
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@customer_ident", customer_ident.Value);
                            cmd.Parameters.AddWithValue("@selling_partner_id", selling_partner_id);
                            cmd.Parameters.AddWithValue("@mws_auth_token", mws_auth_token);
                            cmd.Parameters.AddWithValue("@spapi_oauth_code", spapi_oauth_code);
                            //log.LogInformation(
                            //    $"calling SetSpAuthGranted. customer_ident: {customer_ident.Value}, selling_partner_id: {selling_partner_id}," +
                            //    $"mws_auth_token: {mws_auth_token}, spapi_auth_code: {spapi_oauth_code}");
                            await cmd.ExecuteScalarAsync();
                        }
                    }

                    var authResponse = await GetRefreshToken(log, spapi_oauth_code);
                    log.LogInformation($"Got LWA refresh token. lwa_refresh_token: {authResponse.refresh_token}");
                    
                    await using (SqlConnection myConnection2 = new SqlConnection(connectionString))
                    {
                        await using (SqlCommand cmd = new SqlCommand("SetSpRefreshToken", myConnection2))
                        {
                            myConnection2.Open();
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@customer_ident", customer_ident.Value);
                            cmd.Parameters.AddWithValue("@lwa_refresh_token", authResponse.refresh_token);
                            cmd.Parameters.AddWithValue("@lwa_access_token", authResponse.access_token);
                            await cmd.ExecuteScalarAsync();
                        }
                    }
                }
            }
            
            string responseMessage = string.Empty;
            //responseMessage = $"selling_partner_id: {selling_partner_id}, mws_auth_token: {mws_auth_token}, spapi_oauth_code: {spapi_oauth_code}";
            //log.LogInformation(responseMessage);
            //return new OkObjectResult(responseMessage);
            return new RedirectResult("https://oxwebapptest.azurewebsites.net");
        }

        private static async Task<LWAAuthResponse> GetRefreshToken(ILogger log, string spapi_oauth_code)
        {
            LWAAuthResponse lwaAuthResponse = null;
            var clientSecret = Environment.GetEnvironmentVariable("ClientSecret");
            try
            {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                var uriBuilder = new UriBuilder("https://api.amazon.com/auth/o2/token");
                var url = uriBuilder.ToString();
                var request = new LWAAuthRequest
                {
                    grant_type = "authorization_code",
                    code = spapi_oauth_code,
                    redirect_uri = "https://oxwebapptest.azurewebsites.net",
                    client_id = "amzn1.application-oa2-client.4c53787ad116429187d7990317968e5c",
                    client_secret = clientSecret
                };

                log.LogInformation($"uri: {url}");

                string responseStr = string.Empty;
                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(url),
                    Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")
                };

                var response = await httpClient.SendAsync(httpRequestMessage);
                responseStr = await response.Content.ReadAsStringAsync();

                log.LogInformation($"Get refresh token from amazon. response: {responseStr}");

                lwaAuthResponse = JsonConvert.DeserializeObject<LWAAuthResponse>(responseStr);

                //log.LogInformation(
                //    $"LWAAuthResponse. access_token: {lwaAuthResponse.access_token}, token_type: {lwaAuthResponse.token_type}, " +
                //    $"expires_in: {lwaAuthResponse.expires_in}, refresh_token: {lwaAuthResponse.refresh_token}");
            }
            catch (Exception ex)
            {
                log.LogInformation($"call to amazon failed. ex.Message: {ex.Message}");
            }

            return lwaAuthResponse;
        }
    }
}
