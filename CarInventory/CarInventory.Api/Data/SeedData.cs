using CarInventory.Api.Models;

namespace CarInventory.Api.Data;

public static class SeedData
{
    public static void Apply(CarInventoryDbContext db)
    {
        if (db.Cars.Any()) return; // already seeded

        var owners = new List<Owner>
        {
            new() { FirstName = "Alice",   LastName = "Dupont",   Email = "alice.dupont@example.com",   Phone = "+32 471 11 22 33" },
            new() { FirstName = "Bruno",   LastName = "Martens",  Email = "bruno.martens@example.com",  Phone = "+32 472 44 55 66" },
            new() { FirstName = "Jonas",   LastName = "Vermeulen",  Email = "jonas.vermeulen@elmos.be", Phone = "+32 473 77 88 99" },
            new() { FirstName = "David",   LastName = "Renard",   Email = "david.renard@example.com",   Phone = "+32 474 00 11 22" },
        };
        db.Owners.AddRange(owners);
        db.SaveChanges();

        var cars = new List<Car>
        {
            new() { Make="BMW",        Model="320i",      Year=2021, Vin="WBA5A5C51FD520123", Color="Black",  Mileage=32000,  Price=28500m, Status="Available", OwnerId=owners[0].Id },
            new() { Make="Volkswagen", Model="Golf 8",    Year=2022, Vin="WVWZZZ1KZAM012345", Color="White",  Mileage=18000,  Price=24900m, Status="Available", OwnerId=owners[1].Id },
            new() { Make="Tesla",      Model="Model 3",   Year=2023, Vin="5YJ3E1EA5PF456789", Color="Red",    Mileage=9000,   Price=42000m, Status="Reserved",  OwnerId=owners[2].Id },
            new() { Make="Audi",       Model="A4",        Year=2020, Vin="WAUZZZ8K5LA098765", Color="Silver", Mileage=55000,  Price=22000m, Status="Available", OwnerId=null        },
            new() { Make="Mercedes",   Model="C-Class",   Year=2022, Vin="WDD2050421R234567", Color="Blue",   Mileage=21000,  Price=38000m, Status="Sold",      OwnerId=owners[3].Id },
            new() { Make="Peugeot",    Model="308",       Year=2021, Vin="VF3LBBHYBHS678901", Color="Grey",   Mileage=41000,  Price=16500m, Status="Available", OwnerId=null        },
            new() { Make="Renault",    Model="Megane",    Year=2020, Vin="VF1BM0I0H51789012", Color="Orange", Mileage=62000,  Price=13200m, Status="Available", OwnerId=owners[0].Id },
            new() { Make="Ford",       Model="Mustang",   Year=2019, Vin="1FA6P8TH6K5123456", Color="Yellow", Mileage=48000,  Price=31000m, Status="Available", OwnerId=null        },
            new() { Make="Toyota",     Model="Corolla",   Year=2023, Vin="JTDBRMFE0P3890123", Color="White",  Mileage=5000,   Price=26500m, Status="Available", OwnerId=owners[2].Id },
            new() { Make="Hyundai",    Model="Tucson",    Year=2022, Vin="KMHEC41B1HA901234", Color="Green",  Mileage=27000,  Price=29900m, Status="Reserved",  OwnerId=owners[1].Id },
        };
        db.Cars.AddRange(cars);
        db.SaveChanges();

        var records = new List<ServiceRecord>
        {
            new() { CarId=cars[0].Id, ServiceDate=new DateTime(2023,6,15),  Description="Oil change + brake inspection",       Cost=189m,  Technician="Marc V." },
            new() { CarId=cars[0].Id, ServiceDate=new DateTime(2024,1,20),  Description="Annual service + tire rotation",       Cost=345m,  Technician="Marc V." },
            new() { CarId=cars[1].Id, ServiceDate=new DateTime(2023,9,10),  Description="DSG fluid change",                     Cost=220m,  Technician="Sophie D." },
            new() { CarId=cars[2].Id, ServiceDate=new DateTime(2024,3,5),   Description="Software update + brake fluid",        Cost=95m,   Technician="Tesla Service" },
            new() { CarId=cars[3].Id, ServiceDate=new DateTime(2022,11,22), Description="Timing belt + water pump replacement",  Cost=870m,  Technician="Kurt B." },
            new() { CarId=cars[3].Id, ServiceDate=new DateTime(2023,5,14),  Description="Air filter + spark plugs",             Cost=130m,  Technician="Kurt B." },
            new() { CarId=cars[4].Id, ServiceDate=new DateTime(2023,8,30),  Description="Full service + AC regas",              Cost=420m,  Technician="Hans W." },
            new() { CarId=cars[6].Id, ServiceDate=new DateTime(2023,3,18),  Description="Front brake pads + discs",             Cost=310m,  Technician="Sophie D." },
            new() { CarId=cars[7].Id, ServiceDate=new DateTime(2023,7,4),   Description="Oil change + cabin filter",            Cost=165m,  Technician="Marc V." },
            new() { CarId=cars[8].Id, ServiceDate=new DateTime(2024,2,12),  Description="First service check",                  Cost=75m,   Technician="Toyota Service" },
        };
        db.ServiceRecords.AddRange(records);
        db.SaveChanges();
    }
}
