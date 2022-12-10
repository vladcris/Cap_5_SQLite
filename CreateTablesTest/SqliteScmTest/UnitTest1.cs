using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using WidgetScmDataAccess;

namespace SqliteScmTest;

public class UnitTest1 : IClassFixture<SamplesScmDataFixture>
{
    private SamplesScmDataFixture samplesScmDataFixture;
    private ScmContext _scmContext;
    public UnitTest1(SamplesScmDataFixture samples)
    {
        this.samplesScmDataFixture = samples;
        _scmContext = new ScmContext(samples.Connection);
    }
    [Fact]
    public void Test1()
    {
        var parts = _scmContext.Parts;

        Assert.Equal(1, parts.Count());
        var part = parts.First();
        Assert.Equal("8289 L-shaped plate", part.Name);   
    }

    [Fact]
    public void TestInventoryItems()
    {
        var items = _scmContext.Inventory;
        Assert.Equal(1, items.Count());
        var item = items.First();
        Assert.Equal(100, item.Count);   
        Assert.Equal(10, item.OrderThreshHold);
        Assert.Equal("8289 L-shaped plate", item.part.Name);   
    }

    [Fact]
    public void TestName()
    {
        AddPartCommand(1, 15, PartCountOperation.Add);

        var command = _scmContext.GetPartCommands().First();

        Assert.Equal("8289 L-shaped plate", command.Part.Name);
        Assert.Equal(15, command.PartCount);
        Assert.Equal(PartCountOperation.Add, command.Command);

        AddPartCommand(1, 15, PartCountOperation.Remove);
    }

    [Fact]
    public void TestUpdateInventory()
    {
        AddPartCommand(1, 15, PartCountOperation.Add);

        var command = _scmContext.GetPartCommands().First();
        Assert.Equal(15, command.PartCount);
        
        var item = _scmContext.Inventory.First();
        var startCount = _scmContext.Inventory.First().Count;

        AddPartCommand(1, 5, PartCountOperation.Remove);

        var inventory = new Inventory(_scmContext);
        inventory.UpdateInventory();
        Assert.Equal(startCount + 10, item.Count);
        
    }

    public void AddPartCommand(int partId, int partCount, PartCountOperation operation)
    {
        var partCommand = new PartCommand
        {
            PartTypeId = partId,
            PartCount = partCount,
            Command = operation
        };

        _scmContext.CreatePartCommand(partCommand);
    }

    [Fact]
    public void TestCreateOrder()
    {
        var placedDate = DateTime.Now;
        var supplier = _scmContext.Suppliers.First();
        var order = new Order
        {
            SupplierId = 1,
            PartTypeId = 1,
            PartCount = 100,
            PlacedDate = placedDate
        };

        Assert.Throws<NullReferenceException>( () => _scmContext.CreateOrder(order));

        var command = new SqliteCommand(
        @"SELECT Count(*) FROM [Order] WHERE 
        SupplierId=@supplierId AND 
        PartTypeId=@partTypeId AND
        PlacedDate=@placedDate AND
        PartCount=10 AND
        FulfilledDate IS NULL",
        samplesScmDataFixture.Connection);
        AddParameter(command, "@supplierId", supplier.Id); 
        AddParameter(command, "@partTypeId", supplier.PartTypeId);
        AddParameter(command, "@placedDate", placedDate);
        Assert.Equal(0, (long)command.ExecuteScalar()); 
    }

      [Fact]
    public void TestUpdateInventoryWithOrder()
    {
        var item = _scmContext.Inventory.First();
        var totalCount = item.Count;
        _scmContext.CreatePartCommand(new PartCommand() {
            PartTypeId = item.PartTypeId,
            PartCount = totalCount,
            Command = PartCountOperation.Remove
        });

        var inventory = new Inventory(_scmContext);
        inventory.UpdateInventory();
        var order = _scmContext.GetOrders().FirstOrDefault(
            o => o.PartTypeId == item.PartTypeId &&
            !o.FufilledDate.HasValue);
        Assert.NotNull(order);

        _scmContext.CreatePartCommand(new PartCommand() {
            PartTypeId = item.PartTypeId,
            PartCount = totalCount,
            Command = PartCountOperation.Add
        });

        inventory.UpdateInventory();
        Assert.Equal(totalCount, item.Count);
    }


    private void AddParameter(DbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            if(value == null)
                throw new ArgumentNullException("value");
            Type t = value.GetType();
            if(t == typeof(int))
                p.DbType = DbType.Int32;
            else if(t == typeof(string))
                p.DbType = DbType.String;
            else if(t == typeof(DateTime))
                p.DbType = DbType.DateTime;
            else throw new ArgumentException($"Unrecoqnized type: {t.ToString()}", "value");
            p.Direction = ParameterDirection.Input;
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }


}