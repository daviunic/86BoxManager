using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics; 
using System.IO;
using System.Linq;
using System.Reflection; 
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms; 
namespace _86boxManager
{
    /// <summary>
    /// 2020-11-21  Connor Hyde (Starfrost)
    /// 
    /// I didn't like how this code was written - indeed it says "TODO: Rewrite" so I thought, hey why don't I do it.
    /// 
    /// I moved the settings loading code to this small static class. This allows the settings to be referred to anywhere in 86Box Manager
    /// easier.
    /// </summary>
    public static class ApplicationSettings
    {
        /// <summary>
        /// Directory of 86Box.exe
        /// </summary>
        public static string EXEDir { get; set; }

        /// <summary>
        /// Directory of 86Box.cfg
        /// </summary>
        public static string CFGDir { get; set; }

        /// <summary>
        /// Path of the log to dump 86box logging to if EnableLogging is enabled
        /// </summary>
        public static string LogPath { get; set; }

        /// <summary>
        /// Launch timeout length (WHY IS THIS A STRING, inherited data type from earlier code)
        /// </summary>
        public static string LaunchTimeout { get; set; }
        
        /// <summary>
        /// Minimise 86Box Manager on VM start
        /// </summary>
        public static bool MinimizeOnVMStart { get; set; }

        /// <summary>
        /// Show the 86Box console.
        /// </summary>
        public static bool ShowConsole { get; set; }

        /// <summary>
        /// When 86Manager is minimised, minimise to tray.
        /// </summary>
        public static bool MinimizeToTray { get; set; }

        /// <summary>
        /// When 86Manager is closed, close to tray.
        /// </summary>
        public static bool CloseToTray { get; set; }

        /// <summary>
        /// Enable logging
        /// </summary>
        public static bool EnableLogging { get; set; }

        /// <summary>
        /// Enable grid lines in the VM list
        /// </summary>
        public static bool EnableGridLines { get; set; }

