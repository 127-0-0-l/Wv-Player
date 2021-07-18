using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using Un4seen.Bass;
using System.Threading;

namespace Wv_Player
{
    public partial class MainWindow : Window
    {
        //аудио-поток
        int stream;

        //список проигранных файлов
        LinkedList<string> memory = new LinkedList<string>();

        //цветовые темы
        Theme[] themes = new Theme[10];
        int activeTheme = 0;

        //текущий визуализатор
        int activeVisualizer = 0;

        //настройки приложения
        Settings settings = new Settings();

        //таймер для полосы времени
        DispatcherTimer timer = new DispatcherTimer();

        //визуализаторы
        Grid[] visualizer = new Grid[2];
        DispatcherTimer[] visualTimer = new DispatcherTimer[2];
        //визуализатор-дождь
        struct Rain
        {
            public Rectangle drop;
            public int speed;
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainGrid_Loaded(object sender, RoutedEventArgs e)
        {
            //загрузка тем
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fs = new FileStream("Themes.dat", FileMode.OpenOrCreate))
                themes = (Theme[])formatter.Deserialize(fs);

            #region создание визуализаторов
            float[] fft = new float[1024];

            Rectangle[] border = new Rectangle[2];
            for (int i = 0; i < border.Length; i++)
            {
                border[i] = new Rectangle();
                border[i].HorizontalAlignment = HorizontalAlignment.Left;
                border[i].VerticalAlignment = VerticalAlignment.Top;
                border[i].Margin = new Thickness(0, 0, 0, 0);
                border[i].Width = 198;
                border[i].Height = 120;
                border[i].Stroke = pbTime.BorderBrush;
                border[i].Tag = "border";
            }

            #region столбики
            visualizer[0] = new Grid();
            visualizer[0].HorizontalAlignment = HorizontalAlignment.Left;
            visualizer[0].VerticalAlignment = VerticalAlignment.Top;
            visualizer[0].Margin = new Thickness(101, 40, 0, 0);
            visualizer[0].Width = 198;
            visualizer[0].Height = 120;

            Button mode = new Button();
            mode.SetResourceReference(StyleProperty, "FlatButtonStyle");
            mode.Width = 30;
            mode.Height = 16;
            mode.BorderBrush = pbTime.BorderBrush;
            mode.Foreground = lblProgName.Foreground;
            mode.Background = MainGrid.Background;
            mode.Content = "mode";
            mode.HorizontalAlignment = HorizontalAlignment.Right;
            mode.VerticalAlignment = VerticalAlignment.Top;
            mode.HorizontalContentAlignment = HorizontalAlignment.Center;
            mode.Margin = new Thickness(0, 2, 2, 0);
            mode.FontFamily = new FontFamily("JetBrains Mono");
            mode.FontSize = 10;
            //изменение цвета кнопоки при наведении
            mode.MouseEnter += (s, a) =>
            {
                mode.BorderBrush = lblProgName.Foreground;
            };
            mode.MouseLeave += (s, a) =>
            {
                mode.BorderBrush = pbTime.BorderBrush;
            };

            Rectangle[] rectangle = new Rectangle[39];
            for (int i = 0; i < 39; i++)
            {
                rectangle[i] = new Rectangle();
                rectangle[i].Width = 4;
                rectangle[i].Height = 1;
                rectangle[i].HorizontalAlignment = HorizontalAlignment.Left;
                rectangle[i].VerticalAlignment = VerticalAlignment.Bottom;
                rectangle[i].Margin = new Thickness(2 + i * 5, 0, 0, 2);
                rectangle[i].Fill = pbTime.BorderBrush;
                visualizer[0].Children.Add(rectangle[i]);
            }

            visualizer[0].Children.Add(border[0]);
            visualizer[0].Children.Add(mode);
            MainGrid.Children.Add(visualizer[0]);

            visualTimer[0] = new DispatcherTimer();
            visualTimer[0].Interval = new TimeSpan(230000);

            double columnHeight = 0;
            int h = 1;

            //изменение режима
            mode.Click += (s, a) =>
            {
                if (h == 0) h = 1;
                else if (h == 1) h = 3;
                else h = 0;
            };

