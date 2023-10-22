using DemoApplication.Models;
using DemoDomain.Entities;

namespace DemoApplication.Interfaces;

public interface IBookService
{
    List<Book> GetBooks();
    Book GetBook(int id);
    void AddBook(Book book);
    void UpdateBook(Book book);
    void DeleteBook(int id);
}