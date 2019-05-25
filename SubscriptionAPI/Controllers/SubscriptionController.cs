using System;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using SubscriptionAPI.Models;
using Microsoft.Extensions.Options;

namespace SubscriptionAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController : ControllerBase
    {
        public SubscriptionController(IOptions<MundiPaggApi> apiSettings)
        {
            Environment.SetEnvironmentVariable("API_USER", apiSettings.Value.User);
            Environment.SetEnvironmentVariable("API_PASSWORD", apiSettings.Value.Password);
        }

        [NonAction]
        public string PrintJson(object obj)
        {
            var jObj = JObject.Parse(JsonConvert.SerializeObject(obj));
            return jObj.ToString(Formatting.Indented);
        }

        // POST: api/Subscription
        [HttpPost]
        public string Post([FromBody] object value)
        {
            JObject data = null;

            try
            {
                data = JObject.Parse(value.ToString());
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine(ex.StackTrace);
                return PrintJson(new ErrorMsg { Error = "Formato de entrada invalida." });
            }

            try
            {
                var customer = SdkController.InitializeCustomer(data);
                var card = SdkController.InitializeCard(data, customer.Id, customer.Name);
                var sub = SdkController.InitializeSubscription(data, customer.Id, card.Id);

                return PrintJson(sub);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return PrintJson(new ErrorMsg { Error = "Falha ao realizar a assinatura." });
            }
        }

        // PATCH: api/Subscription
        [HttpPatch]
        public string Patch([FromBody] object value)
        {
            JObject data = null;

            try
            {
                data = JObject.Parse(value.ToString());
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine(ex.StackTrace);
                return PrintJson(new ErrorMsg { Error = "Formato de entrada invalida." });
            }

            try
            {
                var sub = SdkController.UpdateSubscriptionCard(data);

                if (string.IsNullOrEmpty(sub.Id))
                    return PrintJson(new ErrorMsg { Error = "Falha ao atualizar cartão da assinatura." });

                return PrintJson(sub);
            }
            catch (Exception ex)
            {
                Console.Write(ex.StackTrace);
                return PrintJson(new ErrorMsg { Error = ex.Message });
            }
        }
        
        // DELETE: api/Subscription
        [HttpDelete]
        public string Delete([FromBody] object value)
        {
            JObject data = null;

            try
            {
                data = JObject.Parse(value.ToString());
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine(ex.StackTrace);
                return PrintJson(new ErrorMsg { Error = "Formato de entrada invalida." });
            }

            try
            {
                var cancelSub = SdkController.CancelSubscription(data);

                return PrintJson(cancelSub);
            }
            catch (Exception ex)
            {
                Console.Write(ex.StackTrace);
                return PrintJson(new ErrorMsg { Error = ex.Message });
            }
        }
    }
}
