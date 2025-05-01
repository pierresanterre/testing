using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security;
using System.Threading;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private static void ColorWriteLine(string text, ConsoleColor consoleColor)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = consoleColor;
            Console.WriteLine(text);
            Console.ForegroundColor = oldColor;
        }

        public static void Success(string text)
        {
            ColorWriteLine(text, ConsoleColor.Green);
        }

        public static void Warning(string warning)
        {
            ColorWriteLine(warning, ConsoleColor.Yellow);
        }

        public static void Error(string failure)
        {
            ColorWriteLine(failure, ConsoleColor.Red);
        }

        public class AscendingComparer<T> : IComparer<T>
        {
            public int Compare(T? x, T? y)
            {
                return Comparer<T>.Default.Compare(x, y);
            }
        }

        public class DescendingComparer<T> : IComparer<T>
        {
            public int Compare(T? x, T? y)
            {
                return Comparer<T>.Default.Compare(y, x);
            }
        }

        private bool running = false;
        private CancellationTokenSource? cancellationTokenSource;

        public void UpdateListView(ListViewItem[] listViewItems)
        {
            // https://www.webdevtutor.net/blog/c-update-ui-from-thread#google_vignette
            this.Invoke((MethodInvoker)delegate
            {
                listView1.Items.Clear();
                for (int i = 0; i < listViewItems.Length; i++)
                {
                    listView1.Items.Add(listViewItems[i]);
                }
            });
        }

        delegate D ProcessOneDirectory<D, S>(string directoryName, D[] recurseDirectories, string[] fileNames, S globalState);

        private static D ProcessAllFilesInDirectoryRecursive<D, S>(string currentDirectory, ProcessOneDirectory<D, S> processOneDirectory, S globalState, CancellationToken cancellationToken)
        {
            string[] directories;
            string[] files;

            EnumerationOptions enumerationOptions = new EnumerationOptions();
            enumerationOptions.IgnoreInaccessible = true;
            enumerationOptions.RecurseSubdirectories = false;
            enumerationOptions.AttributesToSkip = 0;

            if (cancellationToken.IsCancellationRequested)
            {
                return default(D);
            }

            try
            {
                directories = Directory.GetDirectories(currentDirectory, "*", enumerationOptions);
            }
            catch (Exception exception)
            {
                Warning($"Failed to process directories in {currentDirectory}, exception={exception.Message}");
                directories = new string[0];
            }

            try
            {
                files = Directory.GetFiles(currentDirectory, "*", enumerationOptions);
            }
            catch (Exception exception)
            {
                Warning($"Failed to process files in {currentDirectory}, exception={exception.Message}");
                files = new string[0];
            }

            D[] resultsOfDirectories = new D[directories.Length];

            Parallel.For(0, directories.Length, i =>
            {
                resultsOfDirectories[i] = ProcessAllFilesInDirectoryRecursive(directories[i], processOneDirectory, globalState, cancellationToken);
            });

            D rd = processOneDirectory(currentDirectory, resultsOfDirectories, files, globalState);

            return rd;
        }

        private static D ProcessAllFilesInDirectory<D, S>(string currentDirectory, ProcessOneDirectory<D, S> processOneDirectory, S globalState, CancellationToken cancellationToken)
        {
            D result = ProcessAllFilesInDirectoryRecursive(currentDirectory, processOneDirectory, globalState, cancellationToken);
            return result;
        }

        private class BigDirectory : IComparable
        {
            public string path;
            public long size;

            public BigDirectory(string path, long size)
            {
                this.path = path;
                this.size = size;
            }

            public string SizeAsString()
            {
                string sizeString = size.ToString("N0").PadLeft(17);
                return sizeString;
            }

            public override string ToString()
            {
                string sizeString = SizeAsString();
                return $"{sizeString}, {path}";
            }

            public int CompareTo(object? right)
            {
                if (right == null)
                {
                    throw new Exception("CompareTo failure 1");
                }

                BigDirectory? rightBigDirectory = right as BigDirectory;
                if (rightBigDirectory == null)
                {
                    throw new Exception("CompareTo failure 2");
                }

                int compare = size.CompareTo(rightBigDirectory.size);
                if (compare != 0)
                {
                    return compare;
                }

                int result = ToString().CompareTo(rightBigDirectory.ToString());
                return result;
            }
        }

        private class BigDirectoryGlobalState
        {
            public BigDirectory[] topDirectories;
            private int topCount;
            private int count;
            public long filter;
            public object lockObject;
            public IComparer<BigDirectory> comparer;
            public Form1 form;

            public void ConcurrentAdd(BigDirectory bigDirectory)
            {
                lock (lockObject)
                {
                    bool update = false;

                    int searchCount;

                    if (count == 0)
                    {
                        topDirectories[0] = bigDirectory;
                        searchCount = 1;
                        update = true;
                    }
                    else
                    {
                        searchCount = Math.Min(count, topCount);
                        int index = Array.BinarySearch(topDirectories, 0, searchCount, bigDirectory, comparer);
                        if (index >= 0)
                        {
                            // We have a match?!. Assume we insert before then
                            if (index < topCount)
                            {
                                update = true;
                            }
                            else
                            {
                                // Did not beat the last element;
                            }
                        }
                        else
                        {
                            if (index > (-topCount))
                            {
                                index = -index - 1;
                                update = true;
                            }
                            else
                            {
                                // Did not beat the last element;
                            }
                        }

                        if (update)
                        {
                            if (index < topCount - 1)
                            {
                                Array.Copy(topDirectories, index, topDirectories, index + 1, topCount - index - 1);
                            }
                            topDirectories[index] = bigDirectory;
                        }
                    }

                    count++;
                    if (update)
                    {
                        ListViewItem[] listViewItems = new ListViewItem[searchCount];
                        for (int i = 0; i < searchCount; i++)
                        {
                            string[] strings = new string[2] { topDirectories[i].SizeAsString(), topDirectories[i].path };
                            listViewItems[i] = new ListViewItem(strings);
                        }
                        form.UpdateListView(listViewItems);
                    }
                }
            }

            public BigDirectoryGlobalState(long filter, int topCount, bool ascending, Form1 form)
            {
                this.topCount = topCount;
                this.filter = filter;

                topDirectories = new BigDirectory[topCount];
                count = 0;
                lockObject = new object();
                if (ascending)
                {
                    comparer = new AscendingComparer<BigDirectory>();
                }
                else
                {
                    comparer = new DescendingComparer<BigDirectory>();
                }

                this.form = form;
            }
        }

        private static BigDirectory processOneDirectoryBigDirectory(string directoryName, BigDirectory[] recurseDirectories, string[] fileNames, BigDirectoryGlobalState globalState)
        {
            long localSize = 0;
            for (int i = 0; i < recurseDirectories.Length; i++)
            {
                BigDirectory bd = recurseDirectories[i];
                if (bd != null)
                {
                    localSize += bd.size;
                }
            }

            for (int i = 0; i < fileNames.Length; i++)
            {
                FileInfo fileInfo = new FileInfo(fileNames[i]);
                localSize += fileInfo.Length;
            }

            BigDirectory bigDirectoryAdd = new BigDirectory(directoryName, localSize);
            BigDirectory bigDirectoryResult = new BigDirectory(directoryName, localSize);

            if (localSize > globalState.filter)
            {
                globalState.ConcurrentAdd(bigDirectoryAdd);
                bigDirectoryResult.size = 0;
            }

            return bigDirectoryResult;
        }

        private void FindBigDirectories(string startDirectory, long filterSize, int topCount, CancellationToken cancellationToken)
        {
            BigDirectoryGlobalState bigDirectoryGlobalState = new BigDirectoryGlobalState(filterSize, topCount, false, this);
            _ = ProcessAllFilesInDirectory<BigDirectory, BigDirectoryGlobalState>(startDirectory, processOneDirectoryBigDirectory, bigDirectoryGlobalState, cancellationToken);
        }

        private void StartSearch()
        {
            string startDirectory = textBox1.Text;
            if (string.IsNullOrEmpty(startDirectory))
            {
                startDirectory = @"c:\users\pierre";
            }

            int filterSize = 10 * 1024 * 1024;
            int topCount = 10;

            cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                FindBigDirectories(startDirectory, filterSize, topCount, cancellationTokenSource.Token);
                Invoke((MethodInvoker)delegate
                {
                    button1.Text = "start";
                    button1.BackColor = Color.Green;
                    running = false;
                });
            }, cancellationTokenSource.Token);
        }

        private void StopSearch()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource = null;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (running)
            {
                button1.Text = "start";
                button1.BackColor = Color.Green;
                StopSearch();
            }
            else
            {
                button1.Text = "stop";
                button1.BackColor = Color.Red;
                StartSearch();
            }
            running = !running;
        }

        private OpenFileDialog openFileDialog1;
        private TextBox textBox2;

        private void SetText(string text)
        {
            textBox2.Text = text;
        }

        private void SelectButton_Click(object sender, EventArgs e)
        {
            Debugger.Break();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            openFileDialog1 = new OpenFileDialog();
            button2.Click += new EventHandler(SelectButton_Click);
            textBox2 = new TextBox
            {
                Size = new Size(300, 300),
                Location = new Point(15, 40),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            // ClientSize = new Size(330, 360);
            Controls.Add(button2);
            // Controls.Add(textBox2);

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var sr = new StreamReader(openFileDialog1.FileName);
                    SetText(sr.ReadToEnd());
                }
                catch (SecurityException ex)
                {
                    MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                    $"Details:\n\n{ex.StackTrace}");
                }
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
