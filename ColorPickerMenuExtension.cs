using System;
using System.Windows;
using FrostyColorPicker;
using Frosty.Core;
using FrostyColorPicker.Windows;

namespace FrostyColorPicker
{
    public class ColorPickerMenuExtension : MenuExtension
    {
        public override string TopLevelMenuName => "Tools";
        public override string MenuItemName => "Color Picker";
        public override RelayCommand MenuItemClicked => new RelayCommand((Action<object>)delegate
        {
            ColorPickerWindow2 colorPickerWindow = new ColorPickerWindow2();
            colorPickerWindow.Show();
            //ColorPickerWindow colorPickerWindow = new ColorPickerWindow();
            //colorPickerWindow.Show();
            //ChooseColorWindow chooseColorWindow = new ChooseColorWindow();
            //chooseColorWindow.Show();
        });
    }
}
