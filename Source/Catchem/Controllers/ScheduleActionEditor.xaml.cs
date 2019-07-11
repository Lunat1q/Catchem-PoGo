using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PoGo.PokeMobBot.Logic.Utils;

namespace Catchem.Controllers
{
    /// <summary>
    /// Interaction logic for ScheduleActionEditor.xaml
    /// </summary>
    public partial class ScheduleActionEditor
    {
        private Schedule _schedule = new Schedule();
        private ScheduleAction _actionInEditor;

        private ObservableCollection<ScheduleAction> CurrentActions
        {
            get
            {
                if (_schedule.Bot == null) return new ObservableCollection<ScheduleAction>();
                return new ObservableCollection<ScheduleAction>(_schedule.Bot.GlobalSettings.Schedule.ActionList.Where(x => x.Day == Day && x.Hour == Hour));
            }
        }
        public int Day;
        public int Hour;
        public ScheduleActionEditor()
        {
            InitializeComponent();

            ComboBox.ItemsSource = Enum.GetValues(typeof(ScheduleActionType));
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            if (cb == null || cb.SelectedIndex < 0) return;
            var type = (ScheduleActionType) cb.SelectedItem;
            _actionInEditor = new ScheduleAction
            {
                ActionType = type,
                Day = Day,
                Hour = Hour
            };
            ClearControlHolderGrid();
            switch (type)
            {
                case ScheduleActionType.ChangeRoute:
                    _actionInEditor.ActionArgs = new string[1];
                    CreateRouteControl();
                    break;
                case ScheduleActionType.ChangeLocation:
                    _actionInEditor.ActionArgs = new string[2];
                    CreateLocationControl();
                    break;
                case ScheduleActionType.ChangeSettings:
                    _actionInEditor.ActionArgs = new string[2];
                    CreateSettingsControl();
                    break;
            }
        }

        private void CreateLocationControl()
        {
            Create2TextBox("Lat", "Lon");
        }

        private void CreateSettingsControl()
        {
            Create2TextBox("Param name", "Value");
        }

        private void Create2TextBox(string label1, string label2)
        {
            var tb = new TextBlock
            {
                Text = label1,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            var text = new TextBox
            {
                Margin = new Thickness(label1.Length * 8 + 20, 0, 15, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            text.TextChanged += delegate
            {
                if (text.Text != null)
                    _actionInEditor.ActionArgs[0] = text.Text;
            };
            ParamsGrid.Children.Add(tb);
            ParamsGrid.Children.Add(text);

            var tb2 = new TextBlock
            {
                Text = label2,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            var text2 = new TextBox
            {
                Margin = new Thickness(label2.Length * 8 + 20, 0, 15, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            text2.TextChanged += delegate
            {
                if (text.Text != null)
                    _actionInEditor.ActionArgs[1] = text2.Text;
            };
            Grid.SetColumn(tb2, 1);
            Grid.SetColumn(text2, 1);
            ParamsGrid.Children.Add(tb2);
            ParamsGrid.Children.Add(text2);
        }

        private void ClearControlHolderGrid()
        {
            ParamsGrid.Children.Clear();
        }

        private void CreateRouteControl()
        {
            var tb = new TextBlock
            {
                Text = "Select route",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            var cb = new ComboBox
            {
                Margin = new Thickness(80, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                ItemsSource = MainWindow.BotWindow.GlobalCatchemSettings.Routes
            };
            cb.SelectionChanged += delegate
            {
                _actionInEditor.ActionArgs[0] = cb.SelectedItem.ToString();
            };
            ParamsGrid.Children.Add(tb);
            ParamsGrid.Children.Add(cb);
        }

        public void SetSchedule(Schedule schedule)
        {
            _schedule = schedule;
            ListBox.ItemsSource = CurrentActions;
        }

        private void CloseActionSetter_Click(object sender, RoutedEventArgs e)
        {
            _schedule.CloseSae();
        }

        private void AddNewActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBox.SelectedIndex == -1) return;
            _schedule.Bot.GlobalSettings.Schedule.ActionList.Add(_actionInEditor);
            ComboBox.SelectedIndex = -1;
            ClearControlHolderGrid();
            _actionInEditor = null;
            ListBox.ItemsSource = CurrentActions;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var action = btn?.DataContext as ScheduleAction;
            if (action == null) return;
            _schedule.Bot.GlobalSettings.Schedule.ActionList.Remove(action);
            ListBox.ItemsSource = CurrentActions;
        }
    }
}

