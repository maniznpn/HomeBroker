using HomeBroker.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HouseBroker.Domain.Entities;

public class PropertyListing : BaseEntity
{
    public PropertyType PropertyType { get; set; }
    [MaxLength(100)]
    public string Location { get; set; }
    public decimal Price { get; set; }
    [MaxLength(4000)]
    public string Description { get; set; }
    [MaxLength(2000)]
    public string Features { get; set; }
    public decimal EstimatedCommission { get; set; }
    public long BrokerId { get; set; }
    public string ImageUrl { get; set; }
}

public class BaseEntity
{

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    public DateTime CreatedOn { set; get; } = DateTime.UtcNow;
    public DateTime? UpdatedOn { set; get; }
    public long CreatedBy { set; get; }
    public long? UpdatedBy { set; get; }
    public bool IsDeleted { set; get; } = false;
}