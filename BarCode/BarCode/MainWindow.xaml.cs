using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using OfficeOpenXml;


namespace BarCode
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string[] catalog = new string[5];

        private string[] Load_catalog()
        {
            var filepath = "/Users/roman/Desktop/CSH/BarCode/BarCode/Resources/catalog.xlsx";
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            FileInfo fileInfo = new FileInfo(filepath);
            using (ExcelPackage package = new ExcelPackage(fileInfo))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension.End.Row;
                for (int row = 2; row < (rowCount + 1); row++)
                {
                    for (int col = 1; col < 4; col++)
                    {
                        catalog[row - 2] += worksheet.Cells[row, col].Value;
                        if (col != 3) { catalog[row - 2] += "-"; }
                    }
                }
            }
            return catalog;
        }

        public MainWindow()
        {
            InitializeComponent();
            catalog = Load_catalog();
        }

        

        private void GenerateQRCodeButton_Click(object sender, RoutedEventArgs e)
        {
            string inputText = TextBoxInput.Text;

            if (string.IsNullOrEmpty(inputText))
            {
                MessageBox.Show("Ошибка при создании QR-кода. Отсутствуют товары в чеке", "Ошибка");
                return;
            }

            try
            {
                // Используем QRCoder для генерации QR-кода.
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(inputText, QRCodeGenerator.ECCLevel.Q); // ECCLevel определяет уровень коррекции ошибок
                QRCode qrCode = new QRCode(qrCodeData);
                Bitmap qrCodeImage = qrCode.GetGraphic(20); // GetGraphic(pixelsPerModule) - определяет размер каждой "точки" QR-кода.

                // Преобразуем Bitmap в BitmapImage для отображения в WPF Image.
                BitmapImage bitmapImage = BitmapToImageSource(qrCodeImage);
                QRCodeImage.Source = bitmapImage;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при генерации QR-кода: {ex.Message}", "Ошибка");
            }
        }

        // Вспомогательный метод для преобразования Bitmap в BitmapImage
        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

        
    }
}