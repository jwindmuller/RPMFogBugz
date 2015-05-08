using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPMFogBugz
{
	public class CaseInformation
	{
		public string title {get;set;}
		public int number { get; set; }
		public string url { get; set; }
		public string project { get; set; }
		public string milestone { get; set; }

		public CaseInformation(string title, int number, string url, string project, string milestone)
		{
			this.title = title;
			this.number = number;
			this.url = url;
			this.project = project;
			this.milestone = milestone;
		}
	}
}
