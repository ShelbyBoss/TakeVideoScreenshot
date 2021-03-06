﻿using StdOttFramework.RestoreWindow;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Unosquare.FFME;
using Unosquare.FFME.Common;

namespace TakeVideoScreenshot
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isUpadatingSlider;
        private DateTime videoCreated;
        private readonly DispatcherTimer timer;
        private BitmapDataBuffer bmpBuffer;

        public MainWindow()
        {
            InitializeComponent();

            Library.FFmpegDirectory = Environment.CurrentDirectory;
            RestoreWindowHandler.Activate(this, RestoreWindowSettings.GetDefault());

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (isUpadatingSlider) return;

            isUpadatingSlider = true;
            sld.Value = me.Position.TotalMilliseconds;
            tblPosition.Text = FomratTimeSpan(me.Position);
            isUpadatingSlider = false;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                FileInfo file = new FileInfo(files[0]);

                videoCreated = file.LastWriteTime;

                me.Open(new Uri(file.FullName));
                me.Play();
            }
        }

        private void TbnPlayPause_Checked(object sender, RoutedEventArgs e)
        {
            me.Play();
        }

        private void TbnPlayPause_Unchecked(object sender, RoutedEventArgs e)
        {
            me.Pause();
        }

        private void BtnSlower_Click(object sender, RoutedEventArgs e)
        {
            SlowerDown();
        }

        private void BtnFaster_Click(object sender, RoutedEventArgs e)
        {
            SpeedUp();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            PreviousFrame();
        }

        private void BtnFor_Click(object sender, RoutedEventArgs e)
        {
            NextFrame();
        }

        private void BtnTake_Click(object sender, RoutedEventArgs e)
        {
            TakeScreenshot();
        }

        private void Sld_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUpadatingSlider) return;

            isUpadatingSlider = true;
            me.Position = TimeSpan.FromMilliseconds(sld.Value);
            isUpadatingSlider = false;
        }

        private void Me_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Space:
                    PlayPause();
                    break;

                case Key.OemMinus:
                    SlowerDown();
                    break;

                case Key.OemPlus:
                    SpeedUp();
                    break;

                case Key.Q:
                    PreviousFrame();
                    break;

                case Key.E:
                    NextFrame();
                    break;

                case Key.W:
                case Key.S:
                    TakeScreenshot();
                    break;

                case Key.A:
                    me.Position -= TimeSpan.FromSeconds(5);
                    break;

                case Key.D:
                    me.Position += TimeSpan.FromSeconds(5);
                    break;
            }
        }

        private void PlayPause()
        {
            switch (me.MediaState)
            {
                case MediaPlaybackState.Play:
                    me.Pause();
                    break;

                default:
                    me.Play();
                    break;
            }
        }

        private void SlowerDown()
        {
            me.SpeedRatio /= 1.1;
        }

        private void SpeedUp()
        {
            me.SpeedRatio *= 1.1;
        }

        private void PreviousFrame()
        {
            me.Position -= me.PositionStep;
        }

        private void NextFrame()
        {
            me.Position += me.PositionStep;
        }

        private void TakeScreenshot()
        {
            string path;
            string filePrefix = tbxFilePrefix.Text.Trim();

            if (filePrefix.Length == 0)
            {
                MessageBox.Show("Check targetpath!");
                return;
            }

            DateTime captureTime = videoCreated + me.Position;

            do
            {
                string fileName = string.Format(" {0}.png", Convert(captureTime));
                path = filePrefix + fileName;

                captureTime = captureTime.AddMilliseconds(1);

            } while (File.Exists(path));

            rect.Fill = Brushes.Black;
            Task.Factory.StartNew(() =>
            {
                bmpBuffer.CreateDrawingBitmap().Save(path);
                Dispatcher.Invoke(() => rect.Fill = Brushes.Transparent);
            });
        }

        private static string Convert(DateTime t)
        {
            string date = string.Format("{0,2}-{1,2}-{2,2}", t.Year, t.Month, t.Day).Replace(" ", "0");
            string time = string.Format("{0,2}-{1,2}-{2,2}-{3,3}", t.Hour, t.Minute, t.Second, t.Millisecond).Replace(" ", "0");

            return date + " " + time;
        }

        private void Me_MediaOpened(object sender, MediaOpenedEventArgs e)
        {
            TimeSpan? duration = me.NaturalDuration;
            sld.Maximum = duration.HasValue ? duration.Value.TotalMilliseconds : 0;
            tblDuration.Text = FomratTimeSpan(duration.GetValueOrDefault());
            tblPosition.Text = FomratTimeSpan(me.Position);

            gidSlider.Visibility = Visibility.Visible;
        }

        private static string FomratTimeSpan(TimeSpan ts)
        {
            return TimeSpan.FromSeconds(Math.Round(ts.TotalSeconds)).ToString("c");
        }

        private void Me_RenderingVideo(object sender, RenderingVideoEventArgs e)
        {
            bmpBuffer = e.Bitmap;
        }

        private void Me_MediaStateChanged(object sender, MediaStateChangedEventArgs e)
        {
            tbnPlayPause.IsChecked = e.MediaState == MediaPlaybackState.Play;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            timer.Stop();

            base.OnClosing(e);
        }

        private void Rect_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            ((FrameworkElement)sender).Focus();
        }
    }
}
