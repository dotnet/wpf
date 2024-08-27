using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace DataGrid
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<Person> People { get; set; }
        public ObservableCollection<string> Columns { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            People = new ObservableCollection<Person>
            {
                new Person { Name = "Alice", Age = 30, Country = "USA" },
                new Person { Name = "Bob", Age = 25, Country = "UK" },
                new Person { Name = "Charlie", Age = 35, Country = "Canada" }
            };

            Columns = new ObservableCollection<string> { "Search All Columns", "Name", "Age", "Country" };

            DataContext = this;

            // Populate the ComboBox with columns
            searchColumnComboBox.ItemsSource = Columns;

            // Dynamically generate columns in the DataGrid
            GenerateDataGridColumns();
        }

        private void GenerateDataGridColumns()
        {
            dataGrid.Columns.Clear();

            foreach (var column in Columns)
            {
                if (column == "Search All Columns")
                    continue;

                var dataGridColumn = new DataGridTextColumn
                {
                    Header = column,
                    Binding = new Binding(column),
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                };

                dataGrid.Columns.Add(dataGridColumn);
            }
        }

        private void SearchComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var collectionViewSource = (CollectionViewSource)this.Resources["PeopleViewSource"];
            collectionViewSource.View.Refresh();
        }

        private void PeopleViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is Person person)
            {
                string searchText = searchColumnComboBox.Text.ToLower();
                string selectedColumn = searchColumnComboBox.SelectedItem as string;

                if (string.IsNullOrEmpty(searchText))
                {
                    e.Accepted = true;
                    return;
                }

                if (selectedColumn == "Search All Columns" || string.IsNullOrEmpty(selectedColumn))
                {
                    e.Accepted = person.Name.ToLower().Contains(searchText) ||
                                 person.Age.ToString().Contains(searchText) ||
                                 person.Country.ToLower().Contains(searchText);
                }
                else
                {
                    var propertyValue = person.GetType().GetProperty(selectedColumn)?.GetValue(person, null)?.ToString().ToLower();
                    e.Accepted = propertyValue?.Contains(searchText) ?? false;
                }
            }
        }

        private void ColumnSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var collectionViewSource = (CollectionViewSource)this.Resources["PeopleViewSource"];
            collectionViewSource.View.Refresh();
        }
    }

    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string Country { get; set; }
    }

    public class HighlightSearchTextConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string text = values[0]?.ToString();
            string searchText = values[1]?.ToString().ToLower();

            if (string.IsNullOrEmpty(searchText) || string.IsNullOrEmpty(text))
                return new Run(text);

            int index = text.ToLower().IndexOf(searchText);
            if (index == -1)
                return new Run(text);

            // Create a new TextBlock with the highlighted search text
            var textBlock = new TextBlock();

            // Add text before the search term
            if (index > 0)
                textBlock.Inlines.Add(new Run(text.Substring(0, index)));

            // Add the highlighted search term
            var highlight = new Run(text.Substring(index, searchText.Length))
            {
                Background = Brushes.Yellow,
                Foreground = Brushes.Black
            };
            textBlock.Inlines.Add(highlight);

            // Add text after the search term
            int remainingLength = text.Length - (index + searchText.Length);
            if (remainingLength > 0)
                textBlock.Inlines.Add(new Run(text.Substring(index + searchText.Length)));

            return textBlock;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
