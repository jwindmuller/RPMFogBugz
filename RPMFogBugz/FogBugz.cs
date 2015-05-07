using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml.Linq;
using System.Net.Http;
namespace RPMFogBugz
{
	class FogBugz
	{
		private string baseUrl = "https://rpm.fogbugz.com/";

		private static FogBugz _instance;
		public static FogBugz getInstance()
		{
			if (_instance == null)
			{
				_instance = new FogBugz();
			}
			return _instance;
		}

		private HttpClient _client = null;
		private HttpClient client
		{
			get
			{
				if (_client == null)
				{
					_client = new HttpClient();
					_client.BaseAddress = new Uri(this.baseUrl + "api.asp");
				}
				return _client;
			}
		}

		public string token
		{
			get;
			private set;
		}

		private List<CaseInformation> _cases;
		public List<CaseInformation> cases
		{
			private set
			{
				_cases = value;
				this.didUpdateCases(this, new EventArgs());
			}
			get
			{
				return _cases;
			}
		}

		public event EventHandler didUpdateCases;
		public event EventHandler didLogout;
		public void updateCasesData()
		{
			string[] cols = new string[] { "ixBug", "sTitle", "sProject" };

			XElement doc = this.sendRequest(
				string.Format(
					"?token={0}&cmd=search&q={1}&cols={2}",
					this.token,
					"assignedto:\"joaquin\" project:\"development\"",
					string.Join(",", cols)
				)
			);

			FogBugzError error = this.checkError(doc);
			if (error != null)
			{
				// TODO: report error
				return;
			}

			this.cases = (from caseEl in doc.Descendants("case")
						  select new
						  {
							  CaseNumber = caseEl.Attribute("ixBug").Value,
							  CaseTitle = (from titleEl in caseEl.Descendants("sTitle") select titleEl).First().Value,
							  CaseProject = (from projectEl in caseEl.Descendants("sProject") select projectEl).First().Value
						  }).AsEnumerable()
						  .Select(
							 c => new CaseInformation(c.CaseTitle, int.Parse(c.CaseNumber), this.baseUrl + '?' + c.CaseNumber, c.CaseProject)
						  ).ToList();
		}

		public XElement sendRequest(string query)
		{
			Task<HttpResponseMessage> request = this.client.GetAsync(query);
			Task.WaitAll(request);

			Task<string> bodyRead = request.Result.Content.ReadAsStringAsync();
			Task.WaitAll(bodyRead);

			return XElement.Parse(bodyRead.Result);
		}

		public bool login(string email, string password) {
			XElement doc = this.sendRequest(
				string.Format("?cmd=logon&email={0}&password={1}", email, password)
			);

			FogBugzError error = this.checkError(doc);
			if (error != null)
			{
				return false;
			}

			IEnumerable<string> tokens = from tokenEl in doc.Descendants("token")
										 select (string)tokenEl;
			this.token = tokens.First();
			return true;
		}

		public void logout()
		{
			this.token = "";
			this.didLogout(this, new EventArgs());
		}
		FogBugzError checkError(XElement doc)
		{
			IEnumerable<XElement> errors = doc.Descendants("error");
			if (errors.Count() == 0)
			{
				return null;
			}
			XElement error = errors.First();
			if (error == null)
			{
				return null;
			}
			int code = int.Parse( error.Attribute("code").Value );
			string description = error.Value;
			return new FogBugzError(code, description);
		}
	}
}
