using System.Collections.Generic;
using System.IO;

namespace Scaffolder.Models
{
    public class Configuration
    {
        // Properties
        public string Header { get; set; }
        public string Trailer { get; set; }
        public string Namespace { get; set; }
        public string OriginalOutput { get; set; }
        public string Extension { get; set; }


        private string mOutput;
        public string Output { get => mOutput; set => mOutput = Shared.PathNormalizer(OriginalOutput = value); }


        private string mTemplate;
        public string Template
        {
            get => mTemplate;
            set => mTemplate = Path.Combine(Application.Instance.CurrentDirectory, "apps", Application.Instance.SelectedProject.AppName, value);
        }

        // For Models Scaffold

        private string mDbModels;
        public string DbModels { get => mDbModels; set => mDbModels = Shared.PathNormalizer(value); }

        public List<string> AditionalsProperties { get; set; }
        public List<Replacer> Replacers { get; set; } = new List<Replacer>();
        public IDictionary<string, string> Builders { get; set; } = new Dictionary<string, string>();

    }

    public class Replacer
    {
        public string CurrentValue { get; set; }
        public string NewValue { get; set; }
    }
}