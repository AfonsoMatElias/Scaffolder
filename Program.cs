using System;
using System.IO;
using System.Linq;
using Scaffolder.Scaffold;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using Scaffolder.Models;

namespace Scaffolder
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.Log("... Welcome to Scaffolder ...\n");

            // Initializing the configurations and Running the application
            new Application()
                .Init()
                .Run();

            Logger.Log("\nThanks for your choice!!! 😊");
            Shared.Pause();
        }
    }
}