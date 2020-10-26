using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Siav.APFlibrary.Entity
{
	public class AgrafIndexbook
	{
		public string Id { get; set; }
		public string Nome { get; set; }
        public List<Siav.APFlibrary.Entity.AgrafTag> lAgrafTag { get; set; }

    }
}