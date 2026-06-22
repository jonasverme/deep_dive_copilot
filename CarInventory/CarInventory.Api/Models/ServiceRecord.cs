namespace CarInventory.Api.Models;

public class ServiceRecord
{
    public int      Id          { get; set; }
    public int      CarId       { get; set; }
    public Car      Car         { get; set; } = null!;
    public DateTime ServiceDate { get; set; }
    public string   Description { get; set; } = string.Empty;
    public decimal  Cost        { get; set; }
    public string   Technician  { get; set; } = string.Empty;
}
