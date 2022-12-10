using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScmDataAccess
{
    public class Order
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }
        public int PartTypeId { get; set; }
        public PartType Part { get; set; }
        public int PartCount { get; set; }
        public DateTime PlacedDate { get; set; }
        public DateTime? FufilledDate { get; set; }

    }
}