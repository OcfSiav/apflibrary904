using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCF_Ws.Model
{
    public class MainDoc
    {
        public string Filename { get; set; }
        public bool IsSigned { get; set; }
        public string Extension { get; set; }
        public byte[] oByte { get; set; }
    }
}
