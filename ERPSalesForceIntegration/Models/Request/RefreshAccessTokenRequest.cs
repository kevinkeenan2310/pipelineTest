namespace ERPSalesForceIntegration.Models.Request
{
    public class RefreshAccessTokenRequest
    {
        public string Grant_Type { get; set; }
        public string Client_Id { get; set; }
        public string Client_Secret { get; set; }
        public string Refresh_Token { get; set; }
    }
}