        public static void LoadSettings()
        {
            try
            {
                RegistryKey regkey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\86Box", false); //Open the key as read only

                //If the key doesn't exist yet, fallback to defaults
                if (regkey == null)
                {
                    MessageBox.Show("86Box Manager settings could not be loaded. This is normal if you're running 86Box Manager for the first time. Default values will be used.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    RestoreToDefaults(true); 

                    SaveSettings(); //This will write the default values to the registry
                }
                else
                {
                    EXEDir = regkey.GetValue("EXEdir").ToString();
                    CFGDir = regkey.GetValue("CFGdir").ToString();
                    LaunchTimeout = regkey.GetValue("LaunchTimeout").ToString();
                    LogPath = regkey.GetValue("LogPath").ToString();
                    MinimizeOnVMStart = Convert.ToBoolean(regkey.GetValue("MinimizeOnVMStart"));
                    ShowConsole = Convert.ToBoolean(regkey.GetValue("ShowConsole"));
                    MinimizeToTray = Convert.ToBoolean(regkey.GetValue("MinimizeToTray"));
                    CloseToTray = Convert.ToBoolean(regkey.GetValue("CloseToTray"));
                    EnableLogging = Convert.ToBoolean(regkey.GetValue("EnableLogging"));
                    EnableGridLines = Convert.ToBoolean(regkey.GetValue("EnableGridLines"));
                }

                regkey.Close();
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show($"Exception! {ex.Message}\n\n{ex.StackTrace}");
#else
                MessageBox.Show("An error has occurred. Please provide the following information to the developer:\n" + ex.Message + "\n" + ex.StackTrace, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
#endif
                RestoreToDefaults(false); 
            }
        }
        
        /// <summary>
        /// Restore to defaults
        /// </summary>
        public static void RestoreToDefaults(bool CreateSettings)
        {
            //Create the key and reopen it for write access

            if (CreateSettings)
            {
                if (!CreateSettingsKey())
                {
                    MessageBox.Show("An error occurred creating settings.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            
            CFGDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\86Box VMs\";
            EXEDir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\86Box\";
            MinimizeOnVMStart = false;
            ShowConsole = true;
            MinimizeToTray = false;
            CloseToTray = false;
            EnableLogging = false;
            LaunchTimeout = "5000";
            EnableGridLines = false;
            LogPath = "";
        }

        /// <summary>
        /// Create the settings keys.
        /// </summary>
        /// <returns>BOOL - success code</returns>
        private static bool CreateSettingsKey()
        {
            try
            {
                RegistryKey RegKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\86Box");

                // 2020-11-23   Connor Hyde (starfrost) add additional error checking

                if (RegKey == null) return false;

                RegKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\86Box", true);

                if (RegKey == null) return false;

                RegKey.CreateSubKey("Virtual Machines");
                return true; 
            }
            catch (ArgumentNullException err)
            {
#if DEBUG
                MessageBox.Show($"86Box Manager [Debug] ArgumentNullException occurred in Settings.CreateSettingsKey() [static] (bug?): {err.Message} \n\n{err.StackTrace}");
#endif
                return false;
            }
            catch (IOException err)
            {
#if DEBUG
                MessageBox.Show($"86Box Manager [Debug] IOException occurred in Settings.CreateSettingsKey() [static]: {err.Message} \n\n{err.StackTrace}");
#endif
                return false;
            }
        }

        public static bool SaveSettings()
        {
            if (EnableLogging && string.IsNullOrWhiteSpace(LogPath))
            {
                DialogResult result = MessageBox.Show("Using an empty or whitespace string for the log path will prevent 86Box from logging anything. Are you sure you want to use this path?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.No)
                {
                    return false;
                }
            }
            if (!File.Exists(EXEDir + "86Box.exe") && !File.Exists(EXEDir + @"\86Box.exe"))
            {
                DialogResult result = MessageBox.Show("86Box.exe could not be found in the directory you specified, so you won't be able to use any virtual machines. Are you sure you want to use this path?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                
                if (result == DialogResult.No)
                {
                    return false;
                }
            }
            try
            {
                RegistryKey regkey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\86Box", true); //Try to open the key first (in read-write mode) to see if it already exists
                
                if (regkey == null) //Regkey doesn't exist yet, must be created first and then reopened
                {
                    Registry.CurrentUser.CreateSubKey(@"SOFTWARE\86Box");
                    regkey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\86Box", true);
                    regkey.CreateSubKey("Virtual Machines");
                }

                //Store the new values, close the key, changes are saved
                regkey.SetValue("EXEdir", EXEDir, RegistryValueKind.String);
                regkey.SetValue("CFGdir", CFGDir, RegistryValueKind.String);
                regkey.SetValue("MinimizeOnVMStart", MinimizeOnVMStart, RegistryValueKind.DWord);
                regkey.SetValue("ShowConsole", ShowConsole, RegistryValueKind.DWord);
                regkey.SetValue("MinimizeToTray", MinimizeToTray, RegistryValueKind.DWord);
                regkey.SetValue("CloseToTray", CloseToTray, RegistryValueKind.DWord);
                // why not just store this as an int...i might change this
                regkey.SetValue("LaunchTimeout", Convert.ToInt32(LaunchTimeout), RegistryValueKind.DWord);
                regkey.SetValue("EnableLogging", EnableLogging, RegistryValueKind.DWord);
                regkey.SetValue("LogPath", LogPath, RegistryValueKind.String);
                regkey.SetValue("EnableGridLines", EnableGridLines, RegistryValueKind.DWord);
                regkey.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show("An error has occurred. Please provide the following information to the developer:\n" + ex.Message + "\n" + ex.StackTrace, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the 86Box build number as a 
        /// </summary>
        /// <returns></returns>
        public static FileVersionInfo Get86BoxVersion() => FileVersionInfo.GetVersionInfo($@"{EXEDir}\86Box.exe");

        //Checks if all controls match the currently saved settings to determine if any changes were made
        public static bool CheckForChanges()
        {
            RegistryKey regkey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\86Box");

            bool Result;

            try
            {
                Result = (
                    EXEDir != regkey.GetValue("EXEdir").ToString() ||
                    CFGDir != regkey.GetValue("CFGdir").ToString() ||
                    LogPath != regkey.GetValue("LogPath").ToString() ||
                    LaunchTimeout != regkey.GetValue("LaunchTimeout").ToString() ||
                    MinimizeOnVMStart != Convert.ToBoolean(regkey.GetValue("MinimizeOnVMStart")) ||
                    ShowConsole != Convert.ToBoolean(regkey.GetValue("ShowConsole")) ||
                    MinimizeToTray != Convert.ToBoolean(regkey.GetValue("MinimizeToTray")) ||
                    CloseToTray != Convert.ToBoolean(regkey.GetValue("CloseToTray")) ||
                    EnableLogging != Convert.ToBoolean(regkey.GetValue("EnableLogging")) ||
                    EnableGridLines != Convert.ToBoolean(regkey.GetValue("EnableGridLines")));

                return Result;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                return true; //For now let's just return true if anything goes wrong
            }
            finally
            {
                regkey.Close();
            }
        }

        
    }
}
