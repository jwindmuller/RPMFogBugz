using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPMFogBugz
{
	class FogBugzError
	{
		public int code;
		public string description;

		public FogBugzError(int code, string description)
		{
			this.code = code;
			this.description = description;
		}
	}
}
