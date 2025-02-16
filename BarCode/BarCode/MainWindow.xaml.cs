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
using NHunspell;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Vml;


namespace BarCode
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> catalog = new List<string>();
        private List<List<decimal>> cart = new List<List<decimal>>();

        public class SpellChecker
        {
            private Hunspell hunspell;

            public SpellChecker()
            {
                // Подключаем словарь 
                hunspell = new Hunspell("ru_RU.aff", "ru_RU.dic");
            }
            // Метод для проверки орфографии слова
            public bool CheckWord(string word)
            {
                return hunspell.Spell(word); // Проверяем, есть ли слово в словаре
            }
        }
        private SpellChecker spellChecker = new SpellChecker();

        private void Load_catalog()
        {
            var filepath = "/Users/roman/Desktop/CSH/BarCode/BarCode/Resources/catalog.xlsx";
            if (!File.Exists(filepath))
            {
                MessageBox.Show("Файл с товарами не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                FileInfo fileInfo = new FileInfo(filepath);
                using (ExcelPackage package = new ExcelPackage(fileInfo))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension.End.Row;
                    for (int row = 2; row < (rowCount + 1); row++)
                    {
                        string potential_product = "";
                        for (int col = 1; col < 4; col++)
                        {
                            potential_product += worksheet.Cells[row, col].Value;
                            if (col != 3) { potential_product += "-"; }
                        }
                        var exam_prod = potential_product.Split('-');
                        if (exam_prod.Length == 3 &&
                        decimal.TryParse(exam_prod[1], out decimal price) && price >= 0 &&
                        decimal.TryParse(exam_prod[2], out decimal discount) && discount >= 0 && discount <= 99)
                        {
                            if (!spellChecker.CheckWord(exam_prod[0].Trim()))
                            {
                                MessageBox.Show($"Ошибка в названии товара: {exam_prod[0].Trim()}", "Ошибка загрузки", MessageBoxButton.OK, MessageBoxImage.Warning);
                                continue;
                            }
                            catalog.Add(potential_product);
                            ProductComboBox.Items.Add(exam_prod[0]);
                        }
                        else
                        {
                            MessageBox.Show("Ошибка в данных файла!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            Load_catalog();
        }

        private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductComboBox.SelectedItem is string selectedProduct)
            {
                string[] product = catalog.FirstOrDefault(p => selectedProduct.Equals(p.Split('-')[0])).Split('-');

                if (product != null)
                {
                    PriceTextBlock.Text = $"{product[1]} руб.";

                    if (int.Parse(product[2]) > 0)
                    {
                        DiscountTextBlock.Text = $"{product[2]}%";
                    }
                    else
                    {
                        DiscountTextBlock.Text = "Нет скидки";
                    }

                    QuantityTextBox.Text = "1";
                }
            }
        }
        private void UpdateCartTotals()
        {
            // Вычисляем итоговые суммы
            decimal totalWithoutDiscount = cart.Sum(p => p[0]);
            decimal totalDiscount = cart.Sum(p => p[1]);
            decimal finalTotal = cart.Sum(p => p[2]);

            // Обновляем текстовые блоки с итогами
            TotalWithoutDiscountTextBlock.Text = totalWithoutDiscount.ToString("F2") + " ₽";
            TotalDiscountTextBlock.Text = totalDiscount.ToString("F2") + " ₽";
            FinalPriceTextBlock.Text = finalTotal.ToString("F2") + " ₽";
        }

        private void ClearCartButton_Click(object sender, RoutedEventArgs e)
        {
            QRCodeImage.Source = null; // Очищаем QR-код

            // Очищаем корзину
            cart.Clear();

            // Очищаем содержимое TextBlock
            TextBlockInput.Text = string.Empty;

            // Очищаем итоговые подсчеты
            TotalWithoutDiscountTextBlock.Text = "0.00 ₽";
            TotalDiscountTextBlock.Text = "0.00 ₽";
            FinalPriceTextBlock.Text = "0.00 ₽";

            File.WriteAllText("check.txt", TextBlockInput.Text);
        }

        private void GenerateQrCode(string inputText)
        {
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

        private void GenerateQRCodeButton_Click(object sender, RoutedEventArgs e)
        {
            GenerateQrCode(TextBlockInput.Text);
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

        private void createCheck(object sender, EventArgs e)
        {
            if (cart.Count == 0)
            {
                MessageBox.Show("Корзина пуста! Добавьте товары перед созданием чека.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                string final_txt = TextBlockInput.Text.Replace("\n", " ").Replace("\r", " ");

                File.WriteAllText("check.txt", TextBlockInput.Text);

                GenerateQrCode(TextBlockInput.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании QR-кода: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddToCartButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductComboBox.SelectedItem is string selectedProduct)
            {
                string[] product = catalog.FirstOrDefault(p => selectedProduct.Equals(p.Split('-')[0])).Split('-');
                if (product != null)
                {
                    int quantity;
                    if (!int.TryParse(QuantityTextBox.Text, out quantity) || quantity <= 0)
                    {
                        MessageBox.Show("Введите корректное количество (целое число больше 0).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    decimal priceWoutDisc = decimal.Parse(product[1]);
                    decimal discAmount = (priceWoutDisc > 0) ? (priceWoutDisc * int.Parse(product[2]) / 100) : 0;
                    decimal priceWdisc = priceWoutDisc - discAmount;

                    decimal ttlWoutDisc = priceWoutDisc * quantity;
                    decimal ttlDiscAmount = discAmount * quantity;
                    decimal ttlWdisc = priceWdisc * quantity;

                    List<string> cartText = new List<string>
                    {
                        "----------------------------",
                        $"Товар:.........{product[0]}",
                        $"Количество:....{quantity}",
                        $"Общая цена товара:...{ttlWoutDisc:F2}"
                    };
                    if (discAmount > 0)
                    {

                        cartText.Add($"Скидка........{ttlDiscAmount}");
                        cartText.Add($"Итог..........{ttlWdisc:F2}");
                    }
                    cartText.Add(" ");

                    TextBlockInput.Text += string.Join("\n", cartText) + "\n";
                    cart.Add(new List<decimal> { ttlWoutDisc, ttlDiscAmount, ttlWdisc });

                    UpdateCartTotals();
                }
            }
        }
    }
}