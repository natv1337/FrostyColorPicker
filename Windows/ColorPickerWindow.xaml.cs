using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Frosty.Controls;
using Frosty.Core.Controls;
using FrostySdk;

namespace FrostyColorPicker.Windows
{
    /// <summary>
    /// Interaction logic for ColorPickerWindow2.xaml
    /// </summary>
    public partial class ColorPickerWindow : FrostyDockableWindow
    {
        // My stupid solution for trying to stop these functions from recursively calling each other. But I guess the important thing is that it works.
        bool dontUpdateControls = false;
        bool convert = true;

        public ColorPickerWindow()
        {
            InitializeComponent();
        }

        #region Control Events
        private void xValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Prevent recursion by checking if the control is focused.
            if (!xValueTextBox.IsFocused)
                return;

            // Re-calculate srgb channel values.
            convertVectorToSrgb();
        }

        private void yValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Prevent recursion by checking if the control is focused.
            if (!yValueTextBox.IsFocused)
                return;

            // Re-calculate srgb channel values.
            convertVectorToSrgb();
        }

        private void zValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Prevent recursion by checking if the control is focused.
            if (!zValueTextBox.IsFocused)
                return;

            // Re-calculate srgb channel values.
            convertVectorToSrgb();
        }

        private void wValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Prevent recursion by checking if the control is focused.
            if (!wValueTextBox.IsFocused)
                return;

            // Re-calculate srgb channel values.
            convertVectorToSrgb();
        }

        private void useIntensityMultiplierCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // Enable intensity multiplier box.
            intensityMultiplierBox.IsEnabled = true;

            // Update Vec3 values.
            convertSrgbToVec3();
        }

        private void useIntensityMultiplierCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // Disable intensity multiplier box.
            intensityMultiplierBox.IsEnabled = false;

            // Update Vec3 values.
            convertSrgbToVec3();
        }

        private void intensityMultiplierBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Calculate new vec3 values by taking the srgb channel values and multiplying them.
            convertSrgbToVec3();
        }

        private void outputTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Re-calculate vec3 values upon changing the conversion type.
            convertSrgbToVec3();
        }

        private void vector4ToggleCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            wValueTextBox.IsEnabled = true;
        }

        private void vector4ToggleCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            wValueTextBox.IsEnabled = false;
        }

        private void calculateHdrCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            // Re-calculate vec3 values upon changing the checkbox.
            convertSrgbToVec3();
        }

        private void calculateHdrCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            // Re-calculate vec3 values upon changing the checkbox.
            convertSrgbToVec3();
        }

        // Imports Vec3 FrostyClipboard data into the window.
        private void importValuefromClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            // Checks if clipboard data exists (Needed bc it crashes if you don't have any).
            if (!FrostyClipboard.Current.HasData)
                return;

            // Get clipboard data.
            object obj = FrostyClipboard.Current.GetData();

            // Try to get either a Vec3 or Vec4 out of the clipboard data.
            dynamic vector;
            if (vector4ToggleCheckbox.IsChecked == true) // Gets Frosty's Vec4 class
            {
                vector = TypeLibrary.CreateObject("Vec4");
                vector = obj; // Moves the clipboard data into the vector3 instance.
            }
            else
            {
                vector = TypeLibrary.CreateObject("Vec3"); // Gets Frosty's Vec3 class
                vector = obj; // Moves the clipboard data into the vector3 instance.
            }

            float hdrDivisor = 1;
            // Intensity multiplier for the certain assets that benefit from it.
            float intensityMultiplier = 1;
            if (useIntensityMultiplierCheckBox.IsChecked == true) // Use the user-defined intensity multiplier if the box is checked.
                intensityMultiplier = float.Parse(intensityMultiplierBox.Text);

            // try-catch here to ensure that vector is actually is actually a Vec3/Vec4.
            try
            {
                // Update X/Y/Z/W text boxes accordingly.
                xValueTextBox.Text = (vector.x * intensityMultiplier).ToString();
                yValueTextBox.Text = (vector.y * intensityMultiplier).ToString();
                zValueTextBox.Text = (vector.z * intensityMultiplier).ToString();
            }
            catch
            {
                FrostyMessageBox.Show("Clipboard data is not of type Vec3 or Vec4.", "Clipboard Error");
                return;
            }

            // Import W value if it's a Vec4.
            if (vector4ToggleCheckbox.IsChecked == true)
                wValueTextBox.Text = vector.w.ToString();

            // Enables HDR calculation if value is > 1.
            if (vector.x > 1 || vector.y > 1 || vector.z > 1)
            {
                calculateHdrCheckbox.IsChecked = true;

                // With HDR calculation being enabled, we need to grab the largest of the three values in the vector and divide them all by it.
                hdrDivisor = getHighestVec3Value(vector.x, vector.y, vector.z);
            }

            // Checks for output type
            if (outputTypeComboBox.SelectedIndex == 0) // Simple Linear
            {
                // Check if we should calculate with HDR
                if (calculateHdrCheckbox.IsChecked == true)
                {
                    // Divide by HDR divisor.
                    vector.x /= hdrDivisor;
                    vector.y /= hdrDivisor;
                    vector.z /= hdrDivisor;
                }

                // Update color picker controls.
                updateSquarePickerSimple(vector.x, vector.y, vector.z);
            }
            else if (outputTypeComboBox.SelectedIndex == 1) // Linear
            {
                // Check if we should calculate with HDR.
                if (calculateHdrCheckbox.IsChecked == true)
                {
                    vector.x /= hdrDivisor;
                    vector.y /= hdrDivisor;
                    vector.z /= hdrDivisor;
                }

                // Update color picker controls.
                updateSquarePickerLinear(vector.x, vector.y, vector.z);
            }
        }

        // Exports the current Vector3 values to the FrostyClipboard so that they can be pasted directly into fields.
        private void exportValueToClipboardButton_Click(object sender, RoutedEventArgs e)
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

        private void SquarePicker_ColorChanged(object sender, RoutedEventArgs e)
        {
            // Try to stop recursion.
            if (dontUpdateControls)
                return;

            dontUpdateControls = true;

            // Set other control colors to the current color of this one.
            hexColorTextBox.SelectedColor = squarePicker.SelectedColor;
            colorSliders.SelectedColor = squarePicker.SelectedColor;

            // Creates a brush to change the color of the color preview frame.
            var newBrush = new SolidColorBrush(squarePicker.SelectedColor);
            colorPreviewFrame.Background = newBrush;

            // Update Vec3 values.
            if (convert)
                convertSrgbToVec3();
            dontUpdateControls = false;
        }

        private void colorSliders_ColorChanged(object sender, RoutedEventArgs e)
        {
            // Try to stop recursion.
            if (dontUpdateControls)
                return;

            dontUpdateControls = true;
            // Set other control colors to the current color of this one.
            squarePicker.SelectedColor = colorSliders.SelectedColor;
            hexColorTextBox.SelectedColor = colorSliders.SelectedColor;

            // Creates a brush to change the color of the color preview frame.
            var newBrush = new SolidColorBrush(squarePicker.SelectedColor);
            colorPreviewFrame.Background = newBrush;

            // Update Vec3 values.
            convertSrgbToVec3();
            dontUpdateControls = false;
        }

        private void hexColorTextBox_ColorChanged(object sender, RoutedEventArgs e)
        {
            // Try to stop recursion.
            if (dontUpdateControls)
                return;

            dontUpdateControls = true;
            // Set other control colors to the current color of this one.
            squarePicker.SelectedColor = hexColorTextBox.SelectedColor;
            colorSliders.SelectedColor = hexColorTextBox.SelectedColor;

            // Creates a brush to change the color of the color preview frame.
            var newBrush = new SolidColorBrush(squarePicker.SelectedColor);
            colorPreviewFrame.Background = newBrush;

            // Update Vec3 values.
            convertSrgbToVec3();
            dontUpdateControls = false;
        }
        #endregion

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

        #region Misc. Methods
        public void convertSrgbToVec3()
        {
            try // Random crash if this try-catch doesn't exist. Is the color picker trying to call it before it loads??
            {
                // Another check for seeing whether or not to use a user-defined intensity multiplier.
                float intensityMultiplier = 1;
                if (useIntensityMultiplierCheckBox.IsChecked == true)
                    intensityMultiplier = float.Parse(intensityMultiplierBox.Text);

                float x = 0, y = 0, z = 0, w = 0;

                // Checks for output type to accurately convert colors for their proper use case.
                if (outputTypeComboBox.SelectedIndex == 0) // For simple sRGB Linear.
                {
                    x = srgbChannelToLinearSimple(float.Parse(squarePicker.SelectedColor.R.ToString()) * intensityMultiplier);
                    y = srgbChannelToLinearSimple(float.Parse(squarePicker.SelectedColor.G.ToString()) * intensityMultiplier);
                    z = srgbChannelToLinearSimple(float.Parse(squarePicker.SelectedColor.B.ToString()) * intensityMultiplier);
                    w = srgbChannelToLinearSimple(float.Parse(squarePicker.SelectedColor.A.ToString()));
                }
                else if (outputTypeComboBox.SelectedIndex == 1) // For sRGB Linear.
                {
                    x = srgbChannelToLinear(float.Parse(squarePicker.SelectedColor.R.ToString()) / 255 * intensityMultiplier);
                    y = srgbChannelToLinear(float.Parse(squarePicker.SelectedColor.G.ToString()) / 255 * intensityMultiplier);
                    z = srgbChannelToLinear(float.Parse(squarePicker.SelectedColor.B.ToString()) / 255 * intensityMultiplier);
                    w = srgbChannelToLinear(float.Parse(squarePicker.SelectedColor.A.ToString()) / 255);
                }

                if (calculateHdrCheckbox.IsChecked == true)
                {
                    // I have no idea what I'm doing :/
                    //float hdrMultiplier = getHighestVec3Value(x, y, z);
                    x *= 1.5f;
                    y *= 1.5f;
                    z *= 1.5f;
                }

                // Convert vector values to string for usage in the text boxes.
                xValueTextBox.Text = x.ToString();
                yValueTextBox.Text = y.ToString();
                zValueTextBox.Text = z.ToString();
                wValueTextBox.Text = w.ToString();
            }
            catch
            {
                // lol
            }
        }

        public void convertVectorToSrgb()
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

            float hdrDivisor = getHighestVec3Value(x, y, z);

            if (calculateHdrCheckbox.IsChecked == true)
            {
                x /= hdrDivisor;
                y /= hdrDivisor;
                z /= hdrDivisor;
            }

            if (outputTypeComboBox.SelectedIndex == 0)
            {
                x = (float)Math.Round(linearSimpleFloatToSrgbChannel(x) / intensityMultiplier);
                y = (float)Math.Round(linearSimpleFloatToSrgbChannel(y) / intensityMultiplier);
                z = (float)Math.Round(linearSimpleFloatToSrgbChannel(z) / intensityMultiplier);
                w = (float)Math.Round(linearSimpleFloatToSrgbChannel(w) / intensityMultiplier);
            }
            else if (outputTypeComboBox.SelectedIndex == 1)
            {
                x = (float)Math.Round(linearFloatToSrgbChannel(x) * 255 / intensityMultiplier);
                y = (float)Math.Round(linearFloatToSrgbChannel(y) * 255 / intensityMultiplier);
                z = (float)Math.Round(linearFloatToSrgbChannel(z) * 255 / intensityMultiplier);
                w = (float)Math.Round(linearFloatToSrgbChannel(w) * 255 / intensityMultiplier);
            }

            try
            {
                convert = false;
                squarePicker.SelectedColor = Color.FromArgb(byte.Parse(w.ToString()), byte.Parse(x.ToString()), byte.Parse(y.ToString()), byte.Parse(z.ToString())); // This shouldn't be calling the other functions, but it works :p
            }
            catch
            {
                // Nope
            }
            convert = true;
        }

        // Update color picker controls based on linear srgb channels.
        public void updateSquarePickerLinear(float vecX, float vecY, float vecZ)
        {
            string red = (Math.Round(linearFloatToSrgbChannel(vecX) * 255f)).ToString();
            string green = (Math.Round(linearFloatToSrgbChannel(vecY) * 255f)).ToString();
            string blue = (Math.Round(linearFloatToSrgbChannel(vecZ) * 255f)).ToString();
            squarePicker.SelectedColor = Color.FromArgb(255, byte.Parse(red), byte.Parse(green), byte.Parse(blue));
        }

        // Update color picker controls based on simple linear srgb channels.
        public void updateSquarePickerSimple(float vecX, float vecY, float vecZ)
        {
            string red = (Math.Round(linearSimpleFloatToSrgbChannel(vecX))).ToString();
            string green = (Math.Round(linearSimpleFloatToSrgbChannel(vecY))).ToString();
            string blue = (Math.Round(linearSimpleFloatToSrgbChannel(vecZ))).ToString();
            squarePicker.SelectedColor = Color.FromArgb(255, byte.Parse(red), byte.Parse(green), byte.Parse(blue));
        }

        public float getHighestVec3Value(float vecX, float vecY, float vecZ)
        {
            if (vecX > vecY)
            {
                if (vecX > vecZ)
                    return vecX;
                else
                    return vecZ;
            }
            else if (vecY > vecZ)
                return vecY;
            else
                return vecZ;
        }
        #endregion
    }
}
