using Microsoft.Win32;
using System.Diagnostics;

namespace CodeRedUninstaller
{
    internal class Program
    {
        static void Write(string str)
        {
            Console.WriteLine("[" + DateTime.Now.ToString() + "] " + str);
        }

        static bool IsValidProcess(Process process)
        {
            try
            {
                if ((process != null) && (process.Id > 8)) // A process with an id of 8 or lower is a system process, we shouldn't be trying to access those.
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Write("(IsValidProcess) Exception: " + ex.ToString());
            }

            return false;
        }

        static List<Process> GetFilteredProcesses(string filter)
        {
            List<Process> returnList = new();

            foreach (Process process in Process.GetProcessesByName(filter))
            {
                if (IsValidProcess(process))
                {
                    returnList.Add(process);
                }
            }

            return returnList;
        }


        // Launcher should already be closed by this point, so this is just for the sake of sanity checking.
        static bool CloseLauncher()
        {
            List<Process> launchers = GetFilteredProcesses("CodeRedLauncher");

            foreach (Process launcher in launchers)
            {
                if (IsValidProcess(launcher))
                {
                    Write("(CloseLauncher) Found launcher running, attempting to close \"" + launcher.Id.ToString() + "\"...");

                    try
                    {
                        launcher.Kill();

                        if (!launcher.WaitForExit(5000))
                        {
                            Write("(CloseLauncher) Failed to close launcher, timeout treshhold reached!");
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Write("(CloseLauncher) Exception: " + ex.ToString());
                        return false;
                    }
                }
            }

            return true;
        }

        static void Main(string[] args)
        {
            RegistryKey? coderedKey = Registry.CurrentUser.OpenSubKey("CodeRedModding");

            if (coderedKey != null)
            {
                object? installObject = coderedKey.GetValue("InstallPath");

                if (installObject != null)
                {
                    string? installPath = installObject.ToString();

                    if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                    {
                        if (CloseLauncher())
                        {
                            Write("Deleting install path \"" + installPath + "\"...");

                            try
                            {
                                Directory.Delete(installPath, true);
                            }
                            catch (Exception ex)
                            {
                                Write("Failed to delete install oath, either lacking permissions or being blocked by antivirus!");
                                Write("Exception: " + ex.ToString());
                            }
                        }
                    }
                    else
                    {
                        Write("Given install path \"" + installPath + "\" doesn't exist, nothing to delete.");
                    }
                }
                else
                {
                    Write("Failed to find install path via the registry!");
                }

                coderedKey.Close();

                try
                {
                    Write("Deleting registry keys...");
                    Registry.CurrentUser.DeleteSubKey("CodeRedModding");
                    Write("Uninstall complete!");
                }
                catch (Exception ex)
                {
                    Write("Failed to delete registry key!");
                    Write("Exception: " + ex.ToString());
                }
            }
            else
            {
                Write("Failed to find registry key, CodeRed is not installed!");
            }

            Console.ReadKey();
        }
    }
}