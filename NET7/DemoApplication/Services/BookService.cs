using AutoMapper;
using DemoApplication.Interfaces;
using DemoDomain.Entities;
using DemoDomain.Interfaces;

namespace DemoApplication.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;
    private readonly IMapper _mapper;

    public BookService(IBookRepository bookRepository, IMapper mapper)
    {
        _bookRepository = bookRepository;
        _mapper = mapper;
    }

    public List<Book> GetBooks()
    {
        var books = _bookRepository.GetBooks();
        return _mapper.Map<List<Book>>(books);
    }

    public Book GetBook(int id)
    {
        var book = _bookRepository.GetBook(id);
        return _mapper.Map<Book>(book);
    }

    public void AddBook(Book book)
    {
        var bookEntity = _mapper.Map<Book>(book);
        _bookRepository.AddBook(bookEntity);
    }

    public void UpdateBook(Book book)
    {
        var bookEntity = _mapper.Map<Book>(book);
        _bookRepository.UpdateBook(bookEntity);
    }

    public void DeleteBook(int id)
    {
        _bookRepository.DeleteBook(id);
    }
}