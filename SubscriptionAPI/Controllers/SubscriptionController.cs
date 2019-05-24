using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using MundiAPI.PCL.Models;
using MundiAPI.PCL;
using MundiAPI.PCL.Exceptions;

namespace SubscriptionAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController : ControllerBase
    {
        public GetCustomerResponse InitializeCustomer(JObject data, MundiAPIClient client)
        {
            string name = (string)data?["cliente"]?["nome"];
            string email = (string)data?["cliente"]?["email"];

            var customers = client.Customers.GetCustomers(name, null, null, null, email);

            if (customers.Data.Count > 0)
                return customers.Data[0];

            var createCustomer = new CreateCustomerRequest
            {
                Name = name,
                Email = email
            };

            var customer = client.Customers.CreateCustomer(createCustomer);

            if (string.IsNullOrEmpty(customer.Id))
                throw new MissingMemberException("Falha em criar o usuário.");

            return customer;
        }

        public GetCardResponse InitializeCard(JObject data, string customerId, string customerName, MundiAPIClient client)
        {
            string number = (string)data?["cartao"]?["numero"];
            int expiration_Month = (int)data?["cartao"]?["expiracao_mes"];
            int expiration_Year = (int)data?["cartao"]?["expiracao_ano"];
            string cvv = (string)data?["cartao"]?["cvv"];

            var cards = client.Customers.GetCards(customerId);
            var card = cards.Data.Find(c =>
                                       c.FirstSixDigits.Equals(number.Substring(0, 6)) &&
                                       c.LastFourDigits.Equals(number.Substring(number.Length - 4)) &&
                                       c.ExpMonth == expiration_Month &&
                                       c.ExpYear == expiration_Year
                                      );

            if (card != null)
                return card;

            var createCard = new CreateCardRequest
            {
                Number = number,
                HolderName = customerName,
                ExpMonth = expiration_Month,
                ExpYear = expiration_Year,
                Cvv = cvv
            };

            card = client.Customers.CreateCard(customerId, createCard);

            if (string.IsNullOrEmpty(card.Id))
                throw new MissingMemberException("Falha em criar o cartão.");

            return card;
        }

        public GetPlanResponse InitializePlan(string type, int interval, bool trial, MundiAPIClient client)
        {
            var plans = client.Plans.GetPlans(null, null, type);

            if (plans.Data.Count > 0)
                return plans.Data[0];

            var createPriceScheme = new CreatePricingSchemeRequest
            {
                SchemeType = "unit",
                Price = 0,
            };

            var createPlan = new CreatePlanRequest
            {
                Name = type,
                //BillingDays = new List<int> { 5, 10, 15, 20, 25 },
                Quantity = 1,

                Currency = "BRL",
                Interval = "month",
                IntervalCount = interval,
                TrialPeriodDays = (trial) ? (int?)7 : null,
                PricingScheme = createPriceScheme,
            };

            try
            {
                var plan = client.Plans.CreatePlan(createPlan);

                if (string.IsNullOrEmpty(plan?.Id))
                    throw new MissingMemberException($"Falha ao criar o plano {type}");

                return plan;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public GetSubscriptionResponse InitializeSubscription(JObject data, string customerId, string cardId, MundiAPIClient client)
        {
            GetPlanResponse plan = null;
            var createSub = new CreateSubscriptionRequest();
            var items = new List<CreateIncrementRequest>(); 

            foreach (var product in data["produtos"])
            {
                string type = (string)product["tipo"];

                switch (type)
                {
                    case "plano":
                        string planId = (string)product["plano_id"];

                        try
                        {
                            plan = client.Plans.GetPlan(planId);
                        }
                        catch (ErrorException)
                        {
                            throw;
                        }

                        break;
                    case "trimestral":
                        bool trial = (bool?)product?["periodo_teste"] ?? false;

                        plan = InitializePlan(type, 3, trial, client);

                        items.Add(new CreateIncrementRequest
                        {
                            IncrementType = "Flat",
                            Value = 69,//Value = 69.9d,

                            Description = "Assinatura",
                        });

                        break;
                    case "mensal":
                        trial = (bool?)product?["periodo_teste"] ?? false;

                        plan = InitializePlan(type, 1, trial, client);

                        items.Add(new CreateIncrementRequest
                        {
                            IncrementType = "Flat",
                            Value = 24,//Value = 24.5d,

                            Description = "Assinatura",
                        });

                        break;
                    case "yellowbook":

                        items.Add(new CreateIncrementRequest
                        {
                            IncrementType = "Flat",
                            Value = 139, //Value = 139.9d,

                            Description = "YellowBook",
                            Cycles = 1,
                        });
                        break;
                }

            }

            createSub.PlanId = plan?.Id;
            createSub.CustomerId = customerId;
            createSub.CardId = cardId;
            createSub.Increments = items;

            var sub = client.Subscriptions.CreateSubscription(createSub);

            if (string.IsNullOrEmpty(sub?.Id))
                throw new MissingMemberException("Falha ao criar assinatura.");

            return sub;
        }

        // POST: api/Subscription
        [HttpPost]
        public string Post([FromBody] object value)
        {
            string basicAuthUserName = "sk_test_4AdjlqpseatnmgbW";
            string basicAuthPassword = "pk_test_zD9Jq9IoaSx1JVOk";

            var client = new MundiAPIClient(basicAuthUserName, basicAuthPassword);

            JObject data = JObject.Parse(value.ToString());

            try
            {
                var customer = InitializeCustomer(data, client);
                var card = InitializeCard(data, customer.Id, customer.Name, client);
                InitializeSubscription(data, customer.Id, card.Id, client);
            }
            catch (ErrorException ex)
            {
                return "Falha ao realizar a assinatura.";
            }

            return "Assinatura realizada com sucesso!";
        }

        // DELETE: api/Subscription
        [HttpDelete]
        public void Delete(int id)
        {
        }
    }
}