            //работа визуализатора
            visualTimer[0].Tick += (s, a) =>
            {
                Bass.BASS_ChannelGetData(stream, fft, (int)BASSData.BASS_DATA_FFT2048);

                if (h == 0)
                {
                    for (int i = 0; i < 39; i++)
                    {
                        columnHeight = Math.Sqrt(fft[i * 16 + 80]) * 500;
                        if (columnHeight > 0 && columnHeight < visualizer[0].Height - 4)
                            rectangle[i].Height = (int)columnHeight + 1;
                        else
                            rectangle[i].Height = 1;
                    }
                }
                else
                {
                    for (int i = 0; i < 39; i++)
                    {
                        columnHeight = Math.Sqrt(fft[i * 16 + 80]) * 500;
                        if (rectangle[i].Height < visualizer[0].Height - 4 - h && columnHeight > rectangle[i].Height)
                            rectangle[i].Height += h;
                        else if (rectangle[i].Height > 1)
                            rectangle[i].Height -= 1;
                    }
                }
            };
            #endregion

            #region дождь
            visualizer[1] = new Grid();
            visualizer[1].HorizontalAlignment = HorizontalAlignment.Left;
            visualizer[1].VerticalAlignment = VerticalAlignment.Top;
            visualizer[1].Margin = new Thickness(500, 40, 0, 0);
            visualizer[1].Width = 198;
            visualizer[1].Height = 120;

            List<Rain> rain = new List<Rain>();

            int width = 2;
            Random rand = new Random();
            while (width + 2 < visualizer[1].Width)
            {
                Rectangle r = new Rectangle();
                r.Width = 1;// rand.Next(1, 3);
                r.Height = rand.Next(5, 15);
                r.HorizontalAlignment = HorizontalAlignment.Left;
                r.VerticalAlignment = VerticalAlignment.Top;
                r.Margin = new Thickness(width, rand.Next(0, 110), 0, 0);
                r.Fill = pbTime.BorderBrush;
                Rain newDrop = new Rain();
                newDrop.drop = r;
                newDrop.speed = rand.Next(3, 7);

                rain.Add(newDrop);
                visualizer[1].Children.Add(r);
                width += (int)r.Width + 1;
            }

            visualizer[1].Children.Add(border[1]);
            MainGrid.Children.Add(visualizer[1]);

            visualTimer[1] = new DispatcherTimer();
            visualTimer[1].Interval = new TimeSpan(230000);

            //работа визуализатора
            new Thread(() =>
            {
                visualTimer[1].Tick += (s, a) =>
                {
                    foreach (var drop in rain)
                    {
                        if (drop.drop.Margin.Top < 125)
                            drop.drop.Margin = new Thickness(drop.drop.Margin.Left, drop.drop.Margin.Top + drop.speed, 0, 0);
                        else
                            drop.drop.Margin = new Thickness(drop.drop.Margin.Left, 0, 0, 0);
                    }
                };

            }).Start();
            #endregion
            #endregion

            #region загрузка настроек
            settings = settings.Load();

            //установка темы
            activeTheme = settings.theme;
            SetTheme(activeTheme);

            //инициализация аудиопотока
            Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);

            try
            {
                foreach (var folder in settings.folders)
                    CreateList(folder, true);
                var button = MainGrid.Children.OfType<Button>().
                                               Where(btn => (string)btn.Content == new DirectoryInfo(settings.activeFolder).Name).
                                               FirstOrDefault();
                var canvas = MainCanvas.Children.OfType<Canvas>().
                                                 Where(cnv => cnv.Name == settings.activeCanvas).
                                                 FirstOrDefault();
                string[] filePaths = Directory.GetFiles(settings.activeFolder, "*.mp3");
                Activate(button, canvas, filePaths);
                canvas.Tag = "active";
                RunFile(settings.activeFile);
            }
            catch { }

            if (settings.repeat == true)
            {
                rctRepeat.Fill = lblProgName.Foreground;
                rctRepeat.Tag = "true";
            }
            if (settings.mix == true)
            {
                rctMix.Fill = lblProgName.Foreground;
                rctMix.Tag = "true";
            }
            #endregion

            //изменение цвета кнопок при наведении
            foreach (var rct in MainGrid.Children.OfType<Rectangle>().Where(rc => rc.Tag == null))
            {
                rct.MouseEnter += (s, a) =>
                {
                    rct.Fill = lblProgName.Foreground;
                };

                rct.MouseLeave += (s, a) =>
                {
                    rct.Fill = pbTime.BorderBrush;
                };
            }

