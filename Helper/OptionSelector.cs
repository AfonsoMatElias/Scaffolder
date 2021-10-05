using System;
using System.Collections.Generic;

namespace Scaffolder
{
    public class OptionSelector
    {
        public string Select(ICollection<string> keys)
        {
           var index = 1;
            // Printing the options
            foreach (var key in keys)
                Logger.Log($"({index++}|Yellow). { key }");

            Logger.ILog("Choose an option above: ");

            // Reading the selected option
            var typedKey = Console.ReadKey().KeyChar;
            Logger.Log(""); // Giving some space

            return typedKey.ToString();
        }
    }
}