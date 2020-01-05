using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using IO;
using Timer = System.Threading.Timer;
using FanoCompression;
using LZ77;

namespace WpfGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private long mLz77CompressProgress;

        private readonly Brush mErrorBrush = new SolidColorBrush(Colors.Red);
        private Brush mNormalBrush;
        private Brush mButtonBrush;

        public MainWindow()
        {
            InitializeComponent();
            mNormalBrush = FileTextBoxFanoI.BorderBrush;
            mButtonBrush = CompressLz77.BorderBrush;
        }

        #region LZ77 compress

        private BackgroundWorker mLz77IWorker;

        private void CompressLz77_Click(object sender, RoutedEventArgs e)
        {
            bool isError = false;
            CompressLz77.IsEnabled = false;
            try
            {
                string inputFileName = FileTextBoxLz77I.Text;
                if (!File.Exists(inputFileName))
                {
                    FileTextBoxLz77I.BorderBrush = mErrorBrush;
                    isError = true;
                }

                if (HistorySize.Value == null)
                {
                    HistorySize.BorderBrush = mErrorBrush;
                    isError = true;
                }

                if (PresentSize.Value == null)
                {
                    PresentSize.BorderBrush = mErrorBrush;
                    isError = true;
                }

                if (isError)
                    return;

                mPresent = unchecked((uint) PresentSize.Value.Value);
                mHistory = unchecked((uint) HistorySize.Value.Value);
                if (mPresent > mHistory)
                {
                    HistorySize.BorderBrush = mErrorBrush;
                    isError = true;
                    return;
                }


                SaveFileDialog savePicker = new SaveFileDialog
                {
                    OverwritePrompt = true,
                    CheckPathExists = true,
                    Filter = "LZ77 compressed files|*.lz77|Any|*.*"
                };

                if (savePicker.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    CompressLz77.BorderBrush = mErrorBrush;
                    isError = true;
                    return;
                }

                try
                {
                    mInputStreamLz77I = new FileStream(inputFileName, FileMode.Open, FileAccess.Read, bufferSize: 1024, share: FileShare.None, useAsync: true);
                }
                catch (Exception)
                {
                    FileTextBoxLz77I.BorderBrush = mErrorBrush;
                    isError = true;
                    return;
                }

                try
                {
                     mOutputStreamLz77I = new FileStream(savePicker.FileName, FileMode.Create, FileAccess.Write,
                        bufferSize: 1024, share: FileShare.None, useAsync: true);
                }
                catch (Exception)
                {
                    CompressLz77.BorderBrush = mErrorBrush;
                    isError = true;
                    return;
                }

                HistorySize.BorderBrush = mNormalBrush;
                FileTextBoxLz77I.BorderBrush = mNormalBrush;
                FileTextBoxLz77I.BorderBrush = mNormalBrush;
                CompressLz77.BorderBrush = mButtonBrush;

                mLz77IWorker = new BackgroundWorker();
                mLz77IWorker.DoWork += CompressLz77Worker;
                mLz77IWorker.RunWorkerCompleted += CompressLz77Completed;
                mLz77IWorker.ProgressChanged += CompressLz77Progress;
                mLz77IWorker.WorkerReportsProgress = true;
                mLz77IWorker.RunWorkerAsync();
            }
            finally
            {
                if(isError)
                    CompressLz77.IsEnabled = true;
            }
        }

        private Stream mInputStreamLz77I;
        private Stream mOutputStreamLz77I;
        private uint mHistory;
        private uint mPresent;

        private void CompressLz77Progress(object sender, ProgressChangedEventArgs e)
        {
            ProgressLz77I.Value = e.ProgressPercentage;
        }

        private void CompressLz77Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            CompressLz77.IsEnabled = true;
            ProgressLz77I.Value = 0;
        }

        private async void CompressLz77Worker(object sender, DoWorkEventArgs e)
        {
            long progress = 0;
            var reader = new BufferedReader(2000000, mInputStreamLz77I);
            var writer = new BufferedWriter(2000000, mOutputStreamLz77I);
            var len = mInputStreamLz77I.Length;

            var compressor = await LZ77.Compressor.Create(reader.ReadByte, writer.WriteCustomLength, mHistory, mPresent);
            compressor.WordsWritten += x =>
            {
                progress += x;
                //mLz77IWorker.ReportProgress(Convert.ToInt32(progress / (len / 100)));
            };// mLz77CompressProgress += x;
            Timer t = new Timer(o => mLz77IWorker.ReportProgress(Convert.ToInt32(progress / (len / 100))), null, 0, 25);

            await compressor.Compress((ulong)len);
            t.Dispose();
            await writer.FlushBuffer();
            mInputStreamLz77I.Close();
            mOutputStreamLz77I.Close();
        }

        #endregion LZ77 compress

        #region LZ77 extract

        private BackgroundWorker mLz77OWorker;
        private Stream mInputStreamLz77O;
        private Stream mOutputStreamLz77O;

        private void ExtractLz77_Click(object sender, RoutedEventArgs e)
        {
            bool isError = false;
            ExtractLz77.IsEnabled = false;
            try
            {
                string inputFileName = FileTextBoxLz77O.Text;
                if (!File.Exists(inputFileName))
                {
                    FileTextBoxLz77O.BorderBrush = mErrorBrush;
                    isError = true;
                }

                if (isError)
                    return;

                SaveFileDialog savePicker = new SaveFileDialog
                {
                    OverwritePrompt = true,
                    CheckPathExists = true,
                    Filter = "Any|*.*"
                };

                if (savePicker.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    ExtractLz77.BorderBrush = mErrorBrush;
                    isError = true;
                    return;
                }

                try
                {
                    mInputStreamLz77O = new FileStream(inputFileName, FileMode.Open, FileAccess.Read, bufferSize: 1024, share: FileShare.None, useAsync: true);
                }
                catch (Exception)
                {
                    FileTextBoxLz77O.BorderBrush = mErrorBrush;
                    isError = true;
                    return;
                }

                try
                {
                    mOutputStreamLz77O = new FileStream(savePicker.FileName, FileMode.Create, FileAccess.Write,
                       bufferSize: 1024, share: FileShare.None, useAsync: true);
                }
                catch (Exception)
                {
                    ExtractLz77.BorderBrush = mErrorBrush;
                    isError = true;
                    return;
                }

                HistorySize.BorderBrush = mNormalBrush;
                FileTextBoxLz77O.BorderBrush = mNormalBrush;
                FileTextBoxLz77O.BorderBrush = mNormalBrush;
                ExtractLz77.BorderBrush = mButtonBrush;

                mLz77OWorker = new BackgroundWorker();
                mLz77OWorker.DoWork += ExtractLz77Worker;
                mLz77OWorker.RunWorkerCompleted += ExtractLz77Completed;
                mLz77OWorker.ProgressChanged += ExtractLz77Progress;
                mLz77OWorker.WorkerReportsProgress = true;
                mLz77OWorker.RunWorkerAsync();
            }
            finally
            {
                if (isError)
                    ExtractLz77.IsEnabled = true;
            }
        }

        private void ExtractLz77Progress(object sender, ProgressChangedEventArgs e)
        {
            ProgressLz77O.Value = e.ProgressPercentage;
        }

        private void ExtractLz77Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            ExtractLz77.IsEnabled = true;
            ProgressLz77O.Value = 0;
        }

        private async void ExtractLz77Worker(object sender, DoWorkEventArgs e)
        {
            long progress = 0;
            var reader = new BufferedReader(2000000, mInputStreamLz77O);
            var writer = new BufferedWriter(2000000, mOutputStreamLz77O);
            var len = mInputStreamLz77O.Length / mWordLength * 8;

            var extractor = new Extractor(reader.ReadCustomLength, writer.WriteCustomLength);

            extractor.WordsWritten += x =>
            {
                progress += x;
            };
            Timer t = new Timer(o => mLz77OWorker.ReportProgress(Convert.ToInt32(progress / (len / 100))), null, 0, 25);

            await extractor.Extract();
            t.Dispose();
            await writer.FlushBuffer();
            mInputStreamLz77O.Close();
            mOutputStreamLz77O.Close();
        }

        #endregion LZ77 extract

        private void SetLengthLz77I(long length)
        {
            if (!ProgressLz77I.Dispatcher.CheckAccess())
            {
                ProgressLz77I.Dispatcher.Invoke(() => ProgressLz77I.Maximum = length, DispatcherPriority.Background);
            }
            else
            {
                ProgressLz77I.Maximum = length;
            }
        }

        private void SetProgressLz77I(long progress)
        {
            if (!ProgressLz77I.Dispatcher.CheckAccess())
            {
                ProgressLz77I.Dispatcher.Invoke(() => ProgressLz77I.Value = progress, DispatcherPriority.Background);
            }
            else
            {
                ProgressLz77I.Value = progress;
            }
        }

        #region Fano compress

        private BackgroundWorker mFanoIWorker;

        private void CompressFano_Click(object sender, RoutedEventArgs e)
        {
            bool isError = false;
            CompressFano.IsEnabled = false;
            try
            {
                string inputFileName = FileTextBoxFanoI.Text;
                if (!File.Exists(inputFileName))
                {
                    FileTextBoxFanoI.BorderBrush = mErrorBrush;
                    isError = true;
                }

                if (WordLength.Value == null)
                {
                    WordLength.BorderBrush = mErrorBrush;
                    isError = true;
                }

                if (isError)
                    return;

                mWordLength = Convert.ToByte(WordLength.Value.Value);

                SaveFileDialog savePicker = new SaveFileDialog
                {
                    OverwritePrompt = true,
                    CheckPathExists = true,
                    Filter = "Fano compressed files|*.Fano|Any|*.*"
                };

                if (savePicker.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    CompressFano.BorderBrush = mErrorBrush;
                    isError = true;
                    return;
                }

                try
                {
                    mInputStreamFanoI = new FileStream(inputFileName, FileMode.Open, FileAccess.Read, bufferSize: 1024, share: FileShare.None, useAsync: true);
                }
                catch (Exception)
                {
                    FileTextBoxFanoI.BorderBrush = mErrorBrush;
                    isError = true;
                    return;
                }

                try
                {
                    mOutputStreamFanoI = new FileStream(savePicker.FileName, FileMode.Create, FileAccess.Write,
                       bufferSize: 1024, share: FileShare.None, useAsync: true);
                }
                catch (Exception)
                {
                    CompressFano.BorderBrush = mErrorBrush;
                    isError = true;
                    return;
                }

                HistorySize.BorderBrush = mNormalBrush;
                FileTextBoxFanoI.BorderBrush = mNormalBrush;
                FileTextBoxFanoI.BorderBrush = mNormalBrush;
                CompressFano.BorderBrush = mButtonBrush;

                mFanoIWorker = new BackgroundWorker();
                mFanoIWorker.DoWork += CompressFanoWorker;
                mFanoIWorker.RunWorkerCompleted += CompressFanoCompleted;
                mFanoIWorker.ProgressChanged += CompressFanoProgress;
                mFanoIWorker.WorkerReportsProgress = true;
                mFanoIWorker.RunWorkerAsync();
            }
            finally
            {
                if (isError)
                    CompressFano.IsEnabled = true;
            }
        }

        private Stream mInputStreamFanoI;
        private Stream mOutputStreamFanoI;
        private byte mWordLength;

        private void CompressFanoProgress(object sender, ProgressChangedEventArgs e)
        {
            ProgressFanoI.Value = e.ProgressPercentage;
        }

        private void CompressFanoCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            CompressFano.IsEnabled = true;
            ProgressFanoI.Value = 0;
        }

        private async void CompressFanoWorker(object sender, DoWorkEventArgs e)
        {
            long progress = 0;
            var reader = new BufferedReader(2000000, mInputStreamFanoI);
            var writer = new BufferedWriter(2000000, mOutputStreamFanoI);
            var len = mInputStreamFanoI.Length / mWordLength * 8;

            var compressor = new FanoEncoder(reader, writer);

            compressor.WordsWritten += x =>
            {
                progress += x;
            };
            Timer t = new Timer(o => mFanoIWorker.ReportProgress(Convert.ToInt32(progress / (len / 100))), null, 0, 25);

            await compressor.Encode(mWordLength);
            t.Dispose();
            await writer.FlushBuffer();
            mInputStreamFanoI.Close();
            mOutputStreamFanoI.Close();
        }

        #endregion Fano compress

        #region Fano extract

        private BackgroundWorker mFanoOWorker;
        private Stream mInputStreamFanoO;
        private Stream mOutputStreamFanoO;

        private void ExtractFano_Click(object sender, RoutedEventArgs e)
        {
            bool isError = false;
            ExtractFano.IsEnabled = false;
            try
            {
                string inputFileName = FileTextBoxFanoO.Text;
                if (!File.Exists(inputFileName))
                {
                    FileTextBoxFanoO.BorderBrush = mErrorBrush;
                    isError = true;
                }

                if (isError)
                    return;

                SaveFileDialog savePicker = new SaveFileDialog
                {
                    OverwritePrompt = true,
                    CheckPathExists = true,
                    Filter = "Any|*.*"
                };

                if (savePicker.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    ExtractFano.BorderBrush = mErrorBrush;
                    isError = true;
                    return;
                }

                try
                {
                    mInputStreamFanoO = new FileStream(inputFileName, FileMode.Open, FileAccess.Read, bufferSize: 1024, share: FileShare.None, useAsync: true);
                }
                catch (Exception)
                {
                    FileTextBoxFanoO.BorderBrush = mErrorBrush;
                    isError = true;
                    return;
                }

                try
                {
                    mOutputStreamFanoO = new FileStream(savePicker.FileName, FileMode.Create, FileAccess.Write,
                       bufferSize: 1024, share: FileShare.None, useAsync: true);
                }
                catch (Exception)
                {
                    ExtractFano.BorderBrush = mErrorBrush;
                    isError = true;
                    return;
                }

                HistorySize.BorderBrush = mNormalBrush;
                FileTextBoxFanoO.BorderBrush = mNormalBrush;
                FileTextBoxFanoO.BorderBrush = mNormalBrush;
                ExtractFano.BorderBrush = mButtonBrush;

                mFanoOWorker = new BackgroundWorker();
                mFanoOWorker.DoWork += ExtractFanoWorker;
                mFanoOWorker.RunWorkerCompleted += ExtractFanoCompleted;
                mFanoOWorker.ProgressChanged += ExtractFanoProgress;
                mFanoOWorker.WorkerReportsProgress = true;
                mFanoOWorker.RunWorkerAsync();
            }
            finally
            {
                if (isError)
                    ExtractFano.IsEnabled = true;
            }
        }

        private void ExtractFanoProgress(object sender, ProgressChangedEventArgs e)
        {
            ProgressFanoO.Value = e.ProgressPercentage;
        }

        private void ExtractFanoCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ExtractFano.IsEnabled = true;
            ProgressFanoO.Value = 0;
        }

        private async void ExtractFanoWorker(object sender, DoWorkEventArgs e)
        {
            long progress = 0;
            var reader = new BufferedReader(2000000, mInputStreamFanoO);
            var writer = new BufferedWriter(2000000, mOutputStreamFanoO);
            var len = mInputStreamFanoO.Length / mWordLength * 8;

            var extractor = new FanoEncoder(reader, writer);

            extractor.WordsWritten += x =>
            {
                progress += x;
            };
            Timer t = new Timer(o => mFanoOWorker.ReportProgress(Convert.ToInt32(progress / (len / 100))), null, 0, 25);

            await extractor.Decode();
            t.Dispose();
            await writer.FlushBuffer();
            mInputStreamFanoO.Close();
            mOutputStreamFanoO.Close();
        }

        #endregion Fano extract

        #region Browse buttons

        private void FileButtonLz77O_Click(object sender, RoutedEventArgs e)
        {
            FileButtonLz77O.IsEnabled = false;
            try
            {
                OpenFileDialog openPicker = new OpenFileDialog
                {
                    Multiselect = false,
                    CheckFileExists = true,
                    ShowReadOnly = true,
                    Filter = "LZ77 compressed files|*.lz77|Any|*.*",
                    CheckPathExists = true
                };
                if (openPicker.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    FileTextBoxLz77O.Text = openPicker.FileName;
                }
            }
            finally
            {
                FileButtonLz77O.IsEnabled = true;
            }
        }

        private void FileButtonFanoO_Click(object sender, RoutedEventArgs e)
        {
            FileButtonFanoO.IsEnabled = false;
            try
            {
                OpenFileDialog openPicker = new OpenFileDialog
                {
                    Multiselect = false,
                    CheckFileExists = true,
                    ShowReadOnly = true,
                    Filter = "Fano compressed files|*.fano|Any|*.*",
                    CheckPathExists = true
                };
                if (openPicker.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    FileTextBoxFanoO.Text = openPicker.FileName;
                }
            }
            finally
            {
                FileButtonFanoO.IsEnabled = true;
            }
        }

        private void FileButtonLz77I_Click(object sender, RoutedEventArgs e)
        {
            FileButtonLz77I.IsEnabled = false;
            try
            {
                OpenFileDialog openPicker = new OpenFileDialog
                {
                    Multiselect = false,
                    CheckFileExists = true,
                    ShowReadOnly = true,
                    Filter = "Any|*.*",
                    CheckPathExists = true
                };
                if (openPicker.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    FileTextBoxLz77I.Text = openPicker.FileName;
                }
            }
            finally
            {
                FileButtonLz77I.IsEnabled = true;
            }
        }

        private void FileButtonFanoI_Click(object sender, RoutedEventArgs e)
        {
            FileButtonFanoI.IsEnabled = false;
            try
            {
                OpenFileDialog openPicker = new OpenFileDialog
                {
                    Multiselect = false,
                    CheckFileExists = true,
                    ShowReadOnly = true,
                    Filter = "Any|*.*",
                    CheckPathExists = true
                };
                if (openPicker.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    FileTextBoxFanoI.Text = openPicker.FileName;
                }
            }
            finally
            {
                FileButtonFanoI.IsEnabled = true;
            }
        }

        #endregion Browse buttons
    }
}