            //изменение цвета кнопок Repeat, Mix при наведении
            foreach (var rct in MainGrid.Children.OfType<Rectangle>().Where(rc => rc.Tag != null))
            {
                rct.MouseEnter += (s, a) =>
                {
                    rct.Fill = lblProgName.Foreground;
                };

                rct.MouseLeave += (s, a) =>
                {
                    if ((string)rct.Tag == "false")
                        rct.Fill = pbTime.BorderBrush;
                };
            }

            timer.Interval = new TimeSpan(0, 0, 1);
        }

        //закрытие приложения
        private void rctClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            settings.theme = activeTheme;
            Bass.BASS_StreamFree(stream);
            Bass.BASS_Free();
            settings.Save(settings);
            Close();
        }

        //сворачивание приложения
        private void rctMinimize_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        //перетаскивание окна
        private void MainGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try { DragMove(); }
            catch { }
        }

        //открытие папки
        private void rctOpen_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folder = new System.Windows.Forms.FolderBrowserDialog();
            folder.ShowDialog();
            settings.folders.Add(folder.SelectedPath);
            CreateList(folder.SelectedPath);
        }

        //очистка списка
        private void rctClear_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                //удалить кнопку
                var canvas = MainCanvas.Children.OfType<Canvas>().FirstOrDefault(cnv => cnv.IsEnabled == true);
                MainCanvas.Children.Remove(canvas);
                var button = MainGrid.Children.OfType<Button>().FirstOrDefault(btn => btn.IsEnabled == false);

                //сместить кнопки
                foreach (var btn in MainGrid.Children.OfType<Button>().
                                                      Where(bt => int.Parse((string)bt.Tag) > int.Parse((string)button.Tag)))
                {
                    btn.Margin = new Thickness(btn.Margin.Left - 62, btn.Margin.Top, 0, 0);
                    btn.Tag = (int.Parse((string)btn.Tag) - 1).ToString();
                }
                MainGrid.Children.Remove(button);
            }
            catch { }
        }

        //старт / пауза
        private void rctStartStop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Bass.BASS_ChannelIsActive(stream) == BASSActive.BASS_ACTIVE_PLAYING)
            {
                Bass.BASS_ChannelPause(stream);
                timer.Stop();
                if (activeVisualizer == 0) visualTimer[0].Stop();
                rctStartStop.OpacityMask = new ImageBrush(
                                           new BitmapImage(
                                           new Uri("Resourses/Images/start.png", UriKind.Relative)));
            }
            else if (Bass.BASS_ChannelIsActive(stream) == BASSActive.BASS_ACTIVE_PAUSED)
            {
                Bass.BASS_ChannelPlay(stream, false);
                timer.Start();
                if (activeVisualizer == 0) visualTimer[0].Start();
                rctStartStop.OpacityMask = new ImageBrush(
                                           new BitmapImage(
                                           new Uri("Resourses/Images/stop.png", UriKind.Relative)));
            }
        }

        //повтор одного файла
        private void rctRepeat_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((string)rctRepeat.Tag == "false")
            {
                rctRepeat.Tag = "true";
                settings.repeat = true;
            }
            else if ((string)rctRepeat.Tag == "true")
            {
                rctRepeat.Tag = "false";
                settings.repeat = false;
            }
        }

        //перемешать порядок воспроизведения
        private void rctMix_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((string)rctMix.Tag == "false")
            {
                rctMix.Tag = "true";
                settings.mix = true;
            }
            else if ((string)rctMix.Tag == "true")
            {
                rctMix.Tag = "false";
                settings.mix = false;

            }
        }

        //перемотка трека нажатием на полосу времени
        private void pbTime_MouseDown(object sender, MouseButtonEventArgs e)
        {
            double seconds = Bass.BASS_ChannelBytes2Seconds(stream, Bass.BASS_ChannelGetLength(stream));
            double secPerPoint = seconds / pbTime.Width;
            Bass.BASS_ChannelSetPosition(stream, secPerPoint * e.GetPosition(pbTime).X);
            int time = (int)Bass.BASS_ChannelBytes2Seconds(stream, Bass.BASS_ChannelGetPosition(stream));
            lblTimeNow.Content = $"{time / 60:d1}:{time % 60:d2}";
            pbTime.Value = time;
        }

        //предыдущая тема
        private void rctThemeBack_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (activeTheme != 0)
                SetTheme(--activeTheme);
            else
                SetTheme(activeTheme = themes.Length - 1);
        }

        //следующая тема
        private void rctThemeNext_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (activeTheme < themes.Length - 1)
                SetTheme(++activeTheme);
            else
                SetTheme(activeTheme = 0);
        }

        //предыдущий визуализатор
        private void rctVisualizerBack_MouseDown(object sender, MouseButtonEventArgs e)
        {
            visualTimer[activeVisualizer].Stop();
            visualizer[activeVisualizer].Margin = new Thickness(500, 40, 0, 0);
            if (activeVisualizer > 0)
                activeVisualizer--;
            else
                activeVisualizer = visualizer.Length - 1;
            visualizer[activeVisualizer].Margin = new Thickness(101, 40, 0, 0);
            visualTimer[activeVisualizer].Start();
        }

        //следующий визуализатор
        private void rctVisualizerNext_MouseDown(object sender, MouseButtonEventArgs e)
        {
            visualTimer[activeVisualizer].Stop();
            visualizer[activeVisualizer].Margin = new Thickness(500, 40, 0, 0);
            if (activeVisualizer + 1 < visualizer.Length)
                activeVisualizer++;
            else
                activeVisualizer = 0;
            visualizer[activeVisualizer].Margin = new Thickness(101, 40, 0, 0);
            visualTimer[activeVisualizer].Start();
        }

        //создание списка
        void CreateList(string folder, bool isLoading = false)
        {
            if (MainGrid.Children.OfType<Button>().Count() == 5)
            {
                MessageBox.Show("Максимум 5 папок\nМожете удалить одну для освобождения места");
                if (isLoading) settings.folders.Remove(folder);
                return;
            }

            string[] filesPaths = null;
            try
            {
                filesPaths = Directory.GetFiles(folder, "*.mp3");
            }
            catch
            {
                if (isLoading) settings.folders.Remove(folder);
                return;
            }

            Canvas cnvList = null;

            //заполнение списка
            if (filesPaths.Length != 0)
            {
                //кнопка-вкладка
                Button button = new Button();
                button.SetResourceReference(StyleProperty, "FlatButtonStyle");
                button.Width = 60;
                button.Height = 20;
                button.BorderBrush = pbTime.BorderBrush;
                button.Foreground = lblProgName.Foreground;
                button.Background = MainGrid.Background;
                button.Content = new DirectoryInfo(folder).Name;
                button.HorizontalAlignment = HorizontalAlignment.Left;
                button.VerticalAlignment = VerticalAlignment.Top;
                button.HorizontalContentAlignment = HorizontalAlignment.Center;
                button.Margin = new Thickness(20 + MainGrid.Children.OfType<Button>().Count() * 62, 268, 0, 0);
                button.FontFamily = new FontFamily("JetBrains Mono");
                button.FontSize = 12;
                button.Tag = $"{MainGrid.Children.OfType<Button>().Count()}";
                MainGrid.Children.Add(button);

                //изменение цвета кнопок при наведении
                button.MouseEnter += (s, a) =>
                {
                    button.BorderBrush = lblProgName.Foreground;
                };
                button.MouseLeave += (s, a) =>
                {
                    button.BorderBrush = pbTime.BorderBrush;
                };

                //область для кнопок с песнями
                cnvList = new Canvas();
                cnvList.HorizontalAlignment = HorizontalAlignment.Left;
                cnvList.VerticalAlignment = VerticalAlignment.Top;
                cnvList.Margin = new Thickness(0, 0, 0, 0);
                cnvList.Width = 340;
                cnvList.Height = filesPaths.Length * 40;
                //присваивание имени
                for (int i = 0; i < 5; i++)
                {
                    bool named = false;
                    foreach (var cnv in MainCanvas.Children.OfType<Canvas>())
                    {
                        if (cnv.Name == $"_{i}") named = true;
                    }
                    if (named == false)
                    {
                        cnvList.Name = $"_{i}";
                        break;
                    }
                }
                MainCanvas.Children.Add(cnvList);
                Activate(button, cnvList, filesPaths);

                //нажатие на кнопку-вкладку
                button.Click += (s, a) =>
                {
                    Activate(button, cnvList, filesPaths);
                };

                //кнопки с песнями
                int yLocation = 0;
                foreach (var file in filesPaths)
                {
                    Button song = new Button();
                    song.SetResourceReference(StyleProperty, "FlatButtonStyle");
                    song.Width = cnvList.Width;
                    song.Height = 40;
                    song.HorizontalAlignment = HorizontalAlignment.Left;
                    song.VerticalAlignment = VerticalAlignment.Top;
                    song.Margin = new Thickness(0, yLocation, 0, 0);
                    yLocation += 40;
                    song.Content = System.IO.Path.GetFileNameWithoutExtension(file);
                    song.FontFamily = new FontFamily("JetBrains Mono");
                    song.FontSize = 12;
                    song.Foreground = lblProgName.Foreground;
                    cnvList.Children.Add(song);

                    //изменение фона при наведении
                    song.MouseEnter += (s, a) =>
                    {
                        song.Background = new SolidColorBrush(Color.FromRgb(themes[activeTheme].listElement[0],
                                                                            themes[activeTheme].listElement[1],
                                                                            themes[activeTheme].listElement[2]));
                    };
                    song.MouseLeave += (s, a) =>
                    {
                        if ((string)song.Content != (string)lblSongName.Content)
                        {
                            song.Background = null;
                        }
                    };

                    //нажатие на кнопку из списка
                    song.Click += (s, a) =>
                    {
                        foreach (var canvas in MainCanvas.Children.OfType<Canvas>().
                                                                   Where(cnv => (string)cnv.Tag == "active"))
                            canvas.Tag = null;
                        cnvList.Tag = "active";
                        settings.activeCanvas = cnvList.Name;
                        settings.activeFolder = folder;
                        RunFile(file);
                    };
                }
            }
            else
            {
                MessageBox.Show("Нет mp3-файлов");
                if (isLoading) settings.folders.Remove(folder);
                return;
            }

            //предыдущий файл
            rctBack.MouseDown += (s, a) =>
            {
                if ((string)cnvList.Tag == "active" && filesPaths != null && memory.Count > 1)
                {
                    memory.RemoveLast();
                    RunFile(memory.Last(), true);
                }
            };

            //следующий файл
            void NextFile()
            {
                if ((string)cnvList.Tag == "active" && filesPaths != null)
                    if ((string)rctRepeat.Tag == "true")
                        RunFile(filesPaths.
                            Where(file => System.IO.Path.GetFileNameWithoutExtension(file) == (string)lblSongName.Content).
                            FirstOrDefault());
                    else if ((string)rctMix.Tag == "true")
                        RunFile(filesPaths[new Random().Next(filesPaths.Length)]);
                    else
                        for (int i = 0; i < filesPaths.Length; i++)
                            if (System.IO.Path.GetFileNameWithoutExtension(filesPaths[i]) == (string)lblSongName.Content)
                            {
                                if (i == filesPaths.Length - 1)
                                    RunFile(filesPaths[0]);
                                else
                                    RunFile(filesPaths[i + 1]);
                                break;
                            }
            }

            rctNext.MouseDown += (s, a) =>
            {
                NextFile();
            };

            timer.Tick += (s, a) =>
            {
                if ((string)cnvList.Tag == "active")
                {
                    if (pbTime.Value < pbTime.Maximum)
                    {
                        pbTime.Value++;
                        lblTimeNow.Content = $"{(int)pbTime.Value / 60:d1}:{(int)pbTime.Value % 60:d2}";
                    }
                    else
                    {
                        NextFile();
                    }
                }
            };

            //очистка списка
            rctClear.MouseDown += (s, a) =>
            {
                if (cnvList.IsEnabled == true)
                {
                    settings.folders.Remove(folder);
                    filesPaths = null;
                    folder = null;
                }
            };
        }

        //активировать кнопку
        void Activate(Button button, Canvas cnvList, string[] filesPaths)
        {
            foreach (var canvas in MainCanvas.Children.OfType<Canvas>().Where(cnv => cnv.IsEnabled == true))
            {
                canvas.IsEnabled = false;
                canvas.Visibility = Visibility.Hidden;
            }
            foreach (var btn in MainGrid.Children.OfType<Button>().Where(bt => bt.IsEnabled == false))
            {
                btn.IsEnabled = true;
            }
            cnvList.IsEnabled = true;
            button.IsEnabled = false;
            cnvList.Visibility = Visibility.Visible;
            if (filesPaths.Length < 6)
                MainCanvas.Height = 200;
            else
                MainCanvas.Height = cnvList.Height;
        }

        //запустить файл
        void RunFile(string file, bool isBack = false)
        {
            if (stream != 0)
                Bass.BASS_StreamFree(stream);

            stream = Bass.BASS_StreamCreateFile(file, 0, 0, BASSFlag.BASS_DEFAULT);

            if (stream != 0) Bass.BASS_ChannelPlay(stream, false);
            else return;

            settings.activeFile = file;

            if (isBack == false)
                memory.AddLast(file);

            lblSongName.Content = System.IO.Path.GetFileNameWithoutExtension(file);

            //фокусировка на кнопке
            try
            {
                var canvas = MainCanvas.Children.OfType<Canvas>().
                                                 Where(cnv => (string)cnv.Tag == "active").
                                                 FirstOrDefault();

                foreach (var but in canvas.Children.OfType<Button>().Where(btn => btn.Background != null))
                    if (but != null) but.Background = null;

                var button = canvas.Children.OfType<Button>().
                                             Where(btn => (string)btn.Content == (string)lblSongName.Content).
                                             FirstOrDefault();
                button.Focus();
                button.Background = new SolidColorBrush(Color.FromRgb(themes[activeTheme].listElement[0],
                                                                      themes[activeTheme].listElement[1],
                                                                      themes[activeTheme].listElement[2]));
            }
            catch { }

            int time = (int)Bass.BASS_ChannelBytes2Seconds(stream, Bass.BASS_ChannelGetLength(stream));
            lblTimeAll.Content = $"{time / 60:d1}:{time % 60:d2}";
            pbTime.Value = 0;
            lblTimeNow.Content = "0:00";
            pbTime.Maximum = time;
            timer.Stop();
            timer.Start();

            visualTimer[activeVisualizer].Stop();
            visualTimer[activeVisualizer].Start();
            rctStartStop.OpacityMask = new ImageBrush(new BitmapImage(new Uri("Resourses/Images/stop.png", UriKind.Relative)));
        }

        //установить тему
        void SetTheme(int i)
        {
            //4 основных цвета
            MainGrid.Background = new SolidColorBrush(Color.FromRgb(themes[i].background[0],
                                                                    themes[i].background[1],
                                                                    themes[i].background[2]));
            lblProgName.Foreground = new SolidColorBrush(Color.FromRgb(themes[i].font[0],
                                                                       themes[i].font[1],
                                                                       themes[i].font[2]));
            pbTime.BorderBrush = new SolidColorBrush(Color.FromRgb(themes[i].elements[0],
                                                                   themes[i].elements[1],
                                                                   themes[i].elements[2]));
            svList.Background = new ImageBrush(new BitmapImage(new Uri(themes[i].file, UriKind.Relative)));

            //кнопки
            foreach (var rct in MainGrid.Children.OfType<Rectangle>().Where(rc => rc.IsEnabled == true))
                rct.Fill = pbTime.BorderBrush;
            if ((string)rctRepeat.Tag == "true")
                rctRepeat.Fill = lblProgName.Foreground;
            if ((string)rctMix.Tag == "true")
                rctMix.Fill = lblProgName.Foreground;

            //списки
            foreach (var cnv in MainCanvas.Children.OfType<Canvas>())
            {
                foreach (var btn in cnv.Children.OfType<Button>())
                    btn.Foreground = lblProgName.Foreground;
                foreach (var btn in cnv.Children.OfType<Button>().Where(bt => bt.Background != null))
                    btn.Background = new SolidColorBrush(Color.FromRgb(themes[activeTheme].listElement[0],
                                                                       themes[activeTheme].listElement[1],
                                                                       themes[activeTheme].listElement[2]));
            }

            //вкладки
            foreach (var btn in MainGrid.Children.OfType<Button>())
            {
                btn.Background = MainGrid.Background;
                btn.Foreground = lblProgName.Foreground;
                btn.BorderBrush = pbTime.BorderBrush;
            }

            //визуализаторы
            foreach (var vis in visualizer)
            {
                foreach (var rct in vis.Children.OfType<Rectangle>().Where(r => r.Tag == null))
                {
                    rct.Fill = pbTime.BorderBrush;
                }
                foreach (var rct in vis.Children.OfType<Rectangle>().Where(r => (string)r.Tag == "border"))
                {
                    rct.Stroke = pbTime.BorderBrush;
                }
                foreach (var btn in vis.Children.OfType<Button>())
                {
                    btn.Background = MainGrid.Background;
                    btn.Foreground = lblProgName.Foreground;
                    btn.BorderBrush = pbTime.BorderBrush;
                }
            }
        }
    }
}
