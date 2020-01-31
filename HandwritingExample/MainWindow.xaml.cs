using System;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media;
using Selvasai;

namespace HandwritingExample
{
    public partial class MainWindow : Window
    {
        const int MAX_CANDIDATES = 10;
        IntPtr inkObj = IntPtr.Zero;
        IntPtr settingObj = IntPtr.Zero;
        IntPtr resultObj = IntPtr.Zero;
        Point currentPoint = new Point();
        bool mouseDowned = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeEngine();
            UpdateVersion();
        }

        ~MainWindow()
        {
            DestroyEngine();
        }

        private int InitializeEngine()
        {
            int status = Hwr.Create("./license_key/license.key");
            if (inkObj == IntPtr.Zero)
            {
                inkObj = Hwr.CreateInkObject();
            }
            if (settingObj == IntPtr.Zero)
            {
                settingObj = Hwr.CreateSettingObject();
            }
            if (resultObj == IntPtr.Zero)
            {
                resultObj = Hwr.CreateResultObject();
            }
            Hwr.SetExternalResourcePath("./hdb");
            Hwr.SetExternalLibraryPath("./lib");
            Hwr.SetRecognitionMode(settingObj, Hwr.MULTICHAR);
            Hwr.SetCandidateSize(settingObj, MAX_CANDIDATES);
            SetLanguage(Hwr.DLANG_KOREAN);

            return status;
        }

        private int DestroyEngine()
        {
            int status = Hwr.Close();
            if (inkObj != IntPtr.Zero)
            {
                Hwr.DestroyInkObject(inkObj);
            }
            if (settingObj != IntPtr.Zero)
            {
                Hwr.DestroySettingObject(settingObj);
            }
            if (resultObj != IntPtr.Zero)
            {
                Hwr.DestroyResultObject(resultObj);
            }

            return status;
        }

        private String GetCandidates(IntPtr result)
        {
            StringBuilder candidates = new StringBuilder();
            bool exit = false;
            int lineSize = Hwr.GetLineSize(result);
            if (lineSize == 0)
            {
                candidates.Append("result empty");
                return candidates.ToString();
            }

            for (int i = 0; i < MAX_CANDIDATES; i++)
            {
                for (int j = 0; j < lineSize; j++)
                {
                    IntPtr line = Hwr.GetLine(result, j);
                    int blockSize = Hwr.GetBlockSize(line);
                    for (int k = 0; k < blockSize; k++)
                    {
                        IntPtr block = Hwr.GetBlock(line, k);
                        if (Hwr.GetCandidateSize(block) <= i)
                        {
                            exit = true;
                            break;
                        }
                        int length = 0;
                        candidates.Append(String.Format("[{0}] ", i + 1));
                        candidates.Append(Hwr.GetCandidate(block, i, ref length));
                        if (k + 1 < blockSize)
                        {
                            candidates.Append(" ");
                        }
                    }
                    if (exit)
                    {
                        break;
                    }
                    if (j + 1 < lineSize)
                    {
                        candidates.Append("\n");
                    }
                }
                if (exit)
                {
                    break;
                }
                candidates.Append("\n");
            }
            return candidates.ToString();
        }

        private void SetLanguage(int language)
        {
            Hwr.ClearLanguage(settingObj);

            if (language == Hwr.DLANG_KOREAN)
            {
                Hwr.AddLanguage(settingObj, Hwr.DLANG_KOREAN, Hwr.DTYPE_KOREAN | Hwr.DTYPE_UPPERCASE | Hwr.DTYPE_LOWERCASE);
            }
            else if (language == Hwr.DLANG_ENGLISH)
            {
                Hwr.AddLanguage(settingObj, Hwr.DLANG_ENGLISH, Hwr.DTYPE_UPPERCASE | Hwr.DTYPE_LOWERCASE);
            }
            else if (language == Hwr.DLANG_CHINA)
            {
                Hwr.AddLanguage(settingObj, Hwr.DLANG_CHINA, Hwr.DTYPE_SIMP);
            }
            else if (language == Hwr.DLANG_JAPANESE)
            {
                Hwr.AddLanguage(settingObj, Hwr.DLANG_JAPANESE, Hwr.DTYPE_HIRAGANA);
            }

            Hwr.SetAttribute(settingObj);
        }

        private void Clear()
        {
            int childrenCount = VisualTreeHelper.GetChildrenCount(writingCanvas);
            // remove all except guideText
            writingCanvas.Children.RemoveRange(1, childrenCount - 1);
            Hwr.InkClear(inkObj);
        }

        private void UpdateVersion()
        {
            version.Text += Hwr.GetRevision();
        }

        private void ShowCandidateText(bool visible)
        {
            candidates.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetCandidateText(String text)
        {
            candidates.Text = text;
        }

        private void KoreanEnglish_Click(object sender, RoutedEventArgs e)
        {
            Clear();
            SetLanguage(Hwr.DLANG_KOREAN);
        }

        private void Chinese_Click(object sender, RoutedEventArgs e)
        {
            Clear();
            SetLanguage(Hwr.DLANG_CHINA);
        }

        private void Japanese_Click(object sender, RoutedEventArgs e)
        {
            Clear();
            SetLanguage(Hwr.DLANG_JAPANESE);
        }

        private void RecognizeButton_Click(object sender, RoutedEventArgs e)
        {
            int status = Hwr.Recognize(inkObj, resultObj);
            if (status == Hwr.ERR_SUCCESS)
            {
                SetCandidateText(GetCandidates(resultObj));
            }
            else
            {
                SetCandidateText("No Result");
            }
            ShowCandidateText(true);
            Clear();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Clear();
            ShowCandidateText(false);
        }

        private void WritingCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                currentPoint = e.GetPosition(writingCanvas);
                Hwr.AddPoint(inkObj, (int)currentPoint.X, (int)currentPoint.Y);
                mouseDowned = true;
            }
        }

        private void WritingCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDowned && e.LeftButton == MouseButtonState.Pressed)
            {
                Line line = new Line();
                line.Stroke = Brushes.Black;
                line.StrokeThickness = 2;
                line.X1 = currentPoint.X;
                line.Y1 = currentPoint.Y;
                line.X2 = e.GetPosition(writingCanvas).X;
                line.Y2 = e.GetPosition(writingCanvas).Y;

                currentPoint = e.GetPosition(writingCanvas);
                writingCanvas.Children.Add(line);

                // skips duplicated point
                if (line.X1 != line.X2 && line.Y1 != line.Y2)
                {
                    Hwr.AddPoint(inkObj, (int)line.X2, (int)line.Y2);
                }
            }
        }

        private void WritingCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Released)
            {
                Hwr.EndStroke(inkObj);
                mouseDowned = false;
            }
        }

        private void writingCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Canvas.SetLeft(guideText, (e.NewSize.Width / 2) - (guideText.ActualWidth / 2));
            Canvas.SetTop(guideText, (e.NewSize.Height / 2) - (guideText.ActualHeight / 2));
        }
    }
}
