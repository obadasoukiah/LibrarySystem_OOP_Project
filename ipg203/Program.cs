// File: Program.cs
// Simple Library Management Console App demonstrating OOP principles required by the assignment.
// - Interface, Abstract class, Inheritance, Polymorphism
// - Encapsulation (private fields, properties, read-only values)
// - Delegates & Events (AvailabilityChanged)
// - Static classes & static members (Validator & TotalItems static property)

using System;
using System.Collections.Generic;
using System.Linq;

namespace OOP_Library_System
{
    #region Interface and EventArgs

    /// <summary>
    /// Basic interface that declares core operations for library items.
    /// (Abstraction via an interface)
    /// </summary>
    public interface ILibraryItem
    {
        void Borrow(string borrowerName);
        void Return();
        string GetInfo();
    }

    /// <summary>
    /// Custom EventArgs to pass availability change information.
    /// </summary>
    public class AvailabilityChangedEventArgs : EventArgs
    {
        public string Message { get; }
        public DateTime TimeStamp { get; }

        public AvailabilityChangedEventArgs(string message)
        {
            Message = message;
            TimeStamp = DateTime.Now;
        }
    }

    #endregion

    #region Static Validator Class (Static methods)

    /// <summary>
    /// Static utility class used for simple validation helpers.
    /// (Static class & static methods)
    /// </summary>
    public static class Validator
    {
        // Validate title: not null/empty and minimum length 3
        public static bool IsValidTitle(string title)
        {
            return !string.IsNullOrWhiteSpace(title) && title.Trim().Length >= 3;
        }

        // Very simple year validation (books didn't exist before printing press ~1440)
        public static bool IsValidYear(int year)
        {
            int current = DateTime.Now.Year;
            return year >= 1440 && year <= current;
        }

