namespace CarInventory.Api.Models;

public class Owner
{
    public int    Id        { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName  { get; set; } = string.Empty;
    public string Email     { get; set; } = string.Empty;
    public string Phone     { get; set; } = string.Empty;

    public List<Car> Cars   { get; set; } = [];

    public string FullName => $"{FirstName} {LastName}";
}
