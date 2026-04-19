namespace HouseBroker.Domain.Entities;

public class CommissionConfiguration
{
    public int Id { get; set; }
    public decimal MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal Percentage { get; set; }
}
