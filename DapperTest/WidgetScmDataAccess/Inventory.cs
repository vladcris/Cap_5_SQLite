using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WidgetScmDataAccess
{
    public class Inventory
    {
        private ScmContext _scmContext;

        public Inventory(ScmContext scmContext)
        {
            _scmContext = scmContext;
        }

        public void UpdateInventory()
        {
            foreach(var cmd in _scmContext.GetPartCommands())
            {
                var item = _scmContext.Inventory.Single(x => x.PartTypeId == cmd.PartTypeId);
                var oldCount = item.Count;

                if (cmd.Command == PartCountOperation.Add)
                    item.Count += cmd.PartCount;
                else 
                    item.Count -= cmd.PartCount;

                var transaction = _scmContext.BeginTransaction();
                try
                {
                    _scmContext.UpdateInventoryItem(item.PartTypeId, item.Count, transaction);

                    _scmContext.DeletePartCommand(cmd.PartTypeId, transaction);

                    transaction.Commit();
                }
                catch{
                    transaction.Rollback();
                    item.Count = oldCount;
                    throw;
                }
            }

            var orders = _scmContext.GetOrders();

                foreach (var item in _scmContext.Inventory)
                {
                    if (item.Count < item.OrderThreshHold &&
                    orders.FirstOrDefault(o => 
                    o.PartTypeId == item.PartTypeId && 
                    !o.FufilledDate.HasValue) == null)
                    {
                    OrderPart(item.Part, item.OrderThreshHold);
                    }
                }

        }

         public void OrderPart(PartType part, int count)
        {
            var order = new Order() {
                PartTypeId = part.Id,
                PartCount = count,
                PlacedDate = DateTime.Now
            };
            order.Part = _scmContext.Parts.Single(p => p.Id == order.PartTypeId);
            order.Supplier = _scmContext.Suppliers.First(s => s.PartTypeId == part.Id);
            order.SupplierId = order.Supplier.Id;
            _scmContext.CreateOrder(order);
            }
    }
}