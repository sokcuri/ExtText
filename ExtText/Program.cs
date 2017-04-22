using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExtText
{
    static class Program
    {
        static int SimpleBoyerMooreSearch(byte[] haystack, byte[] needle)
        {
            int[] lookup = new int[256];
            for (int i = 0; i < lookup.Length; i++) { lookup[i] = needle.Length; }

            for (int i = 0; i < needle.Length; i++)
            {
                lookup[needle[i]] = needle.Length - i - 1;
            }

            int index = needle.Length - 1;
            var lastByte = needle.Last();
            while (index < haystack.Length)
            {
                var checkByte = haystack[index];
                if (haystack[index] == lastByte)
                {
                    bool found = true;
                    for (int j = needle.Length - 2; j >= 0; j--)
                    {
                        if (haystack[index - needle.Length + j + 1] != needle[j])
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                        return index - needle.Length + 1;
                    else
                        index++;
                }
                else
                {
                    index += lookup[checkByte];
                }
            }
            return -1;
        }
        /// <summary>
        /// 해당 응용 프로그램의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()

        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Dump File|*.dmp";
            fileDialog.Title = "Select a Dump File";

            if (fileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            FileStream fs;
            try
            {
                fs = new FileStream(fileDialog.FileName, FileMode.Open);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "ExtText", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            List<string> savedText = new List<string>();
           

            Byte[] readByte = new Byte[1024 * 1024];
            Byte[] savedByte = new Byte[1024 * 1024];
            Byte[] ptn_css = { 0x68, 0x72, 0x65, 0x66, 0x3D, 0x22, 0x2E, 0x2E, 0x2F, 0x63, 0x73, 0x73, 0x2F };
            Byte[] ptn_html_close = { 0x3C, 0x2F, 0x68, 0x74, 0x6D, 0x6C, 0x3E };
            while (fs.Position != fs.Length)
            {
                int maxSize = fs.Read(readByte, 0, 1024 * 1024);

                for (int i = 0; i < maxSize; i++)
                {
                    if (readByte[maxSize - i - 1] == 0x00)
                    {
                        fs.Seek(fs.Position - i, SeekOrigin.Begin);
                        maxSize -= i;
                        break;
                    }
                }

                while (true)
                {
                    int pos = SimpleBoyerMooreSearch(readByte, ptn_css);
                    if (pos != -1)
                    {
                        for (int i = 0; i < pos; i++)
                        {
                            if (readByte[pos - i] == 0x00)
                            {
                                pos -= i - 1;
                                break;
                            }
                        }
                        int nSize;
                        for (nSize = 0; nSize < maxSize - pos; nSize++)
                        {
                            if (readByte[pos + nSize] == 0x00)
                                break;
                        }
                        Buffer.BlockCopy(readByte, pos, savedByte, 0, nSize);

                        int pos2 = SimpleBoyerMooreSearch(savedByte, ptn_html_close);
                        string s = Encoding.UTF8.GetString(savedByte, 0, pos2 + 7);
                        savedText.Add(s);

                        Byte[] dummyData = new Byte[s.Length];
                        Buffer.BlockCopy(dummyData, 0, readByte, pos, dummyData.Length);
                    }
                    else break;
                }
            }
            fs.Dispose();

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "XML 파일|*.xml";
            saveFileDialog.Title = "Select a Save Folder";

            if (saveFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }
            string saveFileName = saveFileDialog.FileName;
            for (int i = saveFileName.Length - 1; i >= 0; i--)
            {
                if (saveFileName[i] == '.')
                {
                    saveFileName = saveFileName.Substring(0, i);
                    break;
                }
            }

            int seq = 0;
            foreach (var text in savedText)
            {
                try
                {
                    using (
                        var sw = new StreamWriter(
                        new FileStream($"{saveFileName}_{seq++}.xml", FileMode.Create), Encoding.UTF8))
                    {
                        sw.Write(text);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "ExtText", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
               
            }
            

            MessageBox.Show("Completed", "ExtText", MessageBoxButtons.OK, MessageBoxIcon.Information);

            //Application.Run(new Form1());
        }
    }
}
