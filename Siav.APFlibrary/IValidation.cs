using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siav.APFlibrary
{
	interface IValidation
	{
		Boolean Validate(out string description);
	}
}
