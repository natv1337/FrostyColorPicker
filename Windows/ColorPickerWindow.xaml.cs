using Frosty.Controls;
using Frosty.Core.Controls;
using FrostySdk;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FrostyColorPicker.Windows
{
    public partial class ColorPickerWindow : FrostyDockableWindow
    {
        private bool _dontUpdateControls = false;
        private bool _convert = true;
        private Brush _currentColor;

        public ColorPickerWindow()
        {
            InitializeComponent();
        }

        #region Control Events

        private void XValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!xValueTextBox.IsFocused)
                return;

            ConvertVectorToSrgb();
        }

        private void YValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!yValueTextBox.IsFocused)
                return;

            ConvertVectorToSrgb();
        }

        private void ZValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!zValueTextBox.IsFocused)
                return;

            ConvertVectorToSrgb();
        }

        private void WValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!wValueTextBox.IsFocused)
                return;

            ConvertVectorToSrgb();
        }

        private void UseIntensityMultiplierCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            intensityMultiplierBox.IsEnabled = true;
            ConvertSrgbToVec3();
        }

        private void UseIntensityMultiplierCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            intensityMultiplierBox.IsEnabled = false;
            ConvertSrgbToVec3();
        }

        private void IntensityMultiplierBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ConvertSrgbToVec3();
        }

        private void OutputTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConvertSrgbToVec3();
        }

        private void Vector4ToggleCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            wValueTextBox.IsEnabled = true;
        }

        private void Vector4ToggleCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            wValueTextBox.IsEnabled = false;
        }

        private void CalculateHdrCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            ConvertSrgbToVec3();
        }

        private void CalculateHdrCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            ConvertSrgbToVec3();
        }

        /// <summary>
        /// Imports Vec3 FrostyClipboard data into the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportValuefromClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            // Ensure that clipboard contains data and type is either Vec3 or Vec4 type.
            if(!FrostyClipboard.Current.HasData)
            {
                FrostyMessageBox.Show("Clipboard does not contain data!", "Error", MessageBoxButton.OK);
                return;
            }

            if (!(FrostyClipboard.Current.IsType(TypeLibrary.GetType("Vec3")) || FrostyClipboard.Current.IsType(TypeLibrary.GetType("Vec4"))))
            {
                FrostyMessageBox.Show("Clipboard data is not of type Vec3 or Vec4!", "Error", MessageBoxButton.OK);
                return;
            }

            // Intensity multiplier for the certain assets that benefit from it.

            float hdr = calculateHdrCheckbox.IsChecked == true ? 1.5f : 1.0f;
            float intensityMultiplier = 1.0f;

            dynamic vector = FrostyClipboard.Current.GetData();

            // Shitty hotfix to prevent values from re-multiplying themselves through intensity that probably might not work properly.
            if (useIntensityMultiplierCheckBox.IsChecked == true && vector.x / 1.5f < 1 && vector.y / 1.5f < 1 && vector.z / 1.5f < 1) // Use the user-defined intensity multiplier if the box is checked.
            {
                // Note
                // Using float.TryParse will not throw an exception (and therefore crash the editor) on invalid input.
                // It should also be faster because its implementation does not use exceptions.
                if (float.TryParse(intensityMultiplierBox.Text, out intensityMultiplier))
                    FrostyMessageBox.Show("Invalid intensity multiplier! (A valid float value is required)", "Error", MessageBoxButton.OK);
            }

            // Because of the entry clipboard data type check we are certain this is correct.  
            xValueTextBox.Text = (vector.x * intensityMultiplier * hdr).ToString();
            yValueTextBox.Text = (vector.y * intensityMultiplier * hdr).ToString();
            zValueTextBox.Text = (vector.z * intensityMultiplier * hdr).ToString();

            // Import W value if it's a Vec4.
            if (vector4ToggleCheckbox.IsChecked == true)
                wValueTextBox.Text = vector.w.ToString();

            vector.x /= hdr;
            vector.y /= hdr;
            vector.z /= hdr;

            _convert = false;

            if (outputTypeComboBox.SelectedIndex == 0) // Simple Linear
                UpdateSquarePickerSimple(vector.x, vector.y, vector.z); // Update color picker controls with simple linear values.
            else if (outputTypeComboBox.SelectedIndex == 1) // Linear
                UpdateSquarePickerLinear(vector.x, vector.y, vector.z); // Update color picker controls with linear values.
            
            _convert = true;
        }

        /// <summary>
        /// Exports the current Vector3 values to the FrostyClipboard so that they can be pasted directly into fields.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportValueToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                dynamic vector;

                // Create an instance of Frosty's Vector3/4 class.
                if (vector4ToggleCheckbox.IsChecked == true)
                    vector = TypeLibrary.CreateObject("Vec4");
                else
                    vector = TypeLibrary.CreateObject("Vec3");

                // Try to parse the values in the X/Y/Z text boxes into the Vector.
                vector.x = float.Parse(xValueTextBox.Text);
                vector.y = float.Parse(yValueTextBox.Text);
                vector.z = float.Parse(zValueTextBox.Text);

                // If it's a Vec4, move the W value into vector.w
                if (vector4ToggleCheckbox.IsChecked == true)
                    vector.w = float.Parse(wValueTextBox.Text);

                object obj = vector; // Create an object with the Vector's data.
                FrostyClipboard.Current.SetData(obj); // Move the object into the FrostyClipboard.
            }
            catch
            {
                // Should be caught if one of the vector values are invalid.
                FrostyMessageBox.Show("One of your vector values are invalid (they should be floating-point numbers).", "Clipboard Error");
            }
        }

        // TODO | This is not a good way of handling the color changed events. Doing all of this ends up making the square picker and sliders a bit laggy.
        // Find a way to update the Vec3 values without sacrificing performance of the the two controls.
        private void SquarePicker_ColorChanged(object sender, RoutedEventArgs e)
        {
            _dontUpdateControls = true;

            hexColorTextBox.SelectedColor = squarePicker.SelectedColor;

            // Update Vec3 values.
            if (_convert)
                ConvertSrgbToVec3();

            _dontUpdateControls = false;
        }

        private void HexColorTextBox_ColorChanged(object sender, RoutedEventArgs e)
        {
            // Set other control colors to the current color of this one.
            if (!_dontUpdateControls)
            {
                squarePicker.SelectedColor = hexColorTextBox.SelectedColor;
                colorSliders.SelectedColor = hexColorTextBox.SelectedColor;
            }

            // Creates a brush to change the color of the color preview frame.
            _currentColor = new SolidColorBrush(squarePicker.SelectedColor);
            colorPreviewFrame.Background = _currentColor;
        }

        #endregion

        #region Color Channel Converting

        // TODO: Look into updating to .NET 6.0 to use MathF.Pow

        /// <summary>
        /// Convert sRGB linear float to sRGB channel float.
        /// </summary>
        /// <param name="linear"></param>
        /// <returns></returns>
        public float LinearFloatToSrgbChannel(float linear)
        {
            if (linear <= 0.0031308)
                return linear * 12.92f;

            return (float)(Math.Pow(linear, 0.41667) * 1.055) - 0.055f;
        }

        /// <summary>
        /// Convert sRGB channel float into sRGB linear float.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public float SrgbChannelToLinear(float channel)
        {
            if (channel > 0.04045)
                return (float)Math.Pow((channel + 0.055) / 1.055, 2.4);

            return channel / 12.92f;
        }

        /// <summary>
        /// Convert sRGB channel float to a simple value.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public float SrgbChannelToLinearSimple(float channel)
        {
            return channel / 255.0f;
        }

        /// <summary>
        /// Convert a simple value to an sRGB channel.
        /// </summary>
        /// <param name="simple"></param>
        /// <returns></returns>
        public float LinearSimpleFloatToSrgbChannel(float simple)
        {
            return simple * 255.0f;
        }

        #endregion

        #region Misc. Methods

        /// <summary>
        /// Re-calculate Vec3 values.
        /// </summary>
        public void ConvertSrgbToVec3()
        {
            if (IsLoaded)
            {
                // Another check for seeing whether or not to use a user-defined intensity multiplier.
                float intensityMultiplier = 1;
                if (useIntensityMultiplierCheckBox.IsChecked == true)
                    intensityMultiplier = float.Parse(intensityMultiplierBox.Text);

                float hdr = 1;
                if (calculateHdrCheckbox.IsChecked == true)
                    hdr = 1.5f;

                float x = 0, y = 0, z = 0, w = 0;

                // Checks for output type to accurately convert colors for their proper use case.
                if (outputTypeComboBox.SelectedIndex == 0) // For simple sRGB Linear.
                {
                    x = SrgbChannelToLinearSimple(float.Parse(squarePicker.SelectedColor.R.ToString()) * intensityMultiplier);
                    y = SrgbChannelToLinearSimple(float.Parse(squarePicker.SelectedColor.G.ToString()) * intensityMultiplier);
                    z = SrgbChannelToLinearSimple(float.Parse(squarePicker.SelectedColor.B.ToString()) * intensityMultiplier);
                    w = SrgbChannelToLinearSimple(float.Parse(squarePicker.SelectedColor.A.ToString())) * intensityMultiplier;
                }
                else if (outputTypeComboBox.SelectedIndex == 1) // For sRGB Linear.
                {
                    x = SrgbChannelToLinear(float.Parse(squarePicker.SelectedColor.R.ToString()) / 255 * intensityMultiplier);
                    y = SrgbChannelToLinear(float.Parse(squarePicker.SelectedColor.G.ToString()) / 255 * intensityMultiplier);
                    z = SrgbChannelToLinear(float.Parse(squarePicker.SelectedColor.B.ToString()) / 255 * intensityMultiplier);
                    w = SrgbChannelToLinear(float.Parse(squarePicker.SelectedColor.A.ToString()) / 255 * intensityMultiplier);
                }

                x *= hdr;
                y *= hdr;
                z *= hdr;

                // Convert vector values to string for usage in the text boxes.
                xValueTextBox.Text = x.ToString();
                yValueTextBox.Text = y.ToString();
                zValueTextBox.Text = z.ToString();
                wValueTextBox.Text = w.ToString();
            }
        }

        /// <summary>
        /// Re-calculate srgb channel values.
        /// </summary>
        public void ConvertVectorToSrgb()
        {
            float intensityMultiplier = 1;
            if (useIntensityMultiplierCheckBox.IsChecked == true)
                intensityMultiplier = float.Parse(intensityMultiplierBox.Text);

            float x, y, z, w;
            try
            {
                x = float.Parse(xValueTextBox.Text);
                y = float.Parse(yValueTextBox.Text);
                z = float.Parse(zValueTextBox.Text);
                w = float.Parse(wValueTextBox.Text);
            }
            catch
            {
                // Conversion Error
                return;
            }

            if (calculateHdrCheckbox.IsChecked == true)
            {
                x /= 1.5f;
                y /= 1.5f;
                z /= 1.5f;
            }

            if (outputTypeComboBox.SelectedIndex == 0)
            {
                x = (float)Math.Round(LinearSimpleFloatToSrgbChannel(x) / intensityMultiplier);
                y = (float)Math.Round(LinearSimpleFloatToSrgbChannel(y) / intensityMultiplier);
                z = (float)Math.Round(LinearSimpleFloatToSrgbChannel(z) / intensityMultiplier);
                w = (float)Math.Round(LinearSimpleFloatToSrgbChannel(w) / intensityMultiplier);
            }
            else if (outputTypeComboBox.SelectedIndex == 1)
            {
                x = (float)Math.Round(LinearFloatToSrgbChannel(x) * 255 / intensityMultiplier);
                y = (float)Math.Round(LinearFloatToSrgbChannel(y) * 255 / intensityMultiplier);
                z = (float)Math.Round(LinearFloatToSrgbChannel(z) * 255 / intensityMultiplier);
                w = (float)Math.Round(LinearFloatToSrgbChannel(w) * 255 / intensityMultiplier);
            }

            try
            {
                _convert = false;

                if (vector4ToggleCheckbox.IsChecked == true)
                    squarePicker.SelectedColor = Color.FromArgb(byte.Parse(w.ToString()), byte.Parse(x.ToString()), byte.Parse(y.ToString()), byte.Parse(z.ToString()));
                else
                    squarePicker.SelectedColor = Color.FromArgb(255, byte.Parse(x.ToString()), byte.Parse(y.ToString()), byte.Parse(z.ToString()));
            }
            catch
            {
                // :nope:
            }

            _convert = true;
        }

        /// <summary>
        /// Update color picker controls based on linear srgb channels.
        /// </summary>
        /// <param name="vecX"></param>
        /// <param name="vecY"></param>
        /// <param name="vecZ"></param>
        public void UpdateSquarePickerLinear(float vecX, float vecY, float vecZ)
        {
            string red = (Math.Round(LinearFloatToSrgbChannel(vecX) * 255f)).ToString();
            string green = (Math.Round(LinearFloatToSrgbChannel(vecY) * 255f)).ToString();
            string blue = (Math.Round(LinearFloatToSrgbChannel(vecZ) * 255f)).ToString();

            try
            {
                squarePicker.SelectedColor = Color.FromArgb(255, byte.Parse(red), byte.Parse(green), byte.Parse(blue));
            }
            catch
            {
                // Failed to convert color val for whatever reason.
            }
        }

        /// <summary>
        /// Update square picker controls based on simple linear srgb channels.
        /// </summary>
        /// <param name="vecX"></param>
        /// <param name="vecY"></param>
        /// <param name="vecZ"></param>
        public void UpdateSquarePickerSimple(float vecX, float vecY, float vecZ)
        {
            string red = (Math.Round(LinearSimpleFloatToSrgbChannel(vecX))).ToString();
            string green = (Math.Round(LinearSimpleFloatToSrgbChannel(vecY))).ToString();
            string blue = (Math.Round(LinearSimpleFloatToSrgbChannel(vecZ))).ToString();
            try
            {
                squarePicker.SelectedColor = Color.FromArgb(255, byte.Parse(red), byte.Parse(green), byte.Parse(blue));
            }
            catch
            {
                // Failed to convert color val for whatever reason.
            }
        }

        #endregion
    }
}
