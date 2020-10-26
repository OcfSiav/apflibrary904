
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace OCF_Ws.Model
{
	[DataContract]
	public class Outcome
	{
		[DataMember(IsRequired = true)]
		public int iCode{ get; set; }
		[DataMember(IsRequired = true)]
		public string sDescription{ get; set; }
		[DataMember(IsRequired = true)]
		public string sTransactionId { get; set; }
	}
}
