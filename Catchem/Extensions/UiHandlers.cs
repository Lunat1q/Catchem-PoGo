using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Catchem.Extensions
{
    internal static class UiHandlers
    {
        public static void HandleUiElementChangedEvent(object uiElement, object obj)
        {
            var box = uiElement as TextBox;
            if (box != null)
            {
                var propName = box.Name.Substring(2);
                SetValueByName(propName, box.Text, obj);
                return;
            }
            var chB = uiElement as CheckBox;
            if (chB != null)
            {
                var propName = chB.Name.Substring(2);
                SetValueByName(propName, chB.IsChecked, obj);
            }
            var passBox = uiElement as PasswordBox;
            if (passBox != null)
            {
                var propName = passBox.Name.Substring(2);
                SetValueByName(propName, passBox.Password, obj);
            }
            var comboBox = uiElement as ComboBox;
            if (comboBox != null)
            {
                var propName = comboBox.Name.Substring(2);
                SetValueByName(propName, comboBox.SelectedItem, obj);
            }
        }

        public static List<T> GetLogicalChildCollection<T>(this UIElement parent) where T : DependencyObject
        {
            var logicalCollection = new List<T>();
            GetLogicalChildCollection(parent, logicalCollection);
            return logicalCollection;
        }

        public static string ToN1(this float val) => val.ToString("N1");
        public static string ToN1(this double val) => val.ToString("N1");

        private static void GetLogicalChildCollection<T>(DependencyObject parent, List<T> logicalCollection) where T : DependencyObject
        {
            IEnumerable children = LogicalTreeHelper.GetChildren(parent);
            foreach (object child in children)
            {
                if (child is DependencyObject)
                {
                    DependencyObject depChild = child as DependencyObject;
                    if (child is T)
                    {
                        logicalCollection.Add(child as T);
                    }
                    GetLogicalChildCollection(depChild, logicalCollection);
                }
            }
        }

        public static bool GetValueByName<T>(string propertyName, object obj, out T val)
        {
            return GetPropertyRecursive(propertyName, obj, out val) || GetFieldRecursive(propertyName, obj, out val);
        }

        private static bool GetPropertyRecursive<T>(string propertyName, object obj, out T value)
        {
            if (obj == null)
            {
                value = default(T);
                return false;
            }
            var objType = obj.GetType();
            if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(ObservableCollection<>))
            {
                value = default(T);
                return false;
            }
            foreach (var property in objType.GetProperties())
            {
                if (property.PropertyType == objType) continue;
                if (property.PropertyType.IsMyInterface() && property.PropertyType.IsClass)
                {
                    var nextObj = property.GetValue(obj);
                    if (GetPropertyRecursive(propertyName, nextObj, out value))
                        return true;
                    if (GetFieldRecursive(propertyName, nextObj, out value))
                        return true;
                }
                if (property.Name != propertyName) continue;
                value = (T)Convert.ChangeType(property.GetValue(obj), typeof(T));
                return true;
            }
            value = default(T);
            return false;
        }

        private static bool CheckObservable(Type objType)
        {
            return objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(ObservableCollection<>);
        }

        private static bool GetFieldRecursive<T>(string propertyName, object obj, out T value)
        {
            if (obj == null)
            {
                value = default(T);
                return false;
            }
            var objType = obj.GetType();
            if (CheckObservable(objType))
            {
                value = default(T);
                return false;
            }
            foreach (var property in objType.GetFields())
            {
                if (property.FieldType == objType) continue;
                if (property.FieldType.IsMyInterface() && property.FieldType.IsClass)
                {
                    var nextObj = property.GetValue(obj);
                    if (GetFieldRecursive(propertyName, nextObj, out value))
                        return true;
                    if (GetPropertyRecursive(propertyName, nextObj, out value))
                        return true;
                }
                if (property.Name != propertyName) continue;
                value = (T)Convert.ChangeType(property.GetValue(obj), typeof(T));
                return true;
            }
            value = default(T);
            return false;
        }

        public static void SetValueByName(string propertyName, object value, object obj)
        {
            if (!SetPropertyRecursive(propertyName, value, obj))
                SetFieldRecursive(propertyName, value, obj);
        }


        private static bool SetPropertyRecursive(string propertyName, object value, object obj)
        {
            if (obj == null) return false;
            var objType = obj.GetType();
            if (CheckObservable(objType))
            {
                return false;
            }
            foreach (var property in objType.GetProperties())
            {
                if (property.PropertyType == objType) continue;
                if (property.PropertyType.IsClass && property.PropertyType.IsMyInterface())
                {
                    var nextObj = property.GetValue(obj);
                    if (SetPropertyRecursive(propertyName, value, nextObj))
                        return true;
                    if (SetFieldRecursive(propertyName, value, nextObj))
                        return true;
                }
                if (property.Name != propertyName) continue;
                if (property.PropertyType == typeof(int))
                {
                    int val;
                    if (((string)value).GetVal(out val))
                        property.SetValue(obj, val);
                }
                else if (property.PropertyType == typeof(double))
                {
                    double val;
                    if (((string)value).GetVal(out val))
                        property.SetValue(obj, val);
                }
                else if (property.PropertyType == typeof(float))
                {
                    float val;
                    if (((string)value).GetVal(out val))
                        property.SetValue(obj, val);
                }
                else if (property.PropertyType == typeof(string))
                {
                    var val = value.ToString().Trim();
                    property.SetValue(obj, val);
                }
                else
                    property.SetValue(obj, value);
                return true;
            }
            return false;
        }

        private static bool SetFieldRecursive(string propertyName, object value, object obj)
        {
            if (obj == null) return false;
            var objType = obj.GetType();
            if (CheckObservable(objType))
            {
                return false;
            }
            foreach (var property in objType.GetFields())
            {
                if (property.FieldType == objType) continue;
                if (property.FieldType.IsClass && property.FieldType.IsMyInterface())
                {
                    var nextObj = property.GetValue(obj);
                    if (SetFieldRecursive(propertyName, value, nextObj))
                        return true;
                    if (SetPropertyRecursive(propertyName, value, nextObj))
                        return true;
                }
                if (property.Name != propertyName) continue;
                if (property.FieldType == typeof(int))
                {
                    int val;
                    if (((string)value).GetVal(out val))
                        property.SetValue(obj, val);
                }
                else if (property.FieldType == typeof(double))
                {
                    double val;
                    if (((string)value).GetVal(out val))
                        property.SetValue(obj, val);
                }
                else if (property.FieldType == typeof(float))
                {
                    float val;
                    if (((string)value).GetVal(out val))
                        property.SetValue(obj, val);
                }
                else if (property.FieldType == typeof(string))
                {
                    var val = value.ToString().Trim();
                    property.SetValue(obj, val);
                }
                else
                    property.SetValue(obj, value);
                return true;
            }
            return false;
        }

        public static bool GetVal<T>(this string value, out T resultVal) where T : IConvertible
        {
            resultVal = default(T);
            if (resultVal == null) return false;
            var typeCode = resultVal.GetTypeCode();
            switch (typeCode)
            {
                case TypeCode.Double:
                    {
                        double result;
                        var nfi = NumberFormatInfo.CurrentInfo;
                        var currentDecimalSeparator = nfi.CurrencyDecimalSeparator;
                        value = Conversion(value, currentDecimalSeparator);
                        var res = double.TryParse(value, out result);
                        if (!res) return false;
                        var changeType = Convert.ChangeType(result, typeCode);
                        if (changeType != null)
                            resultVal = (T)changeType;
                        return true;
                    }
                case TypeCode.Single:
                    {
                        float result;
                        //var res = float.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result)
                        //          || float.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out result)
                        //          || float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
                        var nfi = NumberFormatInfo.CurrentInfo;
                        var currentDecimalSeparator = nfi.CurrencyDecimalSeparator;
                        value = Conversion(value, currentDecimalSeparator);
                        var res = float.TryParse(value, out result);
                        if (!res) return false;
                        var changeType = Convert.ChangeType(result, typeCode);
                        if (changeType != null)
                            resultVal = (T)changeType;
                        return true;
                    }
                case TypeCode.Int32:
                    {
                        int result;
                        var res = int.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result)
                                  || int.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out result)
                                  || int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
                        if (!res) return false;
                        var changeType = Convert.ChangeType(result, typeCode);
                        if (changeType != null)
                            resultVal = (T)changeType;
                        return true;
                    }
            }
            return false;
        }

        private static string Conversion(string str1, string str2)
        {

            if (str1.Contains(".") && (str2 != "."))
                return str1.Replace('.', ',');
            if (str1.Contains(",") && (str2 != ","))
                return str1.Replace(',', '.');
            return str1;
        }

        public static void AppendText(this RichTextBox box, string text, System.Windows.Media.Color color)
        {
            var tr = new TextRange(box.Document.ContentEnd, box.Document.ContentEnd) { Text = text };
            try
            {
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
            }
            catch (FormatException) { }
        }

        public static void AppendParagraph(this RichTextBox box, string text, System.Windows.Media.Color color)
        {
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(text);
            paragraph.Foreground = new SolidColorBrush(color);
            box.Document.Blocks.Add(paragraph);

            box.ScrollToEnd();
        }

    }
}
