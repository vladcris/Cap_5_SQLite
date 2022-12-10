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
        }
    }
}