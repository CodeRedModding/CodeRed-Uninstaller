﻿using Microsoft.Win32;
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
                if (process != null
                    && (process.Id > 8) // A process with an id of 8 or lower is a system process, we shouldn't be trying to access those.
                    && (process.MainWindowHandle != IntPtr.Zero))
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
            List<Process> returnList = new List<Process>();
            Process[] processList = Process.GetProcessesByName(filter);

            foreach (Process process in processList)
            {
                if (IsValidProcess(process))
                {
                    if (process.ProcessName.Contains(filter) || process.MainWindowTitle.Contains(filter))
                    {
                        returnList.Add(process);
                    }
                }
            }

            return returnList;
        }

        static bool CloseLauncher()
        {
            List<Process> launchers = GetFilteredProcesses("CodeRedLauncher");

            foreach (Process launcher in launchers)
            {
                if (IsValidProcess(launcher))
                {
                    Write("Found launcher running, attempting to close \"" + launcher.Id.ToString() + "\"...");

                    try
                    {
                        launcher.Kill();
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
                    Write("Failed to delete registry key: " + ex.ToString());
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