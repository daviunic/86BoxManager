using Microsoft.Win32; 
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression; 
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _86boxManager
{
    /// <summary>
    /// 2020-11-23  Connor Hyde (starfrost013)
    /// </summary>
    public class SettingsExporter
    {
        public bool ZipUpRegFile { get; set; }
        
        // we probably need input validation support and result classes,
        // but i already wrote too much code for this pull request
        public bool ExportSettings()
        {
            SaveFileDialog SFD = new SaveFileDialog();
            
            // If the user has chosen to zip up the reg file, modify the dialog accordingly
            if (!ZipUpRegFile)
            {
                SFD.Title = "Export to Registry File";
                SFD.Filter = "Registry file (*.reg)|*.reg";
            }
            else
            {
                SFD.Title = "Export to Zipped Registry File";
                SFD.Filter = "ZIP archive (*.zip)|*.zip";
            }

            SFD.ShowDialog();

            if (SFD.FileName == "")
            {
                return true;
            }
            else
            {
                // Export to .reg
                if (!ZipUpRegFile)
                {
                    return ExportReg(SFD.FileName);
                }
                else
                {
                    return ExportZip(SFD.FileName); 
                }
            }

            return false; 
        }

        private bool ExportReg(string FileName)
        {
            return WriteRegFile(FileName);
        }

        private bool ExportZip(string FileName)
        {
            try
            {
                // UI moment
                FileName = FileName.Replace(".zip", ".reg");
                bool Result = WriteRegFile(FileName);

                if (!Result)
                {
                    return false;
                }
                else
                {
                    try
                    {
                        return CompressRegFile(FileName);
                    }
                    // some more specific error messages for specific scenarios...
                    catch (DirectoryNotFoundException)
                    {
                        MessageBox.Show($"Directory not found!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    catch (ObjectDisposedException err)
                    {
#if DEBUG
                        MessageBox.Show($"Likely a bug - {err.Message}\n\n{err.StackTrace}");
#endif
                        return false; 
                    }
                    catch (PathTooLongException)
                    {
                        MessageBox.Show($"Path too long - please make sure the path to the file is less than 260 characters!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    catch (IOException err)
                    {
#if DEBUG
                        MessageBox.Show($"[Debug] An error occurred while exporting: {err.Message}\n\n{err.StackTrace}");
#endif
                        return false;
                    }
                }
            }
            catch (DirectoryNotFoundException)
            {
                MessageBox.Show($"Directory not found!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (PathTooLongException)
            {
                MessageBox.Show($"Path too long - please make sure the path to the file is less than 260 characters!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (IOException err)
            {
#if DEBUG
                MessageBox.Show($"[Debug] An error occurred while exporting: {err.Message}\n\n{err.StackTrace}");
#endif
                return false;
            }
        }
     
        /// <summary>
        /// Write a .REG file from the settings
        /// </summary>
        /// <returns></returns>
        private bool WriteRegFile(string FileName)
        {
            using (StreamWriter SW = new StreamWriter(new FileStream(FileName, FileMode.OpenOrCreate)))
            {

                string BoxRoot = @"SOFTWARE\86Box";

                WriteRegFileHeader(SW);

                RegistryKey BoxKey = Registry.CurrentUser.OpenSubKey(BoxRoot);

                SW.WriteLine($@"[HKEY_CURRENT_USER\{BoxRoot}]");
                // 86box main key

                WriteRegKeyValues(SW, BoxKey);

                // loop through all subkeys of the 86box key    

                foreach (string BoxSubkeyName in BoxKey.GetSubKeyNames())
                {
                    RegistryKey BoxSubkey = BoxKey.OpenSubKey(BoxSubkeyName);

                    SW.WriteLine($@"[HKEY_CURRENT_USER\{BoxRoot}\{BoxSubkeyName}]");

                    WriteRegKeyValues(SW, BoxSubkey); 
                }

                SW.Close();

                return true; 
            }

        }

        private void WriteRegKeyValues(StreamWriter SW, RegistryKey BoxKey)
        {
            foreach (string BoxValueName in BoxKey.GetValueNames())
            {
                object BoxValue = BoxKey.GetValue(BoxValueName);
                // switch statement looks nicer here probably
                if (BoxValue is int)
                {
                    // write a DWORD
                    WriteRegFileDwordValue(SW, BoxValueName, (int)BoxValue);
                }
                else if (BoxValue is string)
                {
                    WriteRegFileStringValue(SW, BoxValueName, (string)BoxValue);
                }
                else if (BoxValue is byte[])
                {
                    WriteRegFileBinaryValue(SW, BoxValueName, (byte[])BoxValue); 
                }
                else
                {
#if DEBUG
                    MessageBox.Show("Unknown registry data type encountered! Skipping..."); 
#endif
                    continue;
                }
            }
        }

        // maybe best not in this class? idk
        private void WriteRegFileHeader(StreamWriter SW) => SW.WriteLine("Windows Registry Editor Version 5.00\n");

        /// <summary>
        /// Write a registry file dword value.
        /// </summary>
        /// <param name="SW">The stream to use for writing.</param>
        /// <param name="KeyName">The key name to write.</param>
        /// <param name="DWordValue">The DWord value to write.</param>
        /// <returns></returns>
        private bool WriteRegFileDwordValue(StreamWriter SW, string KeyName, int DWordValue)
        {
            SW.Write($"\"{KeyName}\"=dword:");
            string HexString = DWordValue.ToString("X");
            
            HexString = HexString.ToLower(); // safety purposes

            if (HexString.Length > 8)
            {
                // dword moment
                return false;
            }
            else
            {
                // best to split this off into another function
                // just not worth doing it unless this code would be used multiple times
                int LeadingZeros = 8 - HexString.Length;

                // write the leading zeros
                for (int i = 0; i < LeadingZeros; i++) SW.Write("0");

                // write the hex string
                SW.Write(HexString);
                SW.Write("\n"); 

                return true; 
            }

        }

        /// <summary>
        /// Writes a string value to a .reg file. 
        /// </summary>
        /// <param name="SW"></param>
        /// <param name="KeyName"></param>
        /// <param name="KeyValue"></param>
        private void WriteRegFileStringValue(StreamWriter SW, string KeyName, string KeyValue) => SW.Write($"\"{KeyName}\"=\"{KeyValue}\"\n");

        private void WriteRegFileBinaryValue(StreamWriter SW, string KeyName, byte[] KeyData)
        {
            SW.Write($"\"{KeyName}\"=hex:");

            for (int i = 0; i < KeyData.Length; i++)
            {
                byte KeyDataByte = KeyData[i]; 
                // X = hexadecimal format
                string KeyDataString = KeyDataByte.ToString("X");

                // this is dumb and a method would better serve this but i don't have that much time rn (starfrost, 2020-11-23)
                if (KeyDataString.Length == 1)
                {
                    KeyDataString = $"0{KeyDataString}";
                }

                KeyDataString = KeyDataString.ToLower();

                SW.Write($"{KeyDataString}");
                
                // do not write if last byte of hex data
                if ((KeyData.Length - i) >= 2) SW.Write(',');
                
            }

            SW.Write("\n"); 
        }

        /// <summary>
        /// Compress a registry file.
        /// </summary>
        /// <param name="RegFileName"></param>
        /// <returns></returns>
        private bool CompressRegFile(string RegFileName)
        {
            // force path to be relative

            RegFileName = GetSafeFileName(RegFileName); 

            string ZipFileName = $"{RegFileName}_c.zip";

            using (ZipArchive ZA = new ZipArchive(new FileStream(ZipFileName, FileMode.Create), ZipArchiveMode.Create))
            {

                string[] InArchiveFileNames = RegFileName.Split('\\');
                string InArchiveFileName = InArchiveFileNames[InArchiveFileNames.Length - 1]; 
                
                ZipArchiveEntry RegEntry = ZA.CreateEntryFromFile(RegFileName, InArchiveFileName);

                // Get directory name
                string DirName = ApplicationSettings.CFGDir;
                // Compress the VMs folder

                RecursiveAddition(ZA, DirName);

                ZA.Dispose(); 
            }



            File.Delete(RegFileName);
            
            return true; 
        }

        // should be moved to a utilities class and mdae an extension method
        private string GetSafeFileName(string FileName)
        {
            char[] InvalidCharacters = Path.GetInvalidFileNameChars();

            string Final = null;

            foreach (char InvalidCharacter in InvalidCharacters)
            {
                Final = FileName.Replace(InvalidCharacter.ToString(), ""); 
            }

            if (Final == null) throw new Exception("BUGBUG: Somehow, we failed to enumerate invalid file name characters.");

            return Final;
        }

        private bool RecursiveAddition(ZipArchive ZA, string DirName)
        {


            foreach (string DirectoryName in Directory.GetDirectories(DirName))
            {
                foreach (string FileName in Directory.GetFiles(DirectoryName))
                {
                    // just to be safe
                    string OperationFileName = GetSafeFileName(FileName);

                    string InArchiveFileName = FileName.Replace($@"{ApplicationSettings.CFGDir}", "");


                    ZA.CreateEntryFromFile(OperationFileName, InArchiveFileName);

                    if (Directory.GetDirectories(DirectoryName).Length > 0)
                    {
                        string SDirName = GetSafeFileName(DirectoryName); 
                        RecursiveAddition(ZA, SDirName); 
                    }
                }
            }


            return true; 
        }
    }
}
