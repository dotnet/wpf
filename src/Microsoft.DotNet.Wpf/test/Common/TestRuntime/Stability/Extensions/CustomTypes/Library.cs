// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Xml;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.CustomTypes
{
    #region GenreEnum

    public enum BookGenre { Mystery, Reference, SciFi, Romance, Comic, SelfHelp }

    #endregion

    #region Book

    public class Book : INotifyPropertyChanged
    {
        #region Constructors

        public Book() { }

        public Book(string index, DeterministicRandom random) : this(Double.Parse(index), random) { }

        public Book(double index, DeterministicRandom random)
        {
            bookTitle = "Title " + index;
            bookIsbn = index.ToString();
            bookAuthor = "Author " + index;
            bookPublisher = "Publisher " + index;
            bookPrice = (Math.Floor(random.NextDouble() * 10000)) / 100;
            bookGenre = random.NextEnum<BookGenre>();
        }

        public Book(string title, string isbn, string author, string publisher, double price, BookGenre genre)
        {
            bookTitle = title;
            bookIsbn = isbn;
            bookAuthor = author;
            bookPublisher = publisher;
            bookPrice = price;
            bookGenre = genre;
        }

        #endregion

        #region Converters

        /// <summary>
        /// Create an XmlNode representation of this Book.
        /// </summary>
        /// <param name="doc">XmlDocument context to create node in.</param>
        /// <returns>Book in XmlNode format.</returns>
        public XmlNode ToXmlNode(XmlDocument doc)
        {
            XmlNode node = doc.CreateElement(this.GetType().Name);

            XmlElement property;

            property = doc.CreateElement("Title");
            property.InnerText = this.Title;
            node.AppendChild(property);

            property = doc.CreateElement("ISBN");
            property.InnerText = this.ISBN;
            node.AppendChild(property);

            property = doc.CreateElement("Publisher");
            property.InnerText = this.Publisher;
            node.AppendChild(property);

            property = doc.CreateElement("Price");
            property.InnerText = this.Price.ToString();
            node.AppendChild(property);

            property = doc.CreateElement("Author");
            property.InnerText = this.Author;
            node.AppendChild(property);

            property = doc.CreateElement("Genre");
            property.InnerText = this.Genre.ToString();
            node.AppendChild(property);

            return node;
        }

        #endregion

        #region Public Properties

        public string Title
        {
            get { return bookTitle; }
            set
            {
                bookTitle = value;
                RaisePropertyChangedEvent("Title");
            }
        }

        public string ISBN
        {
            get { return bookIsbn; }
            set
            {
                bookIsbn = value;
                RaisePropertyChangedEvent("ISBN");
            }
        }

        public string Author
        {
            get { return bookAuthor; }
            set
            {
                bookAuthor = value;
                RaisePropertyChangedEvent("Author");
            }
        }

        public string Publisher
        {
            get { return bookPublisher; }
            set
            {
                bookPublisher = value;
                RaisePropertyChangedEvent("Publisher");
            }
        }

        public BookGenre Genre
        {
            get { return bookGenre; }
            set
            {
                bookGenre = value;
                RaisePropertyChangedEvent("Genre");
            }
        }

        public double Price
        {
            get { return bookPrice; }
            set
            {
                bookPrice = value;
                RaisePropertyChangedEvent("Price");
            }
        }

        #endregion

        #region Public Overrides

        public override string ToString()
        {
            return "Book " + ISBN;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChangedEvent(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        #endregion

        #region Private Data Members

        private string bookTitle;

        private string bookIsbn;

        private string bookAuthor;

        private string bookPublisher;

        private double bookPrice;

        private BookGenre bookGenre;

        #endregion
    }

    #endregion

    #region Library ObservableCollection<Book>

    public class Library : ObservableCollection<Book>
    {
        #region Constructors

        public Library(double revision, DeterministicRandom random)
        {
            int books = random.Next(10);

            for (int i = 0; i < books; i++)
            {
                Add(new Book(i + revision, random));
            }
        }

        // Default Constructor
        public Library()
        {
        }

        #endregion

        #region Converters

        /// <summary>
        /// Create an XmlDocument representation of this Library.
        /// </summary>
        /// <returns>Library in XmlDocument format.</returns>
        public XmlDocument ToXmlDocument()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<Library></Library>");

            foreach (Book book in this.Items)
            {
                doc.DocumentElement.AppendChild(book.ToXmlNode(doc));
            }

            return doc;
        }

        /// <summary>
        /// Create a DataTable representation of this Library.
        /// </summary>
        /// <returns>Library in DataTable format.</returns>
        public DataTable ToDataTable()
        {
            DataTable library = new DataTable();
            library.TableName = "Library";
            library.Columns.Add("Title");
            library.Columns.Add("ISBN");
            library.Columns.Add("Publisher");
            library.Columns.Add("Price", typeof(double));
            library.Columns.Add("Author");
            library.Columns.Add("Genre", typeof(BookGenre));
            foreach (Book book in this.Items)
            {
                library.Rows.Add(book.Title, book.ISBN, book.Publisher, book.Price, book.Author, book.Genre);
            }

            return library;
        }

        #endregion
    }

    #endregion
}
