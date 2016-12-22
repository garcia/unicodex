using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Unicodex.Properties;

namespace Unicodex
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private static Key[] MODIFIER_KEYS = { Key.LeftAlt, Key.LeftCtrl, Key.LeftShift, Key.RightAlt, Key.RightCtrl, Key.RightShift, Key.LWin, Key.RWin, Key.System };

        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = Settings.Default.UnicodexSettings;
        }

        private void globalHotkeyNonModifier_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!MODIFIER_KEYS.Contains(e.Key))
            {
                e.Handled = true;

                string keyName = Enum.GetName(typeof(Key), e.Key);
                globalHotkeyNonModifier.Text = keyName;
            }
        }

        private void saveAndClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                // Window is closing - save settings
                Settings.Default.Save();
                ((App)Application.Current).UpdateHotkey();
                // TODO: update autostart
            }
            else
            {
                // Window is opening - put it near the cursor
                int left = System.Windows.Forms.Cursor.Position.X;
                int top = System.Windows.Forms.Cursor.Position.Y;
                WindowUtils.PutWindowNear(this, left, top, top);
            }
        }

        private void windowPlacement_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlacementSide item = (PlacementSide) e.AddedItems[0];
            insideOutsidePlacement.IsEnabled = (item != PlacementSide.CENTER);
        }
    }

    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

    // Thanks to Brian Lagunas: http://brianlagunas.com/a-better-way-to-data-bind-enums-in-wpf/
    public class EnumBindingSource : MarkupExtension
    {
        #region EnumBindingSource Members

        private Type _enumType;
        public Type EnumType
        {
            get { return this._enumType; }
            set
            {
                if (value != this._enumType)
                {
                    if (null != value)
                    {
                        Type enumType = Nullable.GetUnderlyingType(value) ?? value;
                        if (!enumType.IsEnum)
                            throw new ArgumentException("Type must be for an Enum.");
                    }

                    this._enumType = value;
                }
            }
        }

        public EnumBindingSource() { }

        public EnumBindingSource(Type enumType)
        {
            this.EnumType = enumType;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (null == this._enumType)
                throw new InvalidOperationException("The EnumType must be specified.");

            Type actualEnumType = Nullable.GetUnderlyingType(this._enumType) ?? this._enumType;
            Array enumValues = Enum.GetValues(actualEnumType);

            if (actualEnumType == this._enumType)
                return enumValues;

            Array tempArray = Array.CreateInstance(actualEnumType, enumValues.Length + 1);
            enumValues.CopyTo(tempArray, 1);
            return tempArray;
        }

        #endregion
    }

    public class EnumDescriptionTypeConverter : EnumConverter
    {
        #region EnumDescriptionTypeConverter Members

        public EnumDescriptionTypeConverter(Type type)
            : base(type)
        {
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                if (value != null)
                {
                    FieldInfo fi = value.GetType().GetField(value.ToString());
                    if (fi != null)
                    {
                        var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
                        return ((attributes.Length > 0) && (!String.IsNullOrEmpty(attributes[0].Description))) ? attributes[0].Description : value.ToString();
                    }
                }

                return string.Empty;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        #endregion
    }

    // Thanks to Lars: http://stackoverflow.com/a/406798
    public class EnumBooleanConverter : IValueConverter
    {
        #region EnumBooleanConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.Equals(true) ? parameter : Binding.DoNothing;
        }

        #endregion
    }



    public class KeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Enum.GetName(typeof(Key), value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (Key)Enum.Parse(typeof(Key), value.ToString());
        }
    }
}
