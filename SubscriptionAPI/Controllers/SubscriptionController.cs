using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using MundiAPI.PCL.Models;
using MundiAPI.PCL;

namespace SubscriptionAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController : ControllerBase
    {
        // GET: api/Subscription
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // POST: api/Subscription
        [HttpPost]
        public string Post([FromBody] Object value)
        {
            string basicAuthUserName = "sk_test_4AdjlqpseatnmgbW";
            string basicAuthPassword = "pk_test_zD9Jq9IoaSx1JVOk";

            var client = new MundiAPIClient(basicAuthUserName, basicAuthPassword);

            JObject data = JObject.Parse(value.ToString());

            string name = (string)data?["cliente"]?["nome"];
            string email = (string)data?["cliente"]?["email"];

            var customers = client.Customers.GetCustomers(name, null, null, null, email);

            if (customers.Data.Count == 0)
            {
                var createCustomer = new CreateCustomerRequest
                {
                    Name = name,
                    Email = email
                };

                var customer = client.Customers.CreateCustomer(createCustomer);

                if (string.IsNullOrEmpty(customer.Id))
                    return "Falha em criar o usuário.";
                else
                    customers.Data.Add(customer);
            }

            string number = (string)data?["cartao"]?["numero"];
            int expiration_Month = (int)data?["cartao"]?["expiracao_mes"];
            int expiration_Year = (int)data?["cartao"]?["expiracao_ano"];
            string cvv = (string)data?["cartao"]?["cvv"];

            var cards = client.Customers.GetCards(customers.Data[0].Id);
            var card = cards.Data.Find(c =>
                                       c.FirstSixDigits.Equals(number.Substring(0, 6)) &&
                                       c.LastFourDigits.Equals(number.Substring(number.Length - 4)) &&
                                       c.ExpMonth == expiration_Month &&
                                       c.ExpYear == expiration_Year
                                      );

            if (card == null)
            {
                var createCard = new CreateCardRequest
                {
                    Number = number,
                    HolderName = customers.Data[0].Name,
                    ExpMonth = expiration_Month,
                    ExpYear = expiration_Year,
                    Cvv = cvv
                };

                var newCard = client.Customers.CreateCard(customers.Data[0].Id, createCard);

                if (string.IsNullOrEmpty(newCard.Id))
                    return "Falha em criar o cartão.";
            }

            foreach (var product in data["produtos"])
            {
                string type = (string)product["tipo"];

                switch (type)
                {
                    case "plano":
                        string planId = (string)product["plano_id"];
                        break;
                }

                var item = new CreateSubscriptionItemRequest();
            }

            return "Cadastro realizado com sucesso!";
        }

        // DELETE: api/Subscription
        [HttpDelete]
        public void Delete(int id)
        {
        }
    }
}
