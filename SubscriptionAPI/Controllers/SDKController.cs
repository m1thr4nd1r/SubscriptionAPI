using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using MundiAPI.PCL;
using MundiAPI.PCL.Exceptions;
using MundiAPI.PCL.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SubscriptionAPI.Controllers
{
    public static class SdkController
    {
        #region Fields

        private static MundiAPIClient _client;

        public static MundiAPIClient Client
        {
            get
            {
                string user = Environment.GetEnvironmentVariable("MundiPaggUser") ??
                              Environment.GetEnvironmentVariable("ASPNETCORE_MundiPaggUser");

                string password = Environment.GetEnvironmentVariable("MundiPaggPassword") ??
                                  Environment.GetEnvironmentVariable("ASPNETCORE_MundiPaggPassword");

                if (_client == null)
                    _client = new MundiAPIClient(user, password);

                return _client;
            }
        }

        #endregion

        public static GetCustomerResponse InitializeCustomer(JObject data)
        {
            string name = (string)data?["cliente"]?["nome"];
            string email = (string)data?["cliente"]?["email"];

            try
            {
                if (name == null || email == null)
                    throw new FormatException("Nome e Email precisam ser informados.");

                var customers = Client.Customers.GetCustomers(name, null, null, null, email);

                if (customers.Data.Count > 0)
                    return customers.Data[0];

                var createCustomer = new CreateCustomerRequest
                {
                    Name = name,
                    Email = email
                };

                var customer = Client.Customers.CreateCustomer(createCustomer);

                if (string.IsNullOrEmpty(customer?.Id))
                    throw new InvalidOperationException("Falha em criar o cliente.");

                return customer;
            }
            catch (APIException ex)
            {
                throw new InvalidOperationException("Falha em criar o cliente.", ex);
            }
        }

        public static GetCardResponse InitializeCard(JObject data, string customerId, string customerName)
        {
            string number = (string)data?["cartao"]?["numero"];
            int expiration_Month = (int)data?["cartao"]?["expiracao_mes"];
            int expiration_Year = (int)data?["cartao"]?["expiracao_ano"];
            string cvv = (string)data?["cartao"]?["cvv"];

            try
            {
                var cards = Client.Customers.GetCards(customerId);
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

                card = Client.Customers.CreateCard(customerId, createCard);

                if (string.IsNullOrEmpty(card.Id))
                    throw new InvalidOperationException("Falha em criar o cartão.");

                return card;
            }
            catch (APIException ex)
            {
                throw new InvalidOperationException("Falha em criar o cartão.", ex);
            }
        }

        public static GetPlanResponse InitializePlan(string type, int interval, bool trial)
        {
            try
            {
                var plans = Client.Plans.GetPlans(null, null, type);

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
                    Quantity = 1,

                    Currency = "BRL",
                    Interval = "month",
                    IntervalCount = interval,
                    TrialPeriodDays = (trial) ? (int?)7 : null,
                    PricingScheme = createPriceScheme,
                };

                var plan = Client.Plans.CreatePlan(createPlan);

                if (string.IsNullOrEmpty(plan?.Id))
                    throw new MissingMemberException($"Falha ao criar o plano {type}");

                return plan;
            }
            catch (APIException ex)
            {
                throw new InvalidOperationException($"Falha ao criar o plano {type}", ex);
            }
        }

        public static GetSubscriptionResponse InitializeSubscription(JObject data, string customerId, string cardId)
        {
            GetPlanResponse plan = null;
            var createSub = new CreateSubscriptionRequest();
            var items = new List<CreateIncrementRequest>();

            try
            {
                foreach (var product in data?["produtos"])
                {
                    string type = (string)product["tipo"];

                    switch (type)
                    {
                        case "plano":
                            string planId = (string)product["plano_id"];

                            plan = Client.Plans.GetPlan(planId);

                            break;
                        case "trimestral":
                            bool trial = (bool?)product?["periodo_teste"] ?? false;

                            plan = InitializePlan(type, 3, trial);

                            items.Add(new CreateIncrementRequest
                            {
                                IncrementType = "Flat",
                                Value = 69,//Value = 69.9d,

                                Description = "Assinatura",
                            });

                            break;
                        case "mensal":
                            trial = (bool?)product?["periodo_teste"] ?? false;

                            plan = InitializePlan(type, 1, trial);

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

                var sub = Client.Subscriptions.CreateSubscription(createSub);

                if (string.IsNullOrEmpty(sub?.Id))
                    throw new InvalidOperationException("Falha ao criar assinatura.");

                return sub;
            }
            catch (APIException ex)
            {
                throw new InvalidOperationException("Falha ao criar assinatura.", ex);
            }
        }

        public static GetSubscriptionResponse UpdateSubscriptionCard(JObject data)
        {
            try
            {
                string customerID = (string)data?["cliente_id"];

                if (string.IsNullOrEmpty(customerID))
                    throw new FormatException("Cliente invalido");

                var cards = Client.Customers.GetCards(customerID);

                if (cards.Data.Count == 0)
                    throw new InvalidOperationException("Cartão inexistente.");
                
                string number = (string)data?["cartao"]?["numero"];
                int expiration_Month = (int)data?["cartao"]?["expiracao_mes"];
                int expiration_Year = (int)data?["cartao"]?["expiracao_ano"];
                string cvv = (string)data?["cartao"]?["cvv"];

                var card = cards.Data[0];

                var createCard = new CreateCardRequest
                {
                    Number = number,
                    HolderName = card.HolderName,
                    ExpMonth = expiration_Month,
                    ExpYear = expiration_Year,
                    Cvv = cvv
                };

                var updateCard = new UpdateSubscriptionCardRequest
                {
                    Card = createCard,
                };

                var subscriptions = Client.Subscriptions.GetSubscriptions(null, null, null, null, customerID);
                var subscription = subscriptions.Data.Find(s => s.Card.Id.Equals(card.Id));

                if (subscriptions == null)
                    throw new InvalidOperationException("Não existem assinaturas para este cliente.");

                var sub = Client.Subscriptions.UpdateSubscriptionCard(subscription.Id, updateCard);

                if (string.IsNullOrEmpty(sub.Id))
                    throw new InvalidOperationException("Falha ao atualizar assinatura.");

                return sub;
            }
            catch (APIException ex)
            {
                throw new InvalidOperationException("Falha ao atualizar assinatura.", ex);
            }
        }

        public static GetSubscriptionResponse CancelSubscription(JObject data)
        {
            string subId = (string)data?["assinatura_id"];
            string customerId = (string)data?["cliente_id"];

            if (string.IsNullOrEmpty(subId) && string.IsNullOrEmpty(customerId))
                throw new ArgumentNullException("data", "Assinatura e cliente não informados.");

            try
            {
                GetSubscriptionResponse cancelSub = null;

                if (!string.IsNullOrEmpty(subId))
                    cancelSub = Client.Subscriptions.CancelSubscription(subId);
                else
                {
                    var subscriptions = Client.Subscriptions.GetSubscriptions(null, null, null, null, customerId);
                    var subscription = subscriptions.Data[0] ?? null;

                    if (subscription == null)
                        throw new InvalidOperationException("Não existe assinatura para este cliente.");

                    cancelSub = Client.Subscriptions.CancelSubscription(subscription.Id);
                }

                if (string.IsNullOrEmpty(cancelSub.Id))
                    throw new InvalidOperationException("Falha no cancelamento da assinatura.");

                return cancelSub;
            }
            catch (APIException ex)
            {
                throw new InvalidOperationException(ex.Message, ex);
            }
        }
    }
}