        // Simple ISBN validation: length 10 or 13 composed of digits or dashes
        public static bool IsValidISBN(string isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn)) return false;
            string cleaned = isbn.Replace("-", "").Trim();
            return (cleaned.Length == 10 || cleaned.Length == 13) && cleaned.All(char.IsDigit);
        }

        // Validate borrower name
        public static bool IsValidPersonName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && name.Trim().Length >= 2;
        }
    }

    #endregion

    #region Abstract Class: LibraryItem (implements ILibraryItem)

    /// <summary>
    /// Abstract base class that implements ILibraryItem.
    /// Demonstrates: encapsulation (private fields), properties, read-only unique ID,
    /// abstract method (CalculateLateFee) to be overridden by subclasses.
    /// Also contains delegate & event for availability changes and a static counter.
    /// </summary>
    public abstract class LibraryItem : ILibraryItem
    {
        // Private fields (encapsulation)
        private readonly Guid _id;               // read-only unique identifier
        private string _title;
        private int _year;
        private bool _isBorrowed;
        private string _borrower;
        private DateTime _dateAdded;

        // Static property to count total items created across the application
        public static int TotalItems { get; private set; } = 0;

        // Delegate and event type for availability notifications
        public delegate void AvailabilityChangedHandler(object sender, AvailabilityChangedEventArgs args);
        public event AvailabilityChangedHandler AvailabilityChanged;

        // Public read-only property exposing ID (cannot be changed after creation)
        public Guid Id => _id;

        // Title property with validation on set
        public string Title
        {
            get => _title;
            set
            {
                if (!Validator.IsValidTitle(value))
                    throw new ArgumentException("Title is invalid (min length 3).");
                _title = value.Trim();
            }
        }

        // Year property with private set (can't be changed freely)
        public int Year
        {
            get => _year;
            private set
            {
                if (!Validator.IsValidYear(value))
                    throw new ArgumentException("Year is invalid.");
                _year = value;
            }
        }

        // IsBorrowed is read-only from outside (private setter)
        public bool IsBorrowed
        {
            get => _isBorrowed;
            private set => _isBorrowed = value;
        }

        // Borrower is read-only from outside
        public string Borrower
        {
            get => _borrower;
            private set => _borrower = value;
        }

        // DateAdded is read-only (set in constructor)
        public DateTime DateAdded => _dateAdded;

        // Constructor
        protected LibraryItem(string title, int year)
        {
            _id = Guid.NewGuid();
            Title = title;
            Year = year;
            _dateAdded = DateTime.Now;
            _isBorrowed = false;
            _borrower = null;

            // increment global count of created items
            TotalItems++;
        }

        // Implement Borrow (virtual so subclasses may override if needed)
        public virtual void Borrow(string borrowerName)
        {
            if (IsBorrowed)
                throw new InvalidOperationException("Item is already borrowed.");

            if (!Validator.IsValidPersonName(borrowerName))
                throw new ArgumentException("Borrower name is invalid.");

            IsBorrowed = true;
            Borrower = borrowerName;

            // Fire availability changed event
            OnAvailabilityChanged(new AvailabilityChangedEventArgs(
                $"Item '{Title}' (ID: {Id}) borrowed by {borrowerName}."
            ));
        }

        // Implement Return (virtual)
        public virtual void Return()
        {
            if (!IsBorrowed)
                throw new InvalidOperationException("Item is not currently borrowed.");

            string previousBorrower = Borrower;
            IsBorrowed = false;
            Borrower = null;

            // Fire availability changed event
            OnAvailabilityChanged(new AvailabilityChangedEventArgs(
                $"Item '{Title}' (ID: {Id}) returned by {previousBorrower}."
            ));
        }

        // Protected method to raise events
        protected virtual void OnAvailabilityChanged(AvailabilityChangedEventArgs args)
        {
            AvailabilityChanged?.Invoke(this, args);
        }

        // Abstract method that must be implemented by subclasses
        // (Each subclass will calculate late fees differently -> Polymorphism)
        public abstract decimal CalculateLateFee(int daysLate);

        // Provide a virtual GetInfo that subclasses can extend
        public virtual string GetInfo()
        {
            string status = IsBorrowed ? $"Borrowed by {Borrower}" : "Available";
            return $"[{GetType().Name}] Title: {Title} | Year: {Year} | Status: {status} | ID: {Id}";
        }
    }

    #endregion

    #region Subclasses: Book, DVD, Magazine (Inheritance & Polymorphism)

    /// <summary>
    /// Book class inherits from LibraryItem.
    /// Overrides CalculateLateFee to use a specific formula.
    /// Shows additional properties (Author, ISBN) with encapsulation.
    /// </summary>
    public class Book : LibraryItem
    {
        // Private fields
        private string _author;
        private readonly string _isbn; // read-only after creation

        // Properties
        public string Author
        {
            get => _author;
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Author cannot be empty.");
                _author = value.Trim();
            }
        }

        // ISBN cannot change after creation (read-only property)
        public string ISBN => _isbn;

        // Constructor
        public Book(string title, int year, string author, string isbn) : base(title, year)
        {
            Author = author;
            if (!Validator.IsValidISBN(isbn))
                throw new ArgumentException("Invalid ISBN format.");
            _isbn = isbn;
        }

        // Override CalculateLateFee (different policy for books)
        public override decimal CalculateLateFee(int daysLate)
        {
            if (daysLate <= 0) return 0m;
            decimal perDay = 0.50m; // 0.50 currency units per day
            return perDay * daysLate;
        }

        // Override GetInfo to include book-specific fields
        public override string GetInfo()
        {
            return base.GetInfo() + $" | Author: {Author} | ISBN: {ISBN}";
        }
    }

    /// <summary>
    /// DVD class inherits from LibraryItem.
    /// Overrides CalculateLateFee differently (higher fee).
    /// </summary>
    public class DVD : LibraryItem
    {
        private int _durationMinutes; // duration in minutes

        public int DurationMinutes
        {
            get => _durationMinutes;
            set
            {
                if (value <= 0) throw new ArgumentException("Duration must be positive.");
                _durationMinutes = value;
            }
        }

        public DVD(string title, int year, int durationMinutes) : base(title, year)
        {
            DurationMinutes = durationMinutes;
        }

        public override decimal CalculateLateFee(int daysLate)
        {
            if (daysLate <= 0) return 0m;
            decimal perDay = 1.50m; // DVDs are more expensive to be late
            return perDay * daysLate;
        }

        public override string GetInfo()
        {
            return base.GetInfo() + $" | Duration: {DurationMinutes} min";
        }
    }

    /// <summary>
    /// Magazine class inherits from LibraryItem.
    /// Different late fee policy (lower).
    /// </summary>
    public class Magazine : LibraryItem
    {
        private int _issueNumber;

        public int IssueNumber
        {
            get => _issueNumber;
            set
            {
                if (value <= 0) throw new ArgumentException("Issue number must be positive.");
                _issueNumber = value;
            }
        }

        public Magazine(string title, int year, int issueNumber) : base(title, year)
        {
            IssueNumber = issueNumber;
        }

        public override decimal CalculateLateFee(int daysLate)
        {
            if (daysLate <= 0) return 0m;
            decimal perDay = 0.20m; // magazines cheap to be late
            return perDay * daysLate;
        }

        public override string GetInfo()
        {
            return base.GetInfo() + $" | Issue: {IssueNumber}";
        }
    }

    #endregion

    #region LibraryCatalog - holds list of LibraryItem to show polymorphism

    /// <summary>
    /// LibraryCatalog contains a list of LibraryItem objects.
    /// Demonstrates polymorphism: catalog processes items without needing to know concrete types.
    /// Also subscribes to availability changed events for contained items.
    /// </summary>
    public class LibraryCatalog
    {
        // Private list (encapsulation)
        private readonly List<LibraryItem> _items = new List<LibraryItem>();

        // Read-only access to items (returns a copy to maintain encapsulation)
        public IReadOnlyList<LibraryItem> Items => _items.AsReadOnly();

        // Static property to compute number of borrowed items (example of static member in "another" class)
        // NOTE: This is not static across instances — it's a normal property here, but requirement asked for a static property in another class;
        // we already provided LibraryItem.TotalItems above. We'll also include a computed property (not static) here to report borrowed count.
        public int BorrowedCount => _items.Count(i => i.IsBorrowed);

        // Add item and subscribe to its events
        public void AddItem(LibraryItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            _items.Add(item);
            // Subscribe to item's availability changed event
            item.AvailabilityChanged += Item_AvailabilityChanged;
        }

        // Remove item and unsubscribe
        public bool RemoveItem(Guid id)
        {
            var item = _items.FirstOrDefault(i => i.Id == id);
            if (item == null) return false;
            item.AvailabilityChanged -= Item_AvailabilityChanged;
            return _items.Remove(item);
        }

        // Find by title (simple search)
        public IEnumerable<LibraryItem> FindByTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return Enumerable.Empty<LibraryItem>();
            string t = title.Trim().ToLower();
            return _items.Where(i => i.Title.ToLower().Contains(t));
        }

        // Borrow an item by ID
        public void BorrowItemById(Guid id, string borrowerName)
        {
            var item = _items.FirstOrDefault(i => i.Id == id);
            if (item == null) throw new KeyNotFoundException("Item not found.");
            item.Borrow(borrowerName); // polymorphic behavior (Borrow implementation can be overridden)
        }

        // Return an item by ID
        public void ReturnItemById(Guid id)
        {
            var item = _items.FirstOrDefault(i => i.Id == id);
            if (item == null) throw new KeyNotFoundException("Item not found.");
            item.Return();
        }

        // Show all items (polymorphism: uses GetInfo method which subclasses override)
        public void ShowAllItems()
        {
            Console.WriteLine("=== Catalog Items ===");
            foreach (var item in _items)
            {
                Console.WriteLine(item.GetInfo());
            }
            Console.WriteLine("=====================");
        }

        // Event handler that handles events coming from items
        private void Item_AvailabilityChanged(object sender, AvailabilityChangedEventArgs args)
        {
            // For demonstration we'll just write to console (in a real app we might log to file)
            Console.WriteLine($"[EVENT] {args.TimeStamp:u} - {args.Message}");
        }
    }

    #endregion

    #region Program (Demo & usage)

    class Program
    {
        static void Main(string[] args)
        {
            // Create a catalog
            var catalog = new LibraryCatalog();

            // Create items (Book, DVD, Magazine)
            var book1 = new Book("The Pragmatic Programmer", 1999, "Andrew Hunt & David Thomas", "9780201616224");
            var dvd1 = new DVD("Inception", 2010, 148);
            var mag1 = new Magazine("National Geographic", 2021, 5);

            // Add items to catalog
            catalog.AddItem(book1);
            catalog.AddItem(dvd1);
            catalog.AddItem(mag1);

            // Show items
            catalog.ShowAllItems();

            // Show static total items counter (LibraryItem.TotalItems)
            Console.WriteLine($"Total library items created (static): {LibraryItem.TotalItems}");
            Console.WriteLine();

            // Subscribe a separate handler to one specific item's event (demonstrate delegate flexibility)
            book1.AvailabilityChanged += (sender, e) =>
            {
                Console.WriteLine($"[Custom Handler] Book event: {e.Message}");
            };

            // Borrow book1
            Console.WriteLine("Borrowing book1...");
            catalog.BorrowItemById(book1.Id, "Alice");

            // Borrow dvd1
            Console.WriteLine("Borrowing dvd1...");
            dvd1.Borrow("Bob");

            // Show status
            catalog.ShowAllItems();
            Console.WriteLine($"Borrowed count (catalog): {catalog.BorrowedCount}");
            Console.WriteLine();

            // Calculate late fees polymorphically (for 3 days late)
            Console.WriteLine("Calculating late fees for 3 days late:");
            foreach (var item in catalog.Items)
            {
                decimal fee = item.CalculateLateFee(3); // virtual/overridden in subclasses
                Console.WriteLine($"- {item.Title} ({item.GetType().Name}): {fee:C}");
            }
            Console.WriteLine();

            // Return items
            Console.WriteLine("Returning book1...");
            catalog.ReturnItemById(book1.Id);

            Console.WriteLine("Returning dvd1...");
            dvd1.Return();

            // Final state
            catalog.ShowAllItems();
            Console.WriteLine($"Borrowed count (catalog): {catalog.BorrowedCount}");
            Console.WriteLine();

            Console.WriteLine("Demo finished. Press any key to exit.");
            Console.ReadKey();
        }
    }

    #endregion
}
