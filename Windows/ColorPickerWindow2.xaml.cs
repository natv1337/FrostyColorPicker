using Frosty.Controls;
using Frosty.Core.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
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
using FrostySdk.Ebx;
using FrostySdk;
using Microsoft.CSharp.RuntimeBinder;

namespace FrostyColorPicker.Windows
{
    /// <summary>
    /// Interaction logic for ColorPickerWindow2.xaml
    /// </summary>
    public partial class ColorPickerWindow2 : FrostyDockableWindow
    {
        public ColorPickerWindow2()
        {
            InitializeComponent();
        }

        private void importValuefromClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            // Checks if clipboard data exists (Needed bc it crashes if you don't have any).
            if (!FrostyClipboard.Current.HasData)
                return;

            // Get clipboard data.
            object obj = FrostyClipboard.Current.GetData(); 

            // Try to get a Vector3 out of the clipboard data.
            try
            {
                // Gets Frosty's Vector3 class
                dynamic vec3 = TypeLibrary.CreateObject("Vec3");
                vec3 = obj; // Moves the clipboard data into the vector3 instance.

                // Intensity multiplier for the certain assets that benefit from it.
                float intensityMultiplier = 1;
                if (useIntensityMultiplierCheckBox.IsChecked == true) // Use the user-defined intensity multiplier if the box is checked.
                    intensityMultiplier = float.Parse(intensityMultiplierBox.Text);

                if (vec3.x > 1 || vec3.y > 1 || vec3.z > 1)
                    calculateHdrCheckbox.IsChecked = true;

                // Checks for output type
                if (outputTypeComboBox.SelectedIndex == 0) // Simple Linear
                {
                    // Check if we should calculate with HDR
                    if (calculateHdrCheckbox.IsChecked == true)
                    {
                        redTextBox.Text = (Math.Round(linearSimpleFloatToSrgbChannel(vec3.x / 1.5f))).ToString();
                        greenTextBox.Text = (Math.Round(linearSimpleFloatToSrgbChannel(vec3.y / 1.5f))).ToString();
                        blueTextBox.Text = (Math.Round(linearSimpleFloatToSrgbChannel(vec3.z / 1.5f))).ToString();
                    }
                    else
                    {
                        redTextBox.Text = (Math.Round(linearSimpleFloatToSrgbChannel(vec3.x))).ToString();
                        greenTextBox.Text = (Math.Round(linearSimpleFloatToSrgbChannel(vec3.y))).ToString();
                        blueTextBox.Text = (Math.Round(linearSimpleFloatToSrgbChannel(vec3.z))).ToString();
                    }
                }
                else if (outputTypeComboBox.SelectedIndex == 1) // Linear
                {
                    // Check if we should calculate with HDR
                    if (calculateHdrCheckbox.IsChecked == true)
                    {
                        redTextBox.Text = (Math.Round(linearFloatToSrgbChannel(vec3.x / 1.5f) * 255)).ToString();
                        greenTextBox.Text = (Math.Round(linearFloatToSrgbChannel(vec3.y / 1.5f) * 255)).ToString();
                        blueTextBox.Text = (Math.Round(linearFloatToSrgbChannel(vec3.z / 1.5f) * 255)).ToString();
                    }
                    else
                    {
                        redTextBox.Text = (Math.Round(linearFloatToSrgbChannel(vec3.x) * 255)).ToString();
                        greenTextBox.Text = (Math.Round(linearFloatToSrgbChannel(vec3.y) * 255)).ToString();
                        blueTextBox.Text = (Math.Round(linearFloatToSrgbChannel(vec3.z) * 255)).ToString();
                    }
                }

                // Update X/Y/Z text boxes accordingly.
                xValueTextBox.Text = (vec3.x * intensityMultiplier).ToString();
                yValueTextBox.Text = (vec3.y * intensityMultiplier).ToString();
                zValueTextBox.Text = (vec3.z * intensityMultiplier).ToString();

                updateColorPreviewFrame(); // Update color preview
                updateHexadecimalBox(); // Update hexadecimal box
            }
            catch
            {
                // Empty catch statement (very helpful).
            }
        }

        #region RGB Input

        private void redTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!redTextBox.IsFocused)
                return;

            convertSrgbToVec3();
        }

        private void greenTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!greenTextBox.IsFocused)
                return;

            convertSrgbToVec3();
        }

        private void blueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!blueTextBox.IsFocused)
                return;

            convertSrgbToVec3();
        }
        #endregion

        #region Vector3 Input
        private void xValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!xValueTextBox.IsFocused)
                return;

            convertVec3ToSrgb();
        }

        private void yValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!yValueTextBox.IsFocused)
                return;

            convertVec3ToSrgb();
        }

        private void zValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!zValueTextBox.IsFocused)
                return;

            convertVec3ToSrgb();
        }
        #endregion

        private void useIntensityMultiplierCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            intensityMultiplierBox.IsEnabled = true;
            convertSrgbToVec3();
        }

        private void useIntensityMultiplierCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            intensityMultiplierBox.IsEnabled = false;
            convertSrgbToVec3();
        }

        private void intensityMultiplierBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            convertSrgbToVec3();
        }

        private void outputTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            convertSrgbToVec3();
        }

        private void calculateHdrCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            convertSrgbToVec3();
        }

        private void calculateHdrCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            convertSrgbToVec3();
        }

        // Exports the current Vector3 values to the FrostyClipboard so that they can be pasted directly into fields.
        private void exportValueToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create an instance of Frosty's Vector3 class.
                dynamic vec3 = TypeLibrary.CreateObject("Vec3");

                // Try to parse the values in the X/Y/Z text boxes into the Vector.
                vec3.x = float.Parse(xValueTextBox.Text);
                vec3.y = float.Parse(yValueTextBox.Text);
                vec3.z = float.Parse(zValueTextBox.Text);

                object obj = vec3; // Create an object with the Vector's data.
                FrostyClipboard.Current.SetData(obj); // Move the object into the FrostyClipboard.
            }
            catch
            {
                // Throw float parse failure error.
                FrostyMessageBox.Show("One of your Vector3 values are invalid (they should be floating-point numbers).", "Clipboard Export Error");
            }
        }

        private void hexadecimalTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!hexadecimalTextBox.IsFocused)
                return;

            string hexString = hexadecimalTextBox.Text;

            if (hexString.Contains("#"))
                hexString = hexString.Replace("#", "");

            if (hexString.Length < 6)
                return;

            try
            {
                System.Drawing.Color color = System.Drawing.Color.FromArgb(int.Parse(hexString, System.Globalization.NumberStyles.HexNumber));
                redTextBox.Text = color.R.ToString();
                greenTextBox.Text = color.G.ToString();
                blueTextBox.Text = color.B.ToString();
                convertSrgbToVec3();
            }
            catch
            {
                // Conversion error occured.
                FrostyMessageBox.Show("Invalid hexadecimal code.", "Conversion Error");
            }
        }

        #region Color Channel Converting
        // TODO: Look into updating to .NET 6.0 to use MathF.Pow

        // Convert sRGB linear float to sRGB channel float.
        public float linearFloatToSrgbChannel(float linear)
        {
            if (linear <= 0.0031308)
                return linear * 12.92f;

            return (float)(Math.Pow(linear, 0.41667) * 1.055) - 0.055f;
        }

        // Convert sRGB channel float into sRGB linear float.
        public float srgbChannelToLinear(float channel)
        {
            if (channel > 0.04045)
                return (float)Math.Pow((channel + 0.055) / 1.055, 2.4);

            return channel / 12.92f;
        }

        // Convert sRGB channel float to a simple value.
        public float srgbChannelToLinearSimple(float channel)
        {
            return channel / 255.0f;
        }

        // Convert a simple value to an sRGB channel.
        public float linearSimpleFloatToSrgbChannel(float simple)
        {
            return simple * 255.0f;
        }

        #endregion

        public void convertSrgbToVec3()
        {
            try
            {
                // Another check for seeing whether or not to use a user-defined intensity multiplier.
                float intensityMultiplier = 1;
                if (useIntensityMultiplierCheckBox.IsChecked == true)
                    intensityMultiplier = float.Parse(intensityMultiplierBox.Text);

                // Checks for output type to accurately convert colors for their proper use case.
                if (outputTypeComboBox.SelectedIndex == 0) // For simple sRGB Linear.
                {
                    if (calculateHdrCheckbox.IsChecked == true)
                    {
                        xValueTextBox.Text = ((srgbChannelToLinearSimple(float.Parse(redTextBox.Text)) * 1.5) * intensityMultiplier).ToString();
                        yValueTextBox.Text = ((srgbChannelToLinearSimple(float.Parse(greenTextBox.Text)) * 1.5) * intensityMultiplier).ToString();
                        zValueTextBox.Text = ((srgbChannelToLinearSimple(float.Parse(blueTextBox.Text)) * 1.5) * intensityMultiplier).ToString();
                    }
                    else
                    {
                        xValueTextBox.Text = (srgbChannelToLinearSimple(float.Parse(redTextBox.Text)) * intensityMultiplier).ToString();
                        yValueTextBox.Text = (srgbChannelToLinearSimple(float.Parse(greenTextBox.Text)) * intensityMultiplier).ToString();
                        zValueTextBox.Text = (srgbChannelToLinearSimple(float.Parse(blueTextBox.Text)) * intensityMultiplier).ToString();
                    }
                }
                else if (outputTypeComboBox.SelectedIndex == 1) // For sRGB Linear.
                {
                    if (calculateHdrCheckbox.IsChecked == true)
                    {
                        xValueTextBox.Text = ((srgbChannelToLinear(float.Parse(redTextBox.Text) / 255) * 1.5) * intensityMultiplier).ToString();
                        yValueTextBox.Text = ((srgbChannelToLinear(float.Parse(greenTextBox.Text) / 255) * 1.5) * intensityMultiplier).ToString();
                        zValueTextBox.Text = ((srgbChannelToLinear(float.Parse(blueTextBox.Text) / 255) * 1.5) * intensityMultiplier).ToString();
                    }
                    else
                    {
                        xValueTextBox.Text = (srgbChannelToLinear(float.Parse(redTextBox.Text) / 255) * intensityMultiplier).ToString();
                        yValueTextBox.Text = (srgbChannelToLinear(float.Parse(greenTextBox.Text) / 255) * intensityMultiplier).ToString();
                        zValueTextBox.Text = (srgbChannelToLinear(float.Parse(blueTextBox.Text) / 255) * intensityMultiplier).ToString();
                    }
                }

                updateColorPreviewFrame(); // Update color preview
                updateHexadecimalBox(); // Update hexadecimal box
            }
            catch
            {

            }
        }

        public void convertVec3ToSrgb()
        {
            try
            {
                float intensityMultiplier = 1;
                if (useIntensityMultiplierCheckBox.IsChecked == true)
                    intensityMultiplier = float.Parse(intensityMultiplierBox.Text);

                float x, y, z;
                x = float.Parse(xValueTextBox.Text);
                y = float.Parse(yValueTextBox.Text);
                z = float.Parse(zValueTextBox.Text);

                if (calculateHdrCheckbox.IsChecked == true)
                {
                    x /= 1.5f;
                    y /= 1.5f;
                    z /= 1.5f;
                }

                if (outputTypeComboBox.SelectedIndex == 0)
                {
                    x = (float)Math.Round(linearSimpleFloatToSrgbChannel(x) / intensityMultiplier);
                    y = (float)Math.Round(linearSimpleFloatToSrgbChannel(y) / intensityMultiplier);
                    z = (float)Math.Round(linearSimpleFloatToSrgbChannel(z) / intensityMultiplier );
                }
                else if (outputTypeComboBox.SelectedIndex == 1)
                {
                    x = (float)Math.Round(linearFloatToSrgbChannel(x) * 255 / intensityMultiplier);
                    y = (float)Math.Round(linearFloatToSrgbChannel(y) * 255 / intensityMultiplier);
                    z = (float)Math.Round(linearFloatToSrgbChannel(z) * 255 / intensityMultiplier);
                }

                redTextBox.Text = x.ToString();
                greenTextBox.Text = y.ToString();
                blueTextBox.Text = z.ToString();

                updateColorPreviewFrame();
                updateHexadecimalBox();
            }
            catch
            {

            }
        }

        // Create a SolidColorBrush for changing the color of the color preview box.
        public void updateColorPreviewFrame()
        {
            // Create a brush based on the RGB input.
            var brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(byte.Parse(redTextBox.Text), byte.Parse(greenTextBox.Text), byte.Parse(blueTextBox.Text)));
            // Change color preview frame background to the new brush.
            colorPreviewFrame.Background = brush;
        }

        // Convert sRGB channel values to hex strings to create a hexadecimal color code.
        public void updateHexadecimalBox()
        {
            // Create a brush based on the RGB input.
            var brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(byte.Parse(redTextBox.Text), byte.Parse(greenTextBox.Text), byte.Parse(blueTextBox.Text)));
            // Concat strings into the result.
            hexadecimalTextBox.Text = "#" + brush.Color.R.ToString("X2") + brush.Color.G.ToString("X2") + brush.Color.B.ToString("X2");
        }
    }
}
