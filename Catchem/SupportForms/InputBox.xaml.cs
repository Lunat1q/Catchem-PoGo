using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace Catchem.SupportForms
{
    public partial class InputDialog : Window
    {
        public InputDialog(string question, string defaultAnswer = "", bool onlyNum = false, int maxLength = 0, List<object> selectList = null)
        {
            InitializeComponent();
            lblQuestion.Content = question;
            if (selectList == null)
            {
                txtAnswer.Text = defaultAnswer;
                if (maxLength != 0) txtAnswer.MaxLength = maxLength;
                if (onlyNum)
                {
                    txtAnswer.PreviewTextInput += TxtAnswerOnPreviewTextInput;
                }
            }
            else
            {
                txtAnswer.Visibility = Visibility.Collapsed;
                comboBox.Visibility = Visibility.Visible;
                comboBox.ItemsSource = selectList;
            }
        }

        private static void TxtAnswerOnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            txtAnswer.SelectAll();
            txtAnswer.Focus();
        }

        private static bool IsTextAllowed(string text)
        {
            Regex regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
            return !regex.IsMatch(text);
        }

        public string Answer => txtAnswer.Text;
        public object ObjectAnswer => comboBox.SelectedItem;
    }
}