using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Catchem.Classes;
using Catchem.UiTranslation;
using PoGo.PokeMobBot.Logic;
using PoGo.PokeMobBot.Logic.Utils;

namespace Catchem.Controllers
{
    /// <summary>
    /// Interaction logic for Schedule.xaml
    /// </summary>
    public partial class Schedule
    {
        internal BotWindowData Bot;
        private readonly bool[,] _schedule = new bool[7, 24];
        private readonly Rectangle[,] _schedRectangles = new Rectangle[7, 24];
        private ScheduleActionEditor _sae;

        public Schedule()
        {
            InitializeComponent();
            CreateShceduleControl();
        }

        private void CreateShceduleControl()
        {
            var green = FindResource("StartBrush") as LinearGradientBrush;
            for (var i = 0; i < 7; i++)
            {
                for (var j = 0; j < 24; j++)
                {
                    _schedule[i,j] = true;
                    var hourRectangle = new Rectangle
                    {
                        Fill = green,
                        Tag = Tuple.Create(i, j),
                        Stroke = new SolidColorBrush(Colors.CornflowerBlue)
                    };
                    hourRectangle.MouseLeftButtonDown += HourRectangle_MouseLeftButtonDown;
                    hourRectangle.MouseEnter += HourRectangle_MouseEnter;
                    hourRectangle.MouseRightButtonDown += HourRectangle_MouseRightButtonDown;
                    _schedRectangles[i, j] = hourRectangle;
                    hourRectangle.Margin = new Thickness(1);
                    Grid.SetRow(hourRectangle, i);
                    Grid.SetColumn(hourRectangle, j);
                    ScheduleGrid.Children.Add(hourRectangle);

                }
            }
        }

        public void CloseSae()
        {
            if (_sae == null) return;
            ScheduleControlGrid.Children.Remove(_sae);
            _sae = null;
            RefreshSchedule(_schedule, Bot.GlobalSettings.Schedule.ActionList);
        }

        private void HourRectangle_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Bot.GlobalSettings.Schedule.ActionList == null)
            {
                Bot.GlobalSettings.Schedule.ActionList = new List<ScheduleAction>();
            }

            var rec = sender as Rectangle;
            var indx = rec?.Tag as Tuple<int, int>;
            if (indx == null) return;


            var sae = new ScheduleActionEditor
            {
                Day = indx.Item1,
                Hour = indx.Item2
            };
            TranslationEngine.ApplyLanguage(sae);

            sae.SetSchedule(this);
            ScheduleControlGrid.Children.Add(sae);
            _sae = sae;
        }



        private void HourRectangle_MouseEnter(object sender, MouseEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) || e.LeftButton == MouseButtonState.Pressed)
            {
                var rec = sender as Rectangle;
                var indx = rec?.Tag as Tuple<int, int>;
                if (indx == null) return;

                ChangeHourState(indx.Item1, indx.Item2);
            }
        }

        private void HourRectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var rec = sender as Rectangle;
            var indx = rec?.Tag as Tuple<int, int>;
            if (indx == null) return;

            ChangeHourState(indx.Item1, indx.Item2);
        }

        private void ChangeHourState(int i, int j, bool? forceVal = null)
        {
            if (forceVal == null)
                _schedule[i, j] = !_schedule[i, j];
            else
                _schedule[i, j] = (bool)forceVal;
            
            ChangeRectangeColor(i, j);
        }

        private void ChangeRectangeColor(int i, int j)
        {
            var green = FindResource("StartBrush") as LinearGradientBrush;
            var red = FindResource("StopBrush") as LinearGradientBrush;
            var rec = _schedRectangles[i, j];
            rec.Fill = _schedule[i, j] ? green : red;
        }

        public void SetNewBotAndShow(BotWindowData bot)
        {
            Bot = bot;
            Visibility = Visibility.Visible;
            RefreshSchedule(bot.GlobalSettings.Schedule.Schedule, bot.GlobalSettings.Schedule.ActionList);
        }

        private void RefreshSchedule(bool[,] sched, List<ScheduleAction> actionList)
        {
            if (actionList == null)
            {
                actionList = new List<ScheduleAction>();
            }
            if (Bot.GlobalSettings.Schedule == null)
            {
                Bot.GlobalSettings.Schedule = new ScheduleSettings();
            }

            for (var i = 0; i < sched.GetLength(0); i++)
            {
                for (var j = 0; j < sched.GetLength(1); j++)
                {
                    _schedule[i, j] = sched[i, j];
                    SetRectangleStroke(i, j, actionList.Any(x => x.Day == i && x.Hour == j));

                    ChangeRectangeColor(i, j);
                }
            }
        }

        private void SetRectangleStroke(int i, int j, bool haveActions)
        {
            var rec = _schedRectangles[i, j];
            rec.StrokeThickness = haveActions ? 3 : 0;
        }

        private void btn_SaveSchedule_Click(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Collapsed;
            for (var i = 0; i < Bot.GlobalSettings.Schedule.Schedule.GetLength(0); i++)
            {
                for (var j = 0; j < Bot.GlobalSettings.Schedule.Schedule.GetLength(1); j++)
                {
                    Bot.GlobalSettings.Schedule.Schedule[i, j] = _schedule[i, j];
                }
            }
        }

        private void btn_CancelShedule_Click(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Collapsed;
        }

        private void btn_ClearSchedule_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 24; j++)
                {
                    ChangeHourState(i, j, true);
                }
            }
        }
    }
}
