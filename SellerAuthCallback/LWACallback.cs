using System;
using System.Data.SqlClient;
using System.IO;
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

            log.LogInformation($"LWACallback function processed a request for {selling_partner_id}.");
            
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);



            //var str = Environment.GetEnvironmentVariable("DbConnectionString");
            //using (SqlConnection conn = new SqlConnection(str))
            //{
            //    conn.Open();
            //    var text = "SELECT * from dbo.AspNetUsers";

            //    using (SqlCommand cmd = new SqlCommand(text, conn))
            //    {
            //        // Execute the command and log the # rows affected.
            //        var rows = await cmd.ExecuteReaderAsync();
            //        log.LogInformation($"{rows} users found");
            //    }
            //}


            string responseMessage = $"selling_partner_id: {selling_partner_id}, mws_auth_token: {mws_auth_token}, spapi_oauth_code: {spapi_oauth_code}";
            log.LogInformation(responseMessage);
            return new OkObjectResult(responseMessage);
        }
    }
}
