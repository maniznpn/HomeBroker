namespace HomeBroker.Application.Common
{
    public class JwtConfiguration
    {
        public string JwtSecurityKey { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
    }
}
