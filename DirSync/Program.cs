using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Permissions;
using System.Configuration;

namespace dirsync
{
    public class DirectorySyncThing
    {
        public static void Main()
        {
            DirectorySyncThing dst = new DirectorySyncThing();
            dst.Run();
        }


        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void Run()
        {
            // Grab the folder groups from the config file
            List<Array> directories = new List<Array>();
            directories = loadDirectories();

            // Loop through the folders and set up a new watcher on them
            foreach (Array d in directories)
            {
                setupWatcher(d);
            }

            // Wait for the user to quit
            Console.WriteLine("Press \'q\' to quit");
            while (Console.Read() != 'q') ;
        }


        // Load in the settings
        private static List<Array> loadDirectories()
        {
            List<Array> directories = new List<Array>();
            List<string> local_dirs = new List<string>();
            List<string> remote_dirs = new List<string>();

            // Loop through all the config options and grab the relevant ones
            foreach (String s in ConfigurationManager.AppSettings.AllKeys)
            {
                // Grab either of the settings
                if (s.IndexOf("local_dir_") != -1)
                {
                    local_dirs.Add(ConfigurationManager.AppSettings[s]);
                }
                else if (s.IndexOf("remote_dir_") != -1)
                {
                    remote_dirs.Add(ConfigurationManager.AppSettings[s]);
                }
            }

            // Now loop through the collectiond and combine them
            for (int i = 0; i < local_dirs.Count; i++)
            {
                if (local_dirs[i] != "" && remote_dirs[i] != "")
                {
                    string[] dir = { local_dirs[i], remote_dirs[i] };
                    directories.Add(dir);
                }
            }

            return directories;
        }


        private static void setupWatcher(Array directories)
        {
            string local_dir = (string)directories.GetValue(0);
            string remote_dir = (string)directories.GetValue(1);

            // Create a new FileSystemWatcher and set the properties
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = local_dir;

            // Watch all files
            watcher.Filter = "*.*";

            // Watch for anything that changes to do with the files
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

            // Add the event handlers
            watcher.Changed += delegate(object source, FileSystemEventArgs e) { watcher_Changed(e, local_dir, remote_dir); };
            watcher.Created += delegate(object source, FileSystemEventArgs e) { watcher_Changed(e, local_dir, remote_dir); };
            watcher.Deleted += delegate(object source, FileSystemEventArgs e) { watcher_Deleted(e, local_dir, remote_dir); };
            watcher.Renamed += delegate(object source, RenamedEventArgs e) { watcher_Renamed(e, local_dir, remote_dir); };

            // Fire it up
            watcher.EnableRaisingEvents = true;
        }


        private static void debug(string str)
        {
            if (ConfigurationManager.AppSettings["enable_debugging_output"] == "1")
            {
                Console.WriteLine(str);
            }
        }
        

        // File updated
        private static void watcher_Changed(FileSystemEventArgs e, string local_dir, string remote_dir)
        {
            string local_file = e.FullPath;
            string remote_file = e.FullPath.Replace(local_dir, remote_dir);

            debug("File modified" + Environment.NewLine + "  Local file: " + local_file + Environment.NewLine + "  Remote file: " + remote_file);
            File.Copy(local_file, remote_file, true);
        }


        // File renamed
        private static void watcher_Deleted(FileSystemEventArgs e, string local_dir, string remote_dir)
        {
            string local_file = e.FullPath;
            string remote_file = e.FullPath.Replace(local_dir, remote_dir);

            debug("File deleted" + Environment.NewLine + "  Remote file: " + remote_file);
            File.Delete(remote_file);
        }


        // File renamed
        private static void watcher_Renamed(RenamedEventArgs e, string local_dir, string remote_dir)
        {
            string old_file = e.OldFullPath.Replace(local_dir, remote_dir);
            string new_file = e.FullPath.Replace(local_dir, remote_dir);

            debug("Renaming file" + Environment.NewLine + "  Old file: " + old_file + Environment.NewLine + "  New file: " + new_file);
            File.Move(old_file, new_file);
        }
    }
}