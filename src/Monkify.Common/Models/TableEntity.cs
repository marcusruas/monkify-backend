using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Common.Models
{
    public class TableEntity
    {
        public TableEntity()
        {
            Id = Guid.NewGuid();
            CreatedDate = DateTime.Now;
            Deleted = false;
        }

        public Guid Id { get; private set; }
        public DateTime CreatedDate { get; private set; }
        public DateTime? UpdatedDate { get; set; }
        public bool Deleted { get; set; }
    }
}
