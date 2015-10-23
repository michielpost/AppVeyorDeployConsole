using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppVeyorDeployConsole.Models
{
	public class AppVeyorProject
	{
		public int ProjectId { get; set; }
		public int AccountId { get; set; }
		public string AccountName { get; set; }
		public string Name { get; set; }
		public string Slug { get; set; }
	}
}
