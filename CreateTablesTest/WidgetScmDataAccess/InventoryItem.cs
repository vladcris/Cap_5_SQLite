using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WidgetScmDataAccess
{
    public class InventoryItem
    {
        public int PartTypeId { get; set; }
        public PartType part { get; set; }
        public int Count { get; set; }
        public int OrderThreshHold { get; set; }
    }
}