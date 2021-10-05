using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Scaffolder.Models;

namespace Scaffolder.Scaffold
{
    public class ViewModelScaffold : GenerationConditions
    {
        public Scaffolders Scaffolders { get; set; }
        public List<Configuration> configs { get; set; }

        private string template = null;
        private string name;

        public ViewModelScaffold()
        {
            this.Scaffolders = Application.GetSelectedProject.GetScaffolders();
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

            if (!name.Contains(","))
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
                var filePath = Path.Combine(config.Output, $"{config.Header}{name}{config.Trailer}.cs");

                if (!this.FileExistenceHandler(filePath, name, config))
                    return;

                // Getting the model name and path
                var model = this.Scaffolders.Models.FirstOrDefault(x => x.Name == name);
                // Reading the file by lines
                var mTemplate = this.template.Replace("@-Namespace-@", config.Namespace)
                                             .Replace("@-Model-@", model.Name);

                // Extracting properties in the model
                var properties = Shared.GetModelProperties(model.Path);

                var identation = "\n        ";
                var propIndetifier = "@-Prop-@";

                // Creating the template value setter function
                void setter(string value)
                    => mTemplate = mTemplate.Replace(propIndetifier, $"{identation}{value}{propIndetifier}");

                // Adding the extra fields
                config.AditionalsProperties?
                    .ToList()
                    .ForEach(item => setter(item));

                for (int i = 1; i <= properties.Count; i++)
                {
                    // Getting the line
                    var line = properties.ElementAt(i - 1).Trim();

                    // Changing the model property type
                    if (line.Contains(" virtual "))
                    {
                        var mLine = line.Trim();
                        var lineSplited = mLine.Split(" ");
                        var type = lineSplited[2];

                        if (type.StartsWith(nameof(ICollection)))
                        {
                            type = type.Replace(nameof(ICollection) + "<", "").Replace(">", "");
                            lineSplited[2] = $"{nameof(IEnumerable)}<{type}Dto>";
                        }
                        else
                        {
                            lineSplited[2] = $"{type}Dto";
                        }

                        line = string.Join(" ", lineSplited);
                    }

                    setter(line);
                }

                File.WriteAllText(filePath, mTemplate.Replace(propIndetifier, ""));
                Logger.Done($"file {config.Header}{name}{config.Trailer}.cs created.");
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
                    $"({ Application.GetSelectedProject.AppName }|Yellow)",
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
                    var name = Console.ReadLine();

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
