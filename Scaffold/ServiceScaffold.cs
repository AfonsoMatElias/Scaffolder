﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Scaffolder.Models;

namespace Scaffolder.Scaffold
{
	public class ServiceScaffold : GenerationConditions
	{
		public Scaffolders Scaffolders { get; set; }
		public List<Configuration> configs { get; set; }

		private string template = null;
		private string name;

		public ServiceScaffold()
		{
			this.Scaffolders = Application.Instance.SelectedProject.GetScaffolders();
			this.name = this.GetType().Name.Replace("Scaffold", "");
			this.configs = this.Scaffolders.Get(this.name);
		}

		public void Run(string name)
		{
			void exec(string m)
			{
				if (!this.Scaffolders.ModelExists(m)) return;
				configs.ForEach(cf => this.Generate(m, cf));
			}

			if (!name.Contains(','))
				exec(name);
			else
				name.Split(",").Select(s => s.Trim()).ToList().ForEach(m =>
				{
					configs.ForEach(cf =>
					{
						this.Generate(m, cf);
					});
				});
		}

		public void Generate(string name, Configuration config)
		{
			this.template = Shared.LoadTemplate(this.template, config, this.configs.Count);

			try
			{
				var filePath = Path.Combine(config.Output, $"{config.Header}{name}{config.Trailer}.{config.Extension ?? "cs"}");

				if (!this.FileExistenceHandler(filePath, name, config))
					return;

				// Applying already the replacers
				config.Replacers.ForEach(x => template = template.Replace(x.CurrentValue, x.NewValue));

				File.WriteAllText(filePath, template.Replace("@-Model-@", name).Replace("@-Namespace-@", config.Namespace));
				Logger.Done($"file {config.Header}{name}{config.Trailer}.{config.Extension ?? "cs"} created.");
			}
			catch (Exception ex)
			{
				Logger.Error($"Error: {ex.Message} ; {ex.InnerException?.Message ?? ""}");
			}
		}

		public void Init()
		{
		Begin:

			Console.Clear();

			Logger.Log(
				string.Join(" -> ", new[] {
					$"Project",
					$"({ Application.Instance.SelectedProject.AppName }|Yellow)",
					$"({ this.name }|Yellow)"
				})
			);
			Logger.Log("");

			var option = Shared.OptionsSpreader<GenerationOptions>(name =>
			{
				if (name == GenerationOptions.Exit.ToString()) return "";
				return "Generate ";
			});

			switch (option)
			{
				case GenerationOptions.One_By_One:
					Logger.Log("\nType the Class Name: ");
					var name = Logger.ReadLine(this.Scaffolders.Models.Select(x => x.Name).ToArray());

					this.Run(name);
					Shared.Pause();
					break;

				case GenerationOptions.All_At_Once:
					this.Scaffolders.Models.ForEach(model => configs.ForEach(cf => this.Generate(model.Name, cf)));
					Shared.Pause();
					break;

				case GenerationOptions.Exit:
					return;
			}

			goto Begin;
		}
	}
}