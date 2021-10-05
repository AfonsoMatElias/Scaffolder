using System;
using System.IO;
using Scaffolder.Models;

namespace Scaffolder.Scaffold
{
    public class GenerationConditions
    {
        protected bool? YesForAll;
        protected bool? NoForAll;

        public bool FileExistenceHandler(string path, string name, Configuration config)
        {
            if (!File.Exists(path))
                return true;

            if (YesForAll == null && NoForAll == null)
            {
                Logger.Log("");
                Logger.Warn($"Do you want to remake '{config.Header}{name}{config.Trailer}.cs'?");
                Logger.Log("[y -> yes, n - no, ya - yes for all, na - no for all]: ");
                var op = Console.ReadLine();
                Logger.Log("");

                switch (op)
                {
                    case "y": return true;
                    case "ya":
                        YesForAll = true;
                        return true;
                    case "n":
                        Logger.Warn($"WARMING: File '{config.Header}{name}{config.Trailer}.cs' was not created!\n");
                        return false;
                    case "na":
                        NoForAll = true;
                        return false;
                    default:
                        Logger.Warn($"WARMING: File '{config.Header}{name}{config.Trailer}.cs' was not created!\n");
                        return false;
                }
            }
            else
            {
                if (NoForAll == true)
                    return false;

                return true;
            }

        }
    
    
    }
}
