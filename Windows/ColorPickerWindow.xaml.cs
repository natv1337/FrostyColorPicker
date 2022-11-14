using Frosty.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FrostyColorPicker.Windows
{
    /// <summary>
    /// Interaction logic for ColorPickerWindow.xaml
    /// </summary>
    public partial class ColorPickerWindow : FrostyDockableWindow
    {
        // srgb to linear convert 1/255	= 1.5/382.5

        Vector3 srgbLinearVals = new Vector3(0.0f, 0.0f, 0.0f);

        public ColorPickerWindow()
        {
            InitializeComponent();
        }

        private void redValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!redValueTextBox.IsFocused)
                return;
        }

        private void xValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!xValueTextBox.IsFocused)
                return;
        }

        private void hexadecimalTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Prevents code from executing when other methods causes a change to the textbox.
            if (!hexadecimalTextBox.IsFocused)
                return;

            // Stores the text box thing in a variable
            string hexString = hexadecimalTextBox.Text;

            //if (!hexString.Contains("#"))
                //hexString = hexString.Insert(0, "#");

            if (hexString.Contains("#"))
                hexString = hexString.Replace("#", "");

            if (hexString.Length < 6)
                return;

            System.Drawing.Color color = System.Drawing.Color.FromArgb(int.Parse(hexString, System.Globalization.NumberStyles.HexNumber));

            redValueTextBox.Text = color.R.ToString();
            greenValueTextBox.Text = color.G.ToString();
            blueValueTextBox.Text = color.B.ToString();
            fullRgbTextBox.Text = color.R.ToString() + ", " + color.G.ToString() + ", " + color.B.ToString();

            if (hdrToggle.IsChecked == true)
            {
                xValueTextBox.Text = (srgbToLinearFloat(color.R) * 1.5f).ToString();
                yValueTextBox.Text = (srgbToLinearFloat(color.G) * 1.5f).ToString();
                zValueTextBox.Text = (srgbToLinearFloat(color.B) * 1.5f).ToString();
            }
            else
            {
                xValueTextBox.Text = srgbToLinearFloat(color.R).ToString();
                yValueTextBox.Text = srgbToLinearFloat(color.G).ToString();
                zValueTextBox.Text = srgbToLinearFloat(color.B).ToString();
            }

            fullVec3TextBox.Text = xValueTextBox.Text + ", " + yValueTextBox.Text + ", " + zValueTextBox.Text;
        }

        private void hdrToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (xValueTextBox.Text.Length > 0 && yValueTextBox.Text.Length > 0 && zValueTextBox.Text.Length > 0)
            {
                
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerWindow2 colorPickerWindow2 = new ColorPickerWindow2();
            colorPickerWindow2.Show();
        }

        public float srgbToLinearFloat(float srgb)
        {
            if (srgb <= 0.04045)
                return srgb / 12.92f;
            else
                return (float)Math.Pow((srgb + 0.055) / 1.055, 2.4);
        }

        public float linearFloatToSrgbFloat(float linear)
        {
            if (linear <= 0.0031308)
                return linear * 12.92f;
            else
                return 1.055f * (float)Math.Pow(linear, 1.0f / 2.4f) - 0.055f;
        }
    }
}
