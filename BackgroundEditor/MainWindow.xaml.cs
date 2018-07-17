using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Background_Creator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public string userSelectionSingleOrMultiple;
        public bool userSelectionLoseslessImages;
        public bool userSelectionCustomResolution;


        public MainWindow()
        {
            //ImageLoading.Visibility = System.Windows.Visibility.Hidden;
            //C:\Programming\Visual Studio Projects\BackgroundEditor\BackgroundEditor\bin\Debug\Created Backgrounds
            InitializeComponent();
            userSelectionSingleOrMultiple = ButtonCreateSingle.Content.ToString(); // Create Single Background
            userSelectionCustomResolution = false;
            TextBoxCreatePath.Text = (string) BackgroundCreator.Properties.Settings.Default["CreatePath"];
            TextBoxSaveLocation.Text = (string) BackgroundCreator.Properties.Settings.Default["SaveLocation"];
            //ComboBoxResolutions.Text = (string) BackgroundCreator.Properties.Settings.Default["Resolution"];
            TextBoxTargetResolutionWidth.Text = (string) BackgroundCreator.Properties.Settings.Default["CustomResolution1"];
            TextBoxTargetResolutionHeight.Text = (string) BackgroundCreator.Properties.Settings.Default["CustomResolution2"];
            userSelectionLoseslessImages = (bool) BackgroundCreator.Properties.Settings.Default["LoselessResolution"];
            SliderBlackBarPercentHeight.Value = (int) BackgroundCreator.Properties.Settings.Default["BlackBarHeight"];
            ComboBoxBlackBarExclusion.SelectedIndex = (int) BackgroundCreator.Properties.Settings.Default["Exclude"];
            SliderBackgroundImageOpacity.Value = (int) BackgroundCreator.Properties.Settings.Default["BackgroundOpacity"];

            if (!Directory.Exists(TextBoxSaveLocation.Text))
                TextBoxSaveLocation.Text = Directory.GetCurrentDirectory() + @"\Created Backgrounds";

            if (!ComboBoxResolutions.Items.Contains(SystemParameters.PrimaryScreenWidth.ToString() + " x " + SystemParameters.PrimaryScreenHeight.ToString()))
            {
                ComboBoxResolutions.Items.Add(SystemParameters.PrimaryScreenWidth.ToString() + " x " + SystemParameters.PrimaryScreenHeight.ToString());
            }
            ComboBoxResolutions.Text = SystemParameters.PrimaryScreenWidth.ToString() + " x " + SystemParameters.PrimaryScreenHeight.ToString();

            if (userSelectionLoseslessImages == true)
            {
                ButtonCheckBoxLosslessImages.Content = "✓";
                ButtonCheckBoxLosslessImages.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 255, 0));
            }

        }

        private void ButtonSelectImageOrFolder_Click(object sender, RoutedEventArgs e)
        {

            if (userSelectionSingleOrMultiple.Equals(ButtonCreateSingle.Content.ToString())) // if user has create single selected
            {
                string initialDirectory = @"C:\";

                if (TextBoxCreatePath.Text.Length > 0 && TextBoxCreatePath.Text.Contains(@"\"))
                {
                    if (Directory.Exists(TextBoxCreatePath.Text.Substring(0, TextBoxCreatePath.Text.LastIndexOf(@"\"))))
                        initialDirectory = TextBoxCreatePath.Text.Substring(0, TextBoxCreatePath.Text.LastIndexOf(@"\"));
                }
                else
                    TextBoxCreatePath.Text = "";
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    InitialDirectory = initialDirectory,
                    Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.bmp, *.png) | *.jpg; *.jpeg; *.jpe; *.bmp; *.png"
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    TextBoxCreatePath.Text = openFileDialog.FileName;
                    BitmapImage bitmapImage = new BitmapImage(new Uri(openFileDialog.FileName));
                    Image1.Source = bitmapImage;
                    ImageBrush imageBrush = new ImageBrush
                    {
                        ImageSource = Image1.Source
                    };
                    //Canvas1.Background = imageBrush;
                    //ButtonCreateImage.IsEnabled = true;
                }
            }
            else // if user has create multiple selected
            {
                CommonOpenFileDialog commonOpenFileDialog = new CommonOpenFileDialog
                {
                    InitialDirectory = @"C:\",
                    IsFolderPicker = true
                };
                if (commonOpenFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    TextBoxCreatePath.Text = commonOpenFileDialog.FileName + @"\";
                    //ButtonCreateImage.IsEnabled = true;
                }

            }
        }

        private void ButtonSelectNewImageSaveLocation_Click(object sender, RoutedEventArgs e) // Folder Selection
        {
            CommonOpenFileDialog commonOpenFileDialog = new CommonOpenFileDialog
            {
                InitialDirectory = @"C:\",
                IsFolderPicker = true
            };
            if (commonOpenFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {

                TextBoxSaveLocation.Text = commonOpenFileDialog.FileName;
            }
        }

        private void ButtonUserChoice_Click(object sender, RoutedEventArgs e) // User Choice, single image or folder of images
        {
            Button TempButton = (Button)sender;

            if (!userSelectionSingleOrMultiple.Equals(TempButton.Content.ToString())) // if Button NOT selected already, update user selection
            {
                TempButton.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                userSelectionSingleOrMultiple = TempButton.Content.ToString();

                //Console.WriteLine(userSelectionSingleOrMultiple);
            }

            if (!userSelectionSingleOrMultiple.Equals(ButtonCreateSingle.Content.ToString())) // Button Create Multiple is selected
            {
                try
                {
                    TextBoxCreatePath.Text = TextBoxCreatePath.Text.Substring(0, TextBoxCreatePath.Text.LastIndexOf(@"\")) + @"\";
                } catch (Exception error)
                {
                    Console.WriteLine(error);
                }


                TempButton = ButtonCreateSingle;
                ButtonSelectImageOrFolder.Content = "SELECT FOLDER";
                ButtonCreateImage.Content = "CREATE IMAGES";
                Image1.Source = null;
                //Image1.Visibility = Visibility.Hidden;
            }
            else // Button Create Single is selected
            {   
                TempButton = ButtonCreateMultiple;
                ButtonSelectImageOrFolder.Content = "SELECT IMAGE";
                ButtonCreateImage.Content = "CREATE IMAGE";
            }


            EnableOrDisableButtonCreateImage();
            TempButton.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));
        }

        private static int FindHighestCommonFactor(int number1, int number2) //  CalculateNearestResolutionToTarget Helper
        {
            while (number2 > 0)
            {
                int temp = number2;
                number2 = number1 % number2;
                number1 = temp;
            }
            return number1;
        }

        public static Point CalculateNewResolution(int width, int height, int targetWidth, int targetHeight)
        {

            int targetHighestCommonFactor = FindHighestCommonFactor(targetWidth, targetHeight);
            Point aspectRatio = new Point(targetWidth / targetHighestCommonFactor, targetHeight / targetHighestCommonFactor);

            Point newResolution = new Point(aspectRatio.X, aspectRatio.Y);
            //Console.WriteLine("\n\n");
            while (newResolution.X < targetWidth || newResolution.X < width || newResolution.Y < targetHeight || newResolution.Y < height)
            {
                // while newResolution X is smaller than width of given image, or width is smaller than target width - increase resolution

                newResolution.X += aspectRatio.X;
                newResolution.Y += aspectRatio.Y;
                //Console.WriteLine(newResolution.X + " : " + newResolution.Y);
            }
            //Console.WriteLine("\n\n");

            /*
            Console.WriteLine("Aspect Ratio " + aspectRatio.X + " : " + aspectRatio.Y);
            Console.WriteLine("Original " + width + " : " + height + "\tTarget " + targetWidth + " : " + targetHeight);
            Console.WriteLine("New Resolution " + newResolution.X + " : " + newResolution.Y + "\n");
            */
            return newResolution;
        }

        public void CreateNewBackground(ImageSource imageSource, string filePath)
        {
            int targetResolutionWidth = Int32.Parse(ComboBoxResolutions.Text.Substring(0, ComboBoxResolutions.Text.IndexOf(" ")));
            int targetResolutionHeight = Int32.Parse(ComboBoxResolutions.Text.Substring(ComboBoxResolutions.Text.LastIndexOf(" ") + 1));
            Point targetResolution = new Point(targetResolutionWidth, targetResolutionHeight);
            if (userSelectionCustomResolution == true)
                targetResolution = new Point(Double.Parse(TextBoxTargetResolutionWidth.Text), Double.Parse(TextBoxTargetResolutionHeight.Text));

            DrawingVisual drawingVisual = new DrawingVisual();
            bool AddBlackBars = false;
            Point Resolution = new Point(0, 0);
            Point imageResolution = new Point(0, 0);
            Resolution = new Point(targetResolution.X, targetResolution.Y);
            if (SliderBlackBarPercentHeight.Value > 0)
                AddBlackBars = true;
            double userSelectionBlackBarPercent = SliderBlackBarPercentHeight.Value / 100;
            //Console.WriteLine("ORIGINAL WIDTH : HEIGHT " + imageSource.Width + " : " + imageSource.Height);

            // ===================================================================================================================================================

            Point imageCenterPosition = new Point(0, 0);
            int blackBarHeight = 0;
            bool imageNotBigEnoughForLosslessImagesAnyway = false;
            if (imageSource.Width < targetResolution.X && imageSource.Height < targetResolution.Y && userSelectionLoseslessImages == true)
            {
                userSelectionLoseslessImages = false;
                imageNotBigEnoughForLosslessImagesAnyway = true;
            }



            if (userSelectionLoseslessImages == true)
            {
                if (AddBlackBars)
                {
                    Resolution = CalculateNewResolution((int)imageSource.Width, (int)imageSource.Height, (int)targetResolution.X, (int)targetResolution.Y);
                    imageResolution.X = imageSource.Width - (imageSource.Width * userSelectionBlackBarPercent * 2);
                    blackBarHeight = (int)(userSelectionBlackBarPercent * Resolution.Y);
                    imageResolution.Y = imageSource.Height - (imageSource.Height * userSelectionBlackBarPercent * 2);
                }
                else
                {
                    imageResolution.X = imageSource.Width;
                    imageResolution.Y = imageSource.Height;
                    Resolution = CalculateNewResolution((int)imageSource.Width, (int)imageSource.Height, (int)targetResolution.X, (int)targetResolution.Y);
                }
                imageCenterPosition = new Point((Resolution.X / 2) - imageResolution.X / 2, (Resolution.Y / 2) - (imageResolution.Y / 2));

            }
            else if (userSelectionLoseslessImages == false)
            {

                imageResolution.Y = (int)((imageSource.Height / imageSource.Width) * targetResolution.X);
                imageResolution.X = (int)((imageSource.Width / imageSource.Height) * targetResolution.Y);

                if (imageResolution.X <= targetResolution.X)
                {
                    imageResolution.Y = targetResolution.Y;
                }
                else if (imageResolution.Y <= targetResolution.Y)
                {
                    imageResolution.X = targetResolution.X;
                }

                blackBarHeight = (int)(userSelectionBlackBarPercent * Resolution.Y);
                if (AddBlackBars == true)
                {
                    imageResolution.X -= Math.Ceiling(imageResolution.X * (SliderBlackBarPercentHeight.Value / 100) * 2); // 10% --> .10
                    imageResolution.Y -= Math.Ceiling(imageResolution.Y * (SliderBlackBarPercentHeight.Value / 100) * 2);
                }
                Resolution = new Point(targetResolution.X, targetResolution.Y);
                imageCenterPosition = new Point((targetResolution.X / 2) - imageResolution.X / 2, (targetResolution.Y / 2) - (imageResolution.Y / 2));
            }

            //Debug.WriteLine("Condition #0");

            // ===================================================================================================================================================

            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {

                if (AddBlackBars == true)
                {
                    int blackBarCount = 2;
                    if (ComboBoxBlackBarExclusion.SelectedIndex != 0)
                        blackBarCount = 1;


                    int topBarYPosition = blackBarHeight;
                    /*
                    if (ComboBoxBlackBarExclusion.SelectedIndex == 0) // ComboBoxBlackBarExclusion index = NONE
                    {

                    } */
                    if (ComboBoxBlackBarExclusion.SelectedIndex == 1) // ComboBoxBlackBarExclusion index = TOP
                    {
                        topBarYPosition = (int)Resolution.Y - blackBarHeight;
                        blackBarCount = 1;
                    }
                    else if (ComboBoxBlackBarExclusion.SelectedIndex == 2) // ComboBoxBlackBarExclusion index = BOTTOM
                    {

                        topBarYPosition = blackBarHeight;
                        blackBarCount = 1;
                    }



                    if (blackBarCount == 1)
                    {
                        if (ComboBoxBlackBarExclusion.SelectedIndex == 1) // TOP // Stetch Upward
                        {
                            Debug.WriteLine("Condition #1");
                            drawingContext.DrawRectangle(Brushes.Black, null, new Rect(0, 0, Resolution.X, Resolution.Y)); // Background Black Rectangle
                            drawingContext.PushOpacity(SliderBackgroundImageOpacity.Value / 100); // Change Opacity
                            drawingContext.DrawImage(imageSource, new Rect(0, 0, Resolution.X, Resolution.Y - blackBarHeight)); // Stretched Background IMAGE
                            drawingContext.Pop(); // Remove Opacity
                            drawingContext.DrawImage(imageSource, new Rect(imageCenterPosition.X, imageCenterPosition.Y - (blackBarHeight / 2), imageResolution.X, imageResolution.Y)); // Original sized Foreground IMAGE
                        }

                        if (ComboBoxBlackBarExclusion.SelectedIndex == 2) // BOTTOM // Stetch Downward
                        {
                            Debug.WriteLine("Condition #2");
                            drawingContext.DrawRectangle(Brushes.Black, null, new Rect(0, 0, Resolution.X, Resolution.Y)); // Background Black Rectangle
                            drawingContext.PushOpacity(SliderBackgroundImageOpacity.Value / 100); // Change Opacity
                            drawingContext.DrawImage(imageSource, new Rect(0, blackBarHeight, Resolution.X, Resolution.Y - blackBarHeight)); // Stretched Background IMAGE
                            drawingContext.Pop(); // Remove Opacity
                            drawingContext.DrawImage(imageSource, new Rect(imageCenterPosition.X, imageCenterPosition.Y + (blackBarHeight / 2), imageResolution.X, imageResolution.Y)); // Original sized Foreground IMAGE
                        }
                    }

                    else if (blackBarCount == 2)
                    {
                        Debug.WriteLine("Condition #3");
                        drawingContext.DrawRectangle(Brushes.Black, null, new Rect(0, 0, Resolution.X, Resolution.Y)); // Background Fill Rectangle
                        drawingContext.PushOpacity(SliderBackgroundImageOpacity.Value / 100); // 10% Opacity
                        drawingContext.DrawImage(imageSource, new Rect(0, topBarYPosition, Resolution.X, Resolution.Y - blackBarHeight * blackBarCount)); // Stretched Background IMAGE
                        drawingContext.Pop(); // Remove Opacity
                        drawingContext.DrawImage(imageSource, new Rect(imageCenterPosition.X, imageCenterPosition.Y, imageResolution.X, imageResolution.Y)); // Original sized Foreground IMAGE                                                                                                            //Console.WriteLine(true);
                    }

                }

                else if (AddBlackBars == false)
                {
                    Debug.WriteLine("Condition #4");
                    drawingContext.DrawRectangle(Brushes.Black, null, new Rect(0, 0, Resolution.X, Resolution.Y)); // Background Fill Rectangle
                    drawingContext.PushOpacity(SliderBackgroundImageOpacity.Value / 100); // 10% Opacity
                    drawingContext.DrawImage(imageSource, new Rect(0, 0, Resolution.X, Resolution.Y)); // Stretched Background IMAGE
                    drawingContext.Pop(); // Remove Opacity
                    drawingContext.DrawImage(imageSource, new Rect(imageCenterPosition.X, imageCenterPosition.Y, imageResolution.X, imageResolution.Y)); // Original sized Foreground IMAGE
                                                                                                                                                         //Console.WriteLine(false);
                }
            }

            DrawingImage image = new DrawingImage(drawingVisual.Drawing);
            Image1.Source = image;
            ImageSource img = Image1.Source;

            // Save Image
            BitmapSource bmp = ToBitmapSource((DrawingImage)img);
            File.SetAttributes(TextBoxSaveLocation.Text, FileAttributes.Normal);
            using (var fileStream = new FileStream(TextBoxSaveLocation.Text + @"\" + filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));
                encoder.Save(fileStream);
            } // - Save image
            if (imageNotBigEnoughForLosslessImagesAnyway == true)
            {
                userSelectionLoseslessImages = true; // Set this to false at the top and reset back to true now so that when the user clicks the "loseless images" checkbox it will work properly
            }
        }

        public static BitmapSource ToBitmapSource(DrawingImage source)
        {
            
            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawImage(source, new Rect(new Point(0, 0), new Size(source.Width, source.Height)));
            drawingContext.Close(); 
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)source.Width, (int)source.Height, 96, 96, PixelFormats.Pbgra32);
            renderTargetBitmap.Render(drawingVisual);

            return renderTargetBitmap;


            /*
            // Define parameters used to create the BitmapSource.
            PixelFormat pixelFormat = PixelFormats.Bgr32;
            int rawStride = ((int) source.Width * pixelFormat.BitsPerPixel + 7) / 8;
            byte[] rawImage = new byte[rawStride * (int) source.Height];

            // Initialize the image with data.
            Random value = new Random();
            value.NextBytes(rawImage);

            // Create a BitmapSource.
            BitmapSource bitmap1 = BitmapSource.Create((int) source.Width, (int) source.Height, 96, 96, pixelFormat, null, rawImage, rawStride);

            return bitmap1;
            */
        }

        private void SliderBlackBarPercentHeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TextBoxBlackBarPercentHeight.Text = SliderBlackBarPercentHeight.Value * 2 + "%";
        }

        private void SliderBackgroundImageOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TextBoxBackgroundImageOpacity.Text = SliderBackgroundImageOpacity.Value + "%";
        }

        private void ButtonCheckBoxLosslessImages_Click(object sender, RoutedEventArgs e)
        {
            if (userSelectionLoseslessImages == true)
            {
                ButtonCheckBoxLosslessImages.Content = "";
                ButtonCheckBoxLosslessImages.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));
                userSelectionLoseslessImages = false;
            }
            else if (userSelectionLoseslessImages == false)
            {
                ButtonCheckBoxLosslessImages.Content = "✓";
                ButtonCheckBoxLosslessImages.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                userSelectionLoseslessImages = true;
            }
        }

        public static String[] GetFilesFrom(String searchFolder, String[] filters, bool isRecursive)
        {
            List<String> filesFound = new List<String>();
            var searchOption = isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (var filter in filters)
            {
                filesFound.AddRange(Directory.GetFiles(searchFolder, String.Format("*.{0}", filter), searchOption));
            }
            return filesFound.ToArray();
        } // ButtonCreateImage_Click Helper Method

        public void CreateImage()
        {
            try
            {
                Directory.CreateDirectory(TextBoxSaveLocation.Text);
            }
            catch (Exception error)
            {
                LogWriteLine(error.ToString());
                //LogWriteLine("ERROR: Could not create save location directory.");
                Console.WriteLine("\n#0#0#0\n" + error);
                return;
            }
            int targetResolutionWidth = Int32.Parse(ComboBoxResolutions.Text.Substring(0, ComboBoxResolutions.Text.IndexOf(" ")));
            int targetResolutionHeight = Int32.Parse(ComboBoxResolutions.Text.Substring(ComboBoxResolutions.Text.LastIndexOf(" ") + 1));
            Point targetResolution = new Point(targetResolutionWidth, targetResolutionHeight);
            if (userSelectionCustomResolution == true)
                targetResolution = new Point(Double.Parse(TextBoxTargetResolutionWidth.Text), Double.Parse(TextBoxTargetResolutionHeight.Text));
            //ButtonCreateImage.IsEnabled = false;
                LogWriteLine(GetTime() + "Creating background(s) made with these settings..." + "\n" +
                GetTime() + "Target Resolution " + targetResolution.X + " x " + targetResolution.Y + "\n" +
                GetTime() + "Loseless = " + userSelectionLoseslessImages + "\n" +
                GetTime() + "Percentage height of black bars " + TextBoxBlackBarPercentHeight.Text + "\n" +
                GetTime() + "Background image opacity " + TextBoxBackgroundImageOpacity.Text + "\n" +
                GetTime() + "Saved to " + TextBoxSaveLocation.Text);

            String[] files = null;

            if (userSelectionSingleOrMultiple.Equals(ButtonCreateSingle.Content.ToString())) // If Create Single Image Selected
            {
                files = new string[] { TextBoxCreatePath.Text };
            }

            else if (userSelectionSingleOrMultiple.Equals(ButtonCreateMultiple.Content.ToString())) // If Create Multiple Images Selected
            {
                String searchFolder = TextBoxCreatePath.Text;
                String[] filters = new String[] { "jpg", "jpeg", "jpe", "png", "bmp" };
                files = GetFilesFrom(searchFolder, filters, false);
            }

            int counter = 0;
            while (counter < files.Length)
            {

                try
                {
                    BitmapImage bitMapImage = new BitmapImage();
                    bitMapImage.BeginInit();
                    bitMapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitMapImage.UriSource = new Uri(files[counter]);
                    bitMapImage.EndInit();
                    ImageSource imageSource = bitMapImage;
                    string filePath = bitMapImage.UriSource.ToString().Substring(bitMapImage.UriSource.ToString().LastIndexOf(@"/") + 1);
                    //Console.WriteLine(filePath);
                    CreateNewBackground(imageSource, filePath);
                        LogWriteLine(GetTime() + " " + filePath + " created!");

                }
                catch (Exception error)
                {
                    Console.WriteLine("\n#1#1#1\n" + error);
                    LogWriteLine(GetTime() + " " + files[counter] + " could not be used!");
                }
                Dispatcher.Invoke(new Action(() => { }), DispatcherPriority.ContextIdle);
                counter++;
            }
            LogWriteLine("");
            //MainWindow1.IsEnabled = true;
            EnableDisableControls(true);
            EnableOrDisableTextBoxResolution();
        }

        public void EnableDisableControls(bool enabled)
        {
            if (enabled)
            {
                ButtonCreateImage.IsEnabled = true;
                ButtonCreateSingle.IsEnabled = true;
                ButtonCreateMultiple.IsEnabled = true;
                TextBoxCreatePath.IsReadOnly = false;
                ButtonSelectImageOrFolder.IsEnabled = true;
                ComboBoxResolutions.IsEnabled = true;
                TextBoxTargetResolutionWidth.IsEnabled = true;
                TextBoxTargetResolutionHeight.IsEnabled = true;
                ButtonCheckBoxLosslessImages.IsEnabled = true;
                SliderBlackBarPercentHeight.IsEnabled = true;
                ComboBoxBlackBarExclusion.IsEnabled = true;
                SliderBackgroundImageOpacity.IsEnabled = true;
                TextBoxSaveLocation.IsReadOnly = false;
                ButtonSelectNewImageSaveLocation.IsEnabled = true;
                ButtonOpenSaveFolder.IsEnabled = true;
                TextBoxLogs.IsEnabled = true;
            }
            else
            {
                ButtonCreateImage.IsEnabled = false;
                ButtonCreateSingle.IsEnabled = false;
                ButtonCreateMultiple.IsEnabled = false;
                TextBoxCreatePath.IsReadOnly = true;
                ButtonSelectImageOrFolder.IsEnabled = false;
                ComboBoxResolutions.IsEnabled = false;
                TextBoxTargetResolutionWidth.IsEnabled = false;
                TextBoxTargetResolutionHeight.IsEnabled = false;
                ButtonCheckBoxLosslessImages.IsEnabled = false;
                SliderBlackBarPercentHeight.IsEnabled = false;
                ComboBoxBlackBarExclusion.IsEnabled = false;
                SliderBackgroundImageOpacity.IsEnabled = false;
                TextBoxSaveLocation.IsReadOnly = true;
                ButtonSelectNewImageSaveLocation.IsEnabled = false;
                ButtonOpenSaveFolder.IsEnabled = false;
                TextBoxLogs.IsEnabled = false;
            }
        }


        private void ButtonCreateImage_Click(object sender, RoutedEventArgs e)
        {
            EnableDisableControls(false);
            CreateImage();
        }

        public string GetTime()
        {
            string date = DateTime.Now.ToString("HHmm");
            string Hr = date.Substring(0, 2);
            String Min = date.Substring(2, 2);
            int hour = Convert.ToInt32(Hr);
            string meridiem = "AM";
            if (hour > 12 && hour != 24)
            {
                hour -= 12;
                meridiem = "PM";
                Hr = " " + Convert.ToString(hour);
            }
            else if (hour == 12)
            {
                meridiem = "PM";
            }
            else if (hour == 0)
            {
                hour = 12;
                Hr = "12";
            }
            if (hour < 10)
            {
                Hr = " " + Convert.ToString(hour);

            }
            return Hr + ":" + Min + " " + meridiem + "  ";
        }

        public void LogWriteLine(string line)
        {
            TextBoxLogs.Text += line + "\n";
            TextBoxLogs.ScrollToEnd();
        }

        public void EnableOrDisableButtonCreateImage()
        {
            //Console.WriteLine(userSelectionSingleOrMultiple);
            if (userSelectionSingleOrMultiple.Equals(ButtonCreateSingle.Content.ToString())) // If Create Single Is Selected
            {
                if (File.Exists(TextBoxCreatePath.Text)) // if the file does exist, enable Create Image Button
                    ButtonCreateImage.IsEnabled = true;
                else
                    ButtonCreateImage.IsEnabled = false;
            }
            else if (userSelectionSingleOrMultiple.Equals(ButtonCreateMultiple.Content.ToString()))
            {
                if (Directory.Exists(TextBoxCreatePath.Text)) // if the folder does exist, enable Create Image Button
                    ButtonCreateImage.IsEnabled = true;
                else
                    ButtonCreateImage.IsEnabled = false;
            }

            if (TextBoxTargetResolutionWidth.Text.Equals("0") || TextBoxTargetResolutionHeight.Text.Equals("0"))
                ButtonCreateImage.IsEnabled = false;
        }

        private void TextBoxCreatePath_TextChanged(object sender, TextChangedEventArgs e)
        {
            EnableOrDisableButtonCreateImage();
        }

        private void TextBoxTargetResolution_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox) sender;
            try
            {
                int number = Int32.Parse(textBox.Text);
            }
            catch (Exception)
            {
                textBox.Text = "";
            }
            EnableOrDisableTextBoxResolution();
            EnableOrDisableButtonCreateImage();
        }

        public void EnableOrDisableTextBoxResolution()
        {
            if (TextBoxTargetResolutionWidth.Text.Length > 0 && TextBoxTargetResolutionHeight.Text.Length > 0)
            {
                ComboBoxResolutions.IsEnabled = false;
                userSelectionCustomResolution = true;
            }
            else
            {
                ComboBoxResolutions.IsEnabled = true;
            }
        }


            private void TextBoxTargetResolution_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            e.Handled = !(Regex.IsMatch(e.Text, "[0-9]"));

        }

        private void TextBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e) // Prevents copy and pasting in textBoxResolutionWidth/Height
        {
            if (e.Command == ApplicationCommands.Copy ||
                e.Command == ApplicationCommands.Cut ||
                e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }

        private void MainWindow1_Closing(object sender, System.ComponentModel.CancelEventArgs e) // Save Settings
        {
            BackgroundCreator.Properties.Settings.Default["SaveLocation"] = TextBoxSaveLocation.Text;
            BackgroundCreator.Properties.Settings.Default["CreatePath"] = TextBoxCreatePath.Text;
            BackgroundCreator.Properties.Settings.Default["Resolution"] = ComboBoxResolutions.Text;
            BackgroundCreator.Properties.Settings.Default["CustomResolution1"] = TextBoxTargetResolutionWidth.Text;
            BackgroundCreator.Properties.Settings.Default["CustomResolution2"] = TextBoxTargetResolutionHeight.Text;
            BackgroundCreator.Properties.Settings.Default["LoselessResolution"] = userSelectionLoseslessImages;
            BackgroundCreator.Properties.Settings.Default["BlackBarHeight"] = (int) SliderBlackBarPercentHeight.Value;
            BackgroundCreator.Properties.Settings.Default["Exclude"] = ComboBoxBlackBarExclusion.SelectedIndex;
            BackgroundCreator.Properties.Settings.Default["BackgroundOpacity"] = (int) SliderBackgroundImageOpacity.Value;
            BackgroundCreator.Properties.Settings.Default.Save();
        }

        private void ButtonOpenSaveFolder_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(TextBoxSaveLocation.Text))
                Process.Start(TextBoxSaveLocation.Text);
        }
    }
}