using AppVeyorDeployConsole.Models;
using AppVeyorDeployConsole.Services;
using ConsoleMenu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AppVeyorDeployConsole
{
	class Program
	{
		private static AppVeyorService _appVeyorService;
		private static GroupFileService _groupFileService = new GroupFileService();

		static void Main(string[] args)
		{
			MainAsync(args).Wait();
			// or, if you want to avoid exceptions being wrapped into AggregateException:
			//  MainAsync().GetAwaiter().GetResult();

			Console.WriteLine("Press ENTER to exit.");
			Console.ReadLine();
		}


		static Task MainAsync(string[] args)
		{
			string appVeyorKey = null;

			//Get AppVeyor API key from first argument
			if (args.Any())
				appVeyorKey = args.First();

			//Or ask for AppVeyor API key
			if (string.IsNullOrWhiteSpace(appVeyorKey))
			{
				Console.WriteLine("Enter AppVeyor API key:");
				appVeyorKey = Console.ReadLine();

				if (string.IsNullOrEmpty(appVeyorKey))
				{
					Console.WriteLine("No key, exiting");
					return null;
				}
			}

			//Initialize the service
			_appVeyorService = new AppVeyorService(appVeyorKey);

			var choices = new List<Func<Task>>
			{
				CreateEnvironmentGroup,
				NewDeploy
			};
			//Present menu of choices
			var menu = new TypedMenu<Func<Task>>(choices, "Choose a number", x => x.Method.Name);

			var picked = menu.Display();
			picked().Wait();


			return Task.FromResult(string.Empty);
		}

		/// <summary>
		/// Flow to create a new environment group
		/// </summary>
		/// <returns></returns>
		private static async Task CreateEnvironmentGroup()
		{
			Console.WriteLine("Creating new Environment Group");

			//Get GroupName
			Console.WriteLine("Enter new group name:");
			var groupName = Console.ReadLine();

			EnvironmentGroup newGroup = new EnvironmentGroup();
			newGroup.Name = groupName;

			//Get all projects
			var allProject = await _appVeyorService.GetAllAppVeyorProjects();

			Console.WriteLine("Pick a Project:");
			var menu = new TypedMenu<AppVeyorProject>(allProject, "Choose a number", x => x.Name);
			var pickedProject = menu.Display();
			newGroup.Project = pickedProject;

			//Get all environments 
			var allEnv = await _appVeyorService.GetAllAppVeyorEnvironments();

			AskForEnvironment(newGroup, allEnv);

			//Save group
			//Write JSON to file
			_groupFileService.SaveGroupToFile(newGroup);
			Console.WriteLine("Saved Environment Group {0} with {1} environments", newGroup.Name, newGroup.Environments.Count);

		}

		/// <summary>
		/// Ask for an environment to add to a group
		/// </summary>
		/// <param name="newGroup"></param>
		/// <param name="allEnv"></param>
		private static void AskForEnvironment(EnvironmentGroup newGroup, List<AppVeyorEnvironment> allEnv)
		{
			Console.WriteLine("Pick an Environment:");

			var menu = new TypedMenu<AppVeyorEnvironment>(allEnv.Except(newGroup.Environments).ToList(), "Choose a number", x => x.Name);
			var picked = menu.Display();

			newGroup.Environments.Add(picked);

			Console.WriteLine("Add another environment? (Y/N) (default Y)");
			var answer = Console.ReadLine();

			if (answer.Equals("N", StringComparison.InvariantCultureIgnoreCase))
				return;
			else
				AskForEnvironment(newGroup, allEnv);

		}

		/// <summary>
		/// Flow to trigger a new deploy
		/// </summary>
		/// <returns></returns>
		private static async Task NewDeploy()
		{
			Console.WriteLine("Which Group do you want to deploy to?");

			//Get all groups from disk
			string path = Directory.GetCurrentDirectory();
			var allFiles = Directory.GetFiles(path, "*.group.json");

			if (!allFiles.Any())
			{
				Console.WriteLine("No group.json files found! Please create an environment group first.");
				return;
			}

			List<EnvironmentGroup> groupList = allFiles.Select(_groupFileService.ReadGroupFile).ToList();

			var menu = new TypedMenu<EnvironmentGroup>(groupList, "Choose a number", x => x.Name);
			var picked = menu.Display();

			Console.WriteLine("Going to deploy: " + picked.Name);
			Console.WriteLine("Project: " + picked.Project.Name);
			picked.Environments.ForEach(x => Console.WriteLine("Env: " + x.Name));

			//Get version to deploy
			Console.WriteLine("Enter build version to deploy:");
			var buildVersion = Console.ReadLine();

			Console.WriteLine("Are you sure? (Y/N) (default N)");
			var answer = Console.ReadLine();

			if (answer.Equals("Y", StringComparison.InvariantCultureIgnoreCase))
			{
				//Create new deploys for each environment in this group
				await DeployEnvironmentGroup(picked, buildVersion);

			}
		}

		/// <summary>
		/// Trigger deploys for the provided environment group
		/// </summary>
		/// <param name="picked"></param>
		/// <param name="buildVersion"></param>
		/// <returns></returns>
		private static async Task DeployEnvironmentGroup(EnvironmentGroup picked, string buildVersion)
		{
			foreach(var env in picked.Environments)
			{
				bool success = await _appVeyorService.DeployEnvironment(picked.Project, env, buildVersion);

				if (success)
					Console.WriteLine("Success, started deploy to: " + env.Name);
				else
					Console.WriteLine("FAILED to start deploy to: " + env.Name);

			}

			Console.WriteLine("Deployments are NOT finished yet.");
			Console.WriteLine("Check the progress of your deployements on ci.appveyor.com");

		}

	}
}
