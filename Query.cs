using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolateTimeoutRepro;

public class Book
{
    public string Title { get; set; }
}

public class Query
{
    public async Task<Book> GetBook(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(40), ct);
        return new Book { Title = "C# in depth." };
    }
}
