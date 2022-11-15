using System;
using Frosty.Core;
using FrostyColorPicker.Windows;

namespace FrostyColorPicker
{
    public class ColorPickerMenuExtension : MenuExtension
    {
        public override string TopLevelMenuName => "Tools";
        public override string MenuItemName => "Color Menu";
        public override RelayCommand MenuItemClicked => new RelayCommand((Action<object>)delegate
        {
            ColorPickerWindow colorPickerWindow = new ColorPickerWindow();
            colorPickerWindow.Show();
        });
    }
}
