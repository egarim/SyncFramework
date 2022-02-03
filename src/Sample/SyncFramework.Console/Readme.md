# SynFramework.Console

It is a sample console application. It shows the power of `SyncFramework`. The console application is using 2 client nodes, which can be switched from console options.

The sample is tested with `Mssqlserver` and `PostgreSQL` database. The sample is configured with a [PostgreSQL](#https://www.postgresql.org/) database. User must change connections string in `appsettings.js` file.

```
{
  "ConnectionStrings": {
    //Postgress Connection strings
    "DeltaStoreClient1": "Server=localhost;User Id=postgres;Password=yourdbpassword;Port=5432;Database=deltaStore1;",
    "DeltaStoreClient2": "Server=localhost;User Id=postgres;Password=yourdbpassword;Port=5432;Database=deltaStore2;",
    "ClientDB1": "Server=localhost;User Id=postgres;Password=yourdbpassword;Port=5432;Database=order1;",
    "ClientDB2": "Server=localhost;User Id=postgres;Password=yourdbpassword;Port=5432;Database=order2;"
    //Ms sqlserver connection strings
    //"DeltaStoreClient1": "Server=.\\sqlexpress;Database=ClientDeltaStore; Trusted_Connection=True;",
    //"DeltaStoreClient2": "Server=.\\sqlexpress;Database=ClientDeltaStore;Trusted_Connection=True;",
    //"ClientDB1": "Server=.\\sqlexpress;Initial Catalog=order1;MultipleActiveResultSets=True;Trusted_Connection=True",
    //"ClientDB2": "Server=.\\sqlexpress;Initial Catalog=order2;MultipleActiveResultSets=True;Trusted_Connection=True",
  },
  //"ServerUrl": "https://localhost:44343",
  "ServerUrl": "http://localhost:5000",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.EntityFrameworkCore": "Debug",
      "Microsoft.EntityFrameworkCore.Database.Command": "Debug"
    }
  },
  "AllowedHosts": "*"
}
```
Each client is using its own deltastore `"DeltaStoreClient1"` and `"DeltaStoreClient2"`. Both client are using dedicated `SynServer`. The server url can be set in "ServerUrl".
The server can be configured to use IIS/IIS Express/or standalone. 

> The **SyncServer** must be running before using this application

> I tried to use single deltastore for both client nodes but there is strange bug. The query didn't fetch records. Once the bug is fixed then we can utilize same deltastore for multiple nodes

The console application has the following options.

![Console App](./resources/console-app.png "console app")
View Order data
![View Orders](./resources/view-data.png "view data")

The options are self explanatory

The app is using very simple Order and OrderDetails entities.
**Order Entity**
```
public class Order
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
    public string Description { get; set; }
    public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.Now;
    public ICollection<OrderDetail> OrderDetails { get; set; }

}
```
**OrderDetails Entity**
```
public class OrderDetail
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int Quantity { get; set; }
    public Guid OrderId { get; set; }
    [ForeignKey("OrderId")]
    public Order Order { get; set; }
}
```
