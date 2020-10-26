using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siav.APFlibrary.Model
{
    public class InternalAttachment
    {
        public string Name { get; set; }
        public string ArchiveId { get; set; }
        public string Note { get; set; }
        public Guid GuidCard { get; set; }
    }
}
