using DemoDomain.Entities;
using DemoDomain.Interfaces;

namespace DemoInfrastructure.Data;

public class BookRepository : IBookRepository
{
    private readonly List<Book> _books = new List<Book>
    {
        new Book { Id = 1, Title = "Clean Code", Author = "Robert C. Martin", Price = 30.00m },
        new Book { Id = 2, Title = "Code Complete", Author = "Steve McConnell", Price = 25.00m },
        new Book { Id = 3, Title = "Refactoring", Author = "Martin Fowler", Price = 27.00m }
    };

    public List<Book> GetBooks()
    {
        return _books;
    }

    public Book GetBook(int id)
    {
        return _books.FirstOrDefault(b => b.Id == id);
    }
    public void AddBook(Book book)
    {
        _books.Add(book);
    }

    public void UpdateBook(Book book)
    {
        var existingBook = _books.FirstOrDefault(b => b.Id == book.Id);
        if (existingBook != null)
        {
            existingBook.Title = book.Title;
            existingBook.Author = book.Author;
            existingBook.Price = book.Price;
        }
    }

    public void DeleteBook(int id)
    {
        var existingBook = _books.FirstOrDefault(b => b.Id == id);
        if (existingBook != null)
        {
            _books.Remove(existingBook);
        }
    }
}