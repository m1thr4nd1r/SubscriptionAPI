using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MundiAPI.PCL;
using MundiAPI.PCL.Models;

namespace SubscriptionAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Neste exemplo estamos utilizando A sdk C# MundiAPI.PCL
            string basicAuthUserName = "sk_test_4AdjlqpseatnmgbW";
            string basicAuthPassword = "pk_test_zD9Jq9IoaSx1JVOk";

            var client = new MundiAPIClient(basicAuthUserName, basicAuthPassword);

            //var customer = new CreateCustomerRequest
            //{
            //    Name = "Tony Stark",
            //    Email = "email@email.com",
            //};

            //var request = new CreateChargeRequest()
            //{
            //    Amount = 1490,
            //    Customer = customer,
            //    Payment = new CreatePaymentRequest()
            //    {
            //        PaymentMethod = "credit_card",
            //        CreditCard = new CreateCreditCardPaymentRequest()
            //        {
            //            Card = new CreateCardRequest
            //            {
            //                Number = "342793631858229",
            //                HolderName = "Tony Stark",
            //                ExpMonth = 1,
            //                ExpYear = 18,
            //                Cvv = "3531",
            //            }
            //        }
            //    }
            //};

            //var response = client.Charges.CreateCharge(request);

            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
