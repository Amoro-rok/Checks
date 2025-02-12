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
using ZXing;
using ZXing.Presentation;
using ZXing.QrCode;
using ZXing.QrCode.Internal;
using NHunspell; 


namespace чек
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Класс для хранения данных о товаре
        public class Product
        {
            public string Name { get; set; }
            public decimal Price { get; set; }
            public decimal Discount { get; set; }
            public int Quantity { get; set; }

            // Вычисление общей цены без скидки для данного количества товара
            public decimal TotalPriceWithoutDiscount => Price * Quantity;
            // Вычисление общей скидки для данного количества товара
            public decimal TotalDiscount => TotalPriceWithoutDiscount * (Discount / 100);
            // Итоговая цена с учетом скидки
            public decimal FinalPrice => TotalPriceWithoutDiscount - TotalDiscount;

            public override string ToString()
            {
                return Discount > 0
                    ? $"{Name} x{Quantity} - {TotalPriceWithoutDiscount}₽ - Скидка: {TotalDiscount}₽ = {FinalPrice}₽"
                    : $"{Name} x{Quantity} - {FinalPrice}₽";
            }
        }

        private List<Product> products = new List<Product>(); // Список товаров
        private List<Product> cart = new List<Product>(); // Корзина

        public MainWindow()
        {
            InitializeComponent();
            LoadProducts(); // Загружаем товары при запуске
        }
        // Метод загрузки товаров из CSV-файла
        private void LoadProducts()
        {
            string filePath = "products.csv"; // Имя файла с товарами

            if (!File.Exists(filePath))
            {
                MessageBox.Show("Файл с товарами не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var lines = File.ReadAllLines(filePath).Skip(1); // Пропускаем заголовок
                SpellChecker spellChecker = new SpellChecker(); // Создаем объект для проверки орфографии

                foreach (var line in lines)
                {
                    var parts = line.Split(';');
                    // Проверка корректности данных товара
                    if (parts.Length == 3 &&
                        decimal.TryParse(parts[1], out decimal price) && price >= 0 &&
                        decimal.TryParse(parts[2], out decimal discount) && discount >= 0)
                    {
                        string productName = parts[0].Trim();

                        // Проверка орфографии названия товара
                        if (!spellChecker.CheckWord(productName))
                        {
                            MessageBox.Show($"Ошибка в названии товара: {productName}", "Ошибка загрузки", MessageBoxButton.OK, MessageBoxImage.Warning);
                            continue; // Пропускаем товар с ошибкой
                        }
                        // Добавляем товар в список
                        products.Add(new Product { Name = parts[0], Price = price, Discount = discount });
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка в данных файла: {line}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                // Заполняем ComboBox названиями товаров
                ProductComboBox.ItemsSource = products.Select(p => p.Name).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчик выбора товара в ComboBox
        private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductComboBox.SelectedItem is string selectedProduct)
            {
                var product = products.FirstOrDefault(p => p.Name == selectedProduct);
                if (product != null)
                {
                    PriceTextBlock.Text = $"{product.Price} руб.";

                    // Выводим скидку в % (если скидка есть)
                    if (product.Discount > 0)
                    {
                        DiscountTextBlock.Text = $"{product.Discount}%";
                    }
                    else
                    {
                        DiscountTextBlock.Text = "Нет скидки";
                    }

                    //количество в 1 по умолчанию
                    QuantityTextBox.Text = "1";
                }
            }
        }
        // Класс для проверки орфографии
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
        private SpellChecker spellChecker = new SpellChecker(); // Создаем объект проверки
        private void AddToCartButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductComboBox.SelectedItem is string selectedProduct)
            {
                // Проверяем орфографию названия товара
                if (!spellChecker.CheckWord(selectedProduct))
                {
                    MessageBox.Show("Название товара содержит ошибку! Проверьте ввод.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var product = products.FirstOrDefault(p => p.Name == selectedProduct);
                if (product != null)
                {
                    // Получаем количество
                    int quantity;
                    if (!int.TryParse(QuantityTextBox.Text, out quantity) || quantity <= 0)
                    {
                        MessageBox.Show("Введите корректное количество (целое число больше 0).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }


                    // Цена без скидки за 1 товар
                    decimal priceWithoutDiscount = product.Price;

                    // Вычисляем скидку (в рублях) и цену со скидкой за 1 товар
                    decimal discountAmount = (product.Discount > 0) ? (product.Price * product.Discount / 100) : 0;
                    decimal priceWithDiscount = product.Price - discountAmount;

                    // Итоговые суммы для нескольких товаров
                    decimal totalWithoutDiscount = priceWithoutDiscount * quantity;
                    decimal totalDiscountAmount = discountAmount * quantity;
                    decimal totalWithDiscount = priceWithDiscount * quantity;

                    // Формируем текст для чека
                    List<string> cartLines = new List<string>
                    {
                    "----------------------------",
                    $"Товар: {product.Name}",
                    $"Цена без скидки: {priceWithoutDiscount:F2} руб."
                    };

                    if (product.Discount > 0)
                    {
                        cartLines.Add($"Скидка: {product.Discount}%");
                        cartLines.Add($"Цена со скидкой: {priceWithDiscount:F2} руб.");
                    }

                    cartLines.Add($"Количество: {quantity}");

                    if (quantity > 1)
                    {
                        cartLines.Add($"Цена за {quantity} товаров без скидки: {totalWithoutDiscount:F2} руб.");
                        if (product.Discount > 0)
                        {
                            cartLines.Add($"Скидка за {quantity} товаров: {totalDiscountAmount:F2} руб.");
                            cartLines.Add($"Цена за {quantity} товаров со скидкой: {totalWithDiscount:F2} руб.");
                        }
                    }

                    cartLines.Add(" "); // Добавляем пустую строку для разделения товаров

                    // Добавляем текст в TextBlock
                    CartTextBlock.Text += string.Join("\n", cartLines) + "\n";

                    // Добавляем товар в корзину для дальнейших подсчетов
                    cart.Add(new Product
                    {
                        Name = product.Name,
                        Price = product.Price,
                        Discount = product.Discount,
                        Quantity = quantity
                    });

                    // Обновляем итоговые суммы
                    UpdateCartTotals();
                }
            }
        }
        // Метод конвертации System.Drawing.Bitmap в BitmapImage для WPF
        private BitmapImage BitmapToImageSource(System.Drawing.Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }
        // Обновление итоговых сумм корзины
        private void UpdateCartTotals()
        {
            // Вычисляем итоговые суммы
            decimal totalWithoutDiscount = cart.Sum(p => p.TotalPriceWithoutDiscount);
            decimal totalDiscount = cart.Sum(p => p.TotalDiscount);
            decimal finalTotal = cart.Sum(p => p.FinalPrice);

            // Обновляем текстовые блоки с итогами
            TotalWithoutDiscountTextBlock.Text = totalWithoutDiscount.ToString("F2") + " ₽";
            TotalDiscountTextBlock.Text = totalDiscount.ToString("F2") + " ₽";
            FinalPriceTextBlock.Text = finalTotal.ToString("F2") + " ₽";
        }
        // Обработчик очистки корзины
        private void ClearCartButton_Click(object sender, RoutedEventArgs e)
        {
            imgQRCode.Source = null; // Очищаем QR-код

            // Очищаем корзину
            cart.Clear();

            // Очищаем содержимое TextBlock
            CartTextBlock.Text = string.Empty;

            // Очищаем итоговые подсчеты
            TotalWithoutDiscountTextBlock.Text = "0.00 ₽";
            TotalDiscountTextBlock.Text = "0.00 ₽";
            FinalPriceTextBlock.Text = "0.00 ₽";
        }
        // Обработчик создания чека и генерации QR-кода
        private void checkButton_Click(object sender, RoutedEventArgs e)
        {
            if (cart.Count == 0)
            {
                MessageBox.Show("Корзина пуста! Добавьте товары перед созданием чека.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Берем текст из CartTextBlock, убираем переносы строк
                string receiptText = CartTextBlock.Text.Replace("\n", " ").Replace("\r", " ");

                // Ограничение длины текста (если больше 200 символов - обрезаем)
                if (receiptText.Length > 200)
                {
                    receiptText = receiptText.Substring(0, 200) + "..."; // Обрезаем, если слишком длинный
                }

                // Сохраняем чек в файл
                File.WriteAllText("receipt.txt", CartTextBlock.Text);

                // Очищаем старый QR-код перед генерацией нового
                imgQRCode.Source = null;

                // Устанавливаем настройки кодирования
                ZXing.Common.EncodingOptions options = new ZXing.Common.EncodingOptions
                {
                    Height = 300, // Увеличиваем размер QR-кода
                    Width = 300,
                    Margin = 1, // Минимальный отступ
                    PureBarcode = true
                };
                options.Hints.Add(ZXing.EncodeHintType.CHARACTER_SET, "UTF-8"); // Устанавливаем кодировку

                // Создаем QR-код
                ZXing.BarcodeWriter barcodeWriter = new ZXing.BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE,
                    Renderer = new ZXing.Rendering.BitmapRenderer(),
                    Options = options
                };

                // Генерация QR-кода
                System.Drawing.Bitmap bitmap = barcodeWriter.Write(receiptText);

                // Сохраняем QR-код в файл
                bitmap.Save("receipt.png", System.Drawing.Imaging.ImageFormat.Png);

                // Отображаем QR-код в UI
                imgQRCode.Source = BitmapToImageSource(bitmap);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании QR-кода: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
