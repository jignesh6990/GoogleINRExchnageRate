using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using GoogleRateExchange.Models;
using System.Text.RegularExpressions;
using RestSharp;
using System.Threading.Tasks;

namespace GoogleRateExchange.Controllers
{
    public class CurrencyExchangeController : ApiController
    {
        #region "Private Declartion"
        private static List<string> sourceCurrencyList = new List<string>();
        /// <summary>
        /// supported Currency List
        /// </summary>
        private List<string> SupportedCurrencyList
        {
            get
            {
                if(sourceCurrencyList.Count==0)
                {
                    sourceCurrencyList.Add("USD");
                    sourceCurrencyList.Add("GBP");
                    sourceCurrencyList.Add("AUD");
                    sourceCurrencyList.Add("EUR");
                    sourceCurrencyList.Add("CAD");
                    sourceCurrencyList.Add("SGD");
                }
                return sourceCurrencyList;
            }
        }
        #endregion

        #region "API Methods"
        /// <summary>
        /// Conversion Rate Calculator
        /// </summary>
        /// <param name="input">object of Currency exchange input paramter class</param>
        /// <returns></returns>
        /// <History>
        /// [Created By]:Jignesh Prajapati - Date:10-May-2017
        /// </History>
        [Route("api/v0/Rate"),HttpPost]
        public async Task<APIResponse> Rate(CurrencyExchanceInput input)
        {
            if (input == null)
            {
                input = new CurrencyExchanceInput();
            }
            APIResponse objResponse = new APIResponse();
            objResponse.ReturnCode = 0;
            objResponse.Amount = input.Amount;
            objResponse.SourceCurrency = input.SourceCurrency;
            try
            {
                if (SupportedCurrencyList.Contains(input.SourceCurrency)) //validate input currency supported or not
                {
                    decimal exchangeRate = 0;
                    const string toCurrency = "INR";
                    exchangeRate = await GetExchangeRateFromGoogle(input.SourceCurrency, toCurrency);
                    if (exchangeRate > 0)
                    {
                        decimal total = (input.Amount * exchangeRate);
                        
                        objResponse.Total = total;
                        objResponse.ConversionRate = exchangeRate;
                        objResponse.Err = "Success";
                        objResponse.ReturnCode = 1;
                    }

                    else
                    {
                        objResponse.Err = "Error";
                    }
                }
                else
                {
                    objResponse.Err = "Source currency not supported";
                }
            }
            catch (Exception ex)
            {
                objResponse.Err = "Unexpected error in processing request.";
            }
            objResponse.TimeStamp = DateTime.Now.Ticks.ToString();
            return objResponse;
        }

        /// <summary>
        /// Rate Daemon - Get currency exchange rate and update it in DB
        /// </summary>
        /// <returns></returns>
        /// <History>
        /// [Created By]:Jignesh Prajapati - Date:11-May-2017
        /// </History>
        [Route("api/v0/RateDaemon"),HttpPost]
        public async Task<List<APIResponse>> UpdateCurrencyRate()
        {
            List<APIResponse> listResponse = new List<Models.APIResponse>();
            try
            {
                CurrencyExchangeEntities dbcontext = new CurrencyExchangeEntities();
                const string toCurrency = "INR";
                foreach (string inputCurrency in SupportedCurrencyList)
                {
                    APIResponse exchangeReponse = new APIResponse();
                    decimal exchangeRate = await GetExchangeRateFromGoogle(inputCurrency, toCurrency);
                    if(exchangeRate>0)
                    {
                        //Update exchange rate in DB
                        CurrencyRate objCurrency = dbcontext.CurrencyRates.Where(c => c.CurrencyCode == inputCurrency).FirstOrDefault();
                        if(objCurrency!=null && objCurrency.Id>0) //Update 
                        {
                            objCurrency.ExchangeRate = exchangeRate;
                            objCurrency.LastUpdatedTime = DateTime.Now;
                            dbcontext.CurrencyRates.Attach(objCurrency);
                            var entry = dbcontext.Entry(objCurrency);
                            entry.State = System.Data.Entity.EntityState.Modified;
                            dbcontext.SaveChanges();
                        }
                        else //Insert
                        {
                            objCurrency = new CurrencyRate();
                            objCurrency.CurrencyCode = inputCurrency;
                            objCurrency.ExchangeRate = exchangeRate;
                            objCurrency.LastUpdatedTime = DateTime.Now;
                            dbcontext.CurrencyRates.Add(objCurrency);
                            dbcontext.SaveChanges();
                        }

                        exchangeReponse.SourceCurrency = inputCurrency;
                        exchangeReponse.Amount = 1;
                        exchangeReponse.ConversionRate = exchangeRate;
                        exchangeReponse.ReturnCode = 1;
                        exchangeReponse.Err = "Success";
                        exchangeReponse.TimeStamp = DateTime.Now.Ticks.ToString();
                    }
                    else
                    {
                        exchangeReponse.ReturnCode = 0;
                        exchangeReponse.Err = "Error";
                    }
                    listResponse.Add(exchangeReponse);
                }


            }
            catch(Exception ex)
            {
               
            }
            return listResponse;
        }

        #endregion

        #region "Private Methods"
        /// <summary>
        /// Get Exchange rate from google api
        /// </summary>
        /// <param name="fromCurrency"></param>
        /// <param name="toCurrency"></param>
        /// <returns></returns>
        private async Task<decimal> GetExchangeRateFromGoogle(string fromCurrency, string toCurrency)
        {
            decimal exchangeRate = 0;
            try
            {
                var client = new RestClient("http://www.google.com/finance");
                var googleRequest = new RestRequest(string.Format("converter?a=1&from={0}&to={1}", fromCurrency, toCurrency), Method.GET);
                string returnResponse = await client.GetContentAsync(googleRequest);
                if (!string.IsNullOrWhiteSpace(returnResponse))
                {
                    var result = Regex.Matches(returnResponse, "<span class=\"?bld\"?>([^<]+)</span>")[0].Groups[1].Value;
                    string[] returnResult = result.Split(' ');
                    if (returnResult.Length > 0)
                    {
                        decimal.TryParse(returnResult[0], out exchangeRate);
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return exchangeRate;
        }

        #endregion
    }
}