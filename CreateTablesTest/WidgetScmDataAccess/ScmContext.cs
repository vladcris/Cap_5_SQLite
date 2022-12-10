using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace WidgetScmDataAccess
{
    public class ScmContext
    {
        private DbConnection connection;
        public IEnumerable<PartType> Parts { get; private set; } 
        public IEnumerable<InventoryItem> Inventory { get; private set; } 
        public IEnumerable<Supplier> Suppliers { get; private set;}
       

        public ScmContext(DbConnection connection)
        {
            this.connection = connection;
            ReadParts();
            ReadInventory();
            ReadSuppliers();
        }

        public DbTransaction BeginTransaction()
        {
            return connection.BeginTransaction();
        }

        private void ReadParts()
        {
            using(var command = connection.CreateCommand())
            {
                command.CommandText = @"Select Id, Name 
                                        From  PartType";
                using( var reader = command.ExecuteReader() )
                {
                    var parts = new List<PartType>();

                    while(reader.Read())
                    {
                        parts.Add(
                            new PartType 
                            { 
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                            System.Console.WriteLine(reader.GetString(1));
                    }
                    
                    Parts = parts;
                }
            }
        }

        private void ReadInventory()
        {
            using(var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT PartTypeId, Count, OrderThreshold FROM InventoryItem";

                using(var reader = command.ExecuteReader())
                {
                    var items = new List<InventoryItem>();
                    Inventory = items;

                    while(reader.Read())
                    {
                        var item = new InventoryItem
                        {
                            PartTypeId = reader.GetInt32(reader.GetOrdinal("PartTypeId")),
                            Count = reader.GetInt32(1),
                            OrderThreshHold = reader.GetInt32(reader.GetOrdinal("OrderThreshold"))
                        };

                        items.Add(item);
                        item.part = Parts.Single(x => x.Id == item.PartTypeId);
                    }

                }
            }
        }

        private void ReadSuppliers()
        {
            var command = connection.CreateCommand();
            command.CommandText = @"SELECT 
                Id, Name, Email, PartTypeId 
                FROM Supplier";
            var reader = command.ExecuteReader();
            var suppliers = new List<Supplier>();
            Suppliers = suppliers;
            while (reader.Read())
            {
                var supplier = new Supplier() {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Email = reader.GetString(2),
                PartTypeId = reader.GetInt32(3)
                };
                suppliers.Add(supplier);
                supplier.Part = Parts.Single(p => p.Id == supplier.PartTypeId);
            }  
        }

        public void CreatePartCommand(PartCommand partCommand)
        {
            using(var command = connection.CreateCommand())
            {
                command.CommandText = @"insert into PartCommand
                                        (PartTypeId, PartCount, Command)
                                        values
                                        (@partType, @partCount, @command);
                                        select last_insert_rowid();";
                AddParameter(command, "@partType", partCommand.PartTypeId);
                AddParameter(command, "@partCount", partCommand.PartCount);
                AddParameter(command, "@command", partCommand.Command.ToString());

                long partCommandId = (long)command.ExecuteScalar();
                partCommand.Id = (int)partCommandId;
            }
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

        public IEnumerable<PartCommand> GetPartCommands()
        {
            using(var command = connection.CreateCommand())
            {
                command.CommandText = @"select Id, PartTypeId, PartCount, Command
                                        from PartCommand
                                        order by Id;";

                using (var reader = command.ExecuteReader())
                {
                    var partCommands = new List<PartCommand>();

                    while(reader.Read())
                    {
                        var partCommand = new PartCommand
                        {
                            Id = reader.GetInt32(0),
                            PartTypeId = reader.GetInt32(1),
                            PartCount = reader.GetInt32(2),
                            Command = (PartCountOperation)Enum.Parse(typeof(PartCountOperation), reader.GetString(3))
                            //Command = reader.GetString(3) == "Add" ? PartCountOperation.Add : PartCountOperation.Remove
                        };

                        partCommand.Part = Parts.Single(x => x.Id == partCommand.PartTypeId);
                        partCommands.Add(partCommand);
                    }

                    return partCommands;
                }
            }
        }

        public void UpdateInventoryItem(int partTypeId, int count, DbTransaction transaction)
        {
            using(var command = connection.CreateCommand())
            {
                if(transaction != null)
                command.Transaction = transaction;

                command.CommandText = @"update InventoryItem
                                        set Count = @count
                                        where PartTypeId = @partTypeId;";
                AddParameter(command, "@count", count);
                AddParameter(command, "@partTypeId", partTypeId);

                command.ExecuteNonQuery();
            }
        }

        public void DeletePartCommand(int id, DbTransaction transaction)
        {
            using ( var command = connection.CreateCommand())
            {
                if(transaction != null)
                    command.Transaction = transaction;

                command.CommandText = @"delete from PartCommand
                                        where Id = @id;";
                AddParameter(command, "@id", id);
                command.ExecuteNonQuery();
            }
        }

        public void CreateOrder(Order order)
        {
            var transaction = connection.BeginTransaction();
            try
            {
                var command = connection.CreateCommand();
            
                command.Transaction = transaction;
                command.CommandText = @"insert into [Order]
                                        (SupplierId, PartTypeId, PartCount, PlacedDate)
                                        values
                                        (@supplierId, @partTypeId, @partCount, @placedDate);
                                        select last_insert_rowid();";
                AddParameter(command, "@supplierId", order.SupplierId);
                AddParameter(command, "@partTypeId", order.PartTypeId);
                AddParameter(command, "@partCount", order.PartCount);
                AddParameter(command, "@placedDate", order.PlacedDate);

                long id = (long)command.ExecuteNonQuery();
                order.Id = (int)id;

                command = connection.CreateCommand();

                command.Transaction = transaction;
                command.CommandText = @"insert into SendEmailCommand
                                        ([To], Subject, Body) VALUES 
                                        (@To, @Subject, @Body";
                AddParameter(command, "@To", order.Supplier.Email);
                AddParameter(command, "@Subject", $"Order #{id} for {order.Part.Name}");
                AddParameter(command, "@",  $"Please send {order.PartCount}" +
                                            $" items of {order.Part.Name} to Widget Corp");
                command.ExecuteNonQuery();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        public IEnumerable<Order> GetOrders()
        {
            var command = connection.CreateCommand();
            command.CommandText = @"SELECT 
                Id, SupplierId, PartTypeId, PartCount, PlacedDate, FulfilledDate 
                FROM [Order]";
            var reader = command.ExecuteReader();
            var orders = new List<Order>();
            while (reader.Read())
            {
                var order = new Order() {
                Id = reader.GetInt32(0),
                SupplierId = reader.GetInt32(1),
                PartTypeId = reader.GetInt32(2),
                PartCount = reader.GetInt32(3),
                PlacedDate = reader.GetDateTime(4),
                FufilledDate = reader.IsDBNull(5) ? 
                    default(DateTime?) : reader.GetDateTime(5)
                };
                order.Part = Parts.Single(p => p.Id == order.PartTypeId);
                order.Supplier = Suppliers.First(s => s.Id == order.SupplierId);
                orders.Add(order);
            }  

            return orders;
        }

    

    }
}