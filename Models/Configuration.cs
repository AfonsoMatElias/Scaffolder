using System.IO;

namespace Scaffolder.Models
{
    public class Configuration
    {
        // Properties
        public string Header { get; set; }
        public string Trailer { get; set; }
        public string Namespace { get; set; }

        private string mOutput;
        public string Output { get => mOutput; set => mOutput = Shared.PathNormalizer(value); }

        private string mTemplate;
        public string Template
        {
            get => mTemplate;
            set => mTemplate = Path.Combine(Application.Instance.CurrentDirectory, "apps", Application.GetSelectedProject.AppName, value);
        }

        // For Models Scaffold
        private string mDbModels;
        public string DbModels { get => mDbModels; set => mDbModels = Shared.PathNormalizer(value); }

        public string[] AditionalsProperties { get; set; }
        

    }
}