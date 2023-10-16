using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Scaffolder.Models;

namespace Scaffolder.Scaffold;

public class ViewModelScaffold : GenerationConditions
{
	public Scaffolders Scaffolders { get; set; }
	public List<Configuration> configs { get; set; }

	private string name;

	public ViewModelScaffold()
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
		try
		{
			var filePath = Path.Combine(config.Output, $"{config.Header}{name}{config.Trailer}.{config.Extension ?? "cs"}");

			if (!this.FileExistenceHandler(filePath, name, config))
				return;

			// Getting the model name and path
			var mappedModels = Scaffolders.Models.ToDictionary(x => x.Name, x => x);
			var model = mappedModels.Get(name);

			var modelLines = Shared.GetModelLines(model.Path);
			var dtoLines = new List<string>();

			for (int i = 0; i < modelLines.Count; i++)
			{
				var line = modelLines[i];

				// Applying already the replacers
				config.Replacers.ForEach(x => line = line.Replace(x.CurrentValue, x.NewValue));

				// if namespace
				if (line.StartsWith("namespace "))
				{
					var @namespace = config.Namespace;
					if (string.IsNullOrEmpty(@namespace))
						@namespace = config.OriginalOutput.Replace("/", ".").Replace("\\", ".");

					// namespace {ABC}
					line = Shared.StringReplacer(line, "namespace", @namespace, 1);
					dtoLines.Add(line);
					continue;
				}

				// if Class
				if (line.Contains("class "))
				{
					line = Shared.StringReplacer(line, "class", $"{model.Name}Dto", 1);
					dtoLines.Add(line);
					continue;
				}

				// if virtual
				if (line.Contains(" virtual "))
				{
					var buildProperty = Shared.BuildProperty(line);
					var type = buildProperty.Type;

					if (line.Contains(nameof(ICollection)))
						line = Shared.StringReplacer(line, "virtual", $"{nameof(IEnumerable)}<{type.Replace(nameof(ICollection) + "<", "").Replace(">", "")}Dto>", 1, true);
					else
						line = Shared.StringReplacer(line, "virtual", $"{type}Dto", 1);

					dtoLines.Add(line);
					continue;
				}

				// Check models in line
				// 1º Check
				var modelInLine = Scaffolders.Models.FirstOrDefault(x => line.Contains($"<{x.Name}>"));

				if (modelInLine != null)
					line = line.Replace($"<{modelInLine.Name}>", $"<{modelInLine.Name}Dto>");
				else
				// 2º Check
				if ((modelInLine = Scaffolders.Models.FirstOrDefault(x => line.Contains($" {x.Name} "))) != null)
					line = line.Replace($" {modelInLine.Name} ", $" {modelInLine.Name}Dto ");

				dtoLines.Add(line);
			}

			// Reading the file by lines
			var mTemplate = String.Join("\n", dtoLines);
			File.WriteAllText(filePath, mTemplate);
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
