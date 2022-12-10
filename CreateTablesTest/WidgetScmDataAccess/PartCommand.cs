using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WidgetScmDataAccess
{
    public class PartCommand
    {
        public int Id { get; set; }
        public int PartTypeId { get; set; }
        public PartType Part { get; set; }
        public int PartCount { get; set; }
        public PartCountOperation Command { get; set; }    
    }

    public enum PartCountOperation
    {
        Add,
        Remove
    }
}