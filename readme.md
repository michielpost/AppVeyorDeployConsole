#AppVeyor Deploy Console

This console app lets you combine multiple AppVeyor Deploy Environments into a single group so you can kick off a deployment to a whole group at once.

Example, you have a project which contains a Website, CMS and API.
They each have their own deploy environment on AppVeyor. When you want to deploy a new version of your code, you'll have to trigger 3 deploys though the AppVeyor UI. This can become time consuming.

With this console app, you can create 1 deployment group and include the Web, CMS and API environments and then trigger a deploy.

##Usage
The console app asks for your AppVeyor key so it can communicate on your behalf with the AppVeyor api.
They key is not stored or used for anything else then communicating with AppVeyor.
It's also possible to start the console app with the API key as the first argument.

Group files are saved as simple your-group-name.group.json files.

##Work in progress
This is a first version and it's usable, but I can think of lots of improvements:
- Edit groups
- Track deployment status
- More command line options to start a deployment by giving a set of command line arguments 

Pull requests are welcome!

##AppVeyor
AppVeyor is a great build environment for C# / .Net projects, but managing environments and deployments is currently lacking. My hope is that this project can one day become obsolete, when AppVeyor improves their deployment management functionality.
Discuss and view progress about this here: http://help.appveyor.com/discussions/suggestions/228-environments-and-appveyor-as-deploy-manager
