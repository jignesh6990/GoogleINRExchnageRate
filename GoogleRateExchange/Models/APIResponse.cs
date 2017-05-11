using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GoogleRateExchange.Models
{
    /// <summary>
    /// Currency Exchange API response class
    /// </summary>
    /// <History>
    /// [Created By]:Jignesh Prajapati - Date:11-May-2017
    /// </History>
    public class APIResponse
    {
        public int ReturnCode { get; set; }
        public string SourceCurrency { get; set; }
        public decimal ConversionRate { get; set; }

        public decimal Amount { get; set; }

        public decimal Total { get; set; }

        public string Err { get; set; }

        public string TimeStamp { get; set; }
    }

    /// <summary>
    /// class for Currency Exchange Input parameters
    /// </summary>
    /// <History>
    /// [Created By]:Jignesh Prajapati - Date:11-May-2017
    /// </History>
    public class CurrencyExchanceInput
    {
        public string SourceCurrency { get; set; } = "USD"; //Default source currency USD

        public decimal Amount { get; set; } = 1; //Default Amout value=1
    }
}