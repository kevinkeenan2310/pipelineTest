using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERPSalesForceIntegration.Models.Secrets
{
    public class SalesforceAuthSecret
    {
        public string accessToken { get; set; }
        public string refreshToken { get; set; }   
        public string consumerKey { get; set; }
        public string consumerSecret { get; set; } 
        public string url { get; set; }
    }
}
