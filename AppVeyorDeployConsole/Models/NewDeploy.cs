using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppVeyorDeployConsole.Models
{
	public class NewDeploy
	{
		public string EnvironmentName { get; set; }
		public string AccountName { get; set; }
		public string ProjectSlug { get; set; }
		public string BuildVersion { get; set; }

		/// <summary>
		/// optional job id with artifacts if build contains multiple jobs
		/// </summary>
		public string BuildJobId { get; set; }

	}


}
