using DockerCompose;
using Marten;
using Npgsql;
using WaitForPostgres;

namespace MartenQuerying.Tests.Embbeding;

public class ScannedDocument
{
  public ScannedDocument()
  {
    ScannedPages = new List<ScannedPage>();
  }

  public Guid Id { get; set; }
  public string? PdfFilename { get; set; }
  public List<ScannedPage> ScannedPages { get; set; }
}

public class ScannedPage
{
  public Guid Id { get; set; }
  public string? Text { get; set; }
}

public class UnitTest1
{
  [Fact]
  public async Task ShouldQueryDocuments()
  {
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
    var compose = new Compose(directory);
    await compose.Up();

    var connectionString = new NpgsqlConnectionStringBuilder()
    {
      Pooling = false,
      Port = 5432,
      Host = "localhost",
      CommandTimeout = 20,
      Database = "marten",
      Password = "123456",
      Username = "marten"
    }.ToString();

    await Database.WaitForConnection(connectionString);

    using var store = DocumentStore.For(connectionString);
    await using var session = store.LightweightSession();

    var doc = new ScannedDocument()
    {
      PdfFilename = "Some file"
    };

    session.Store(doc);
    await session.SaveChangesAsync().ConfigureAwait(false);
    Assert.NotEqual(Guid.Empty, doc.Id);

    var documents = session.Query<ScannedDocument>().Where(d => d.PdfFilename.Contains("file")).ToList();
    Assert.Equal("Some file", documents.First().PdfFilename);

    await session.Connection?.CloseAsync();
    NpgsqlConnection.ClearAllPools();
    
    
    await compose.Down();
  }

  [Fact]
  public async Task ShouldQueryEmbeddedDocuments()
  {
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
    var compose = new Compose(directory);
    await compose.Up();

    var connectionString = new NpgsqlConnectionStringBuilder()
    {
      Pooling = false,
      Port = 5432,
      Host = "localhost",
      CommandTimeout = 20,
      Database = "marten",
      Password = "123456",
      Username = "marten"
    }.ToString();

    await Database.WaitForConnection(connectionString);

    using var store = DocumentStore.For(connectionString);
    await using var session = store.LightweightSession();

    var doc = new ScannedDocument()
    {
      PdfFilename = "Some file",
      ScannedPages = new List<ScannedPage>()
      {
        new ScannedPage()
        {
          Text = "My Text"
        }
      }
    };

    session.Store(doc);
    await session.SaveChangesAsync().ConfigureAwait(false);

    var documents =
      session.Query<ScannedDocument>().Where(d => Enumerable.Any<ScannedPage>(d.ScannedPages, p => p.Text.Contains("Text")));

    Assert.Equal(1, documents.Count());

    await session.Connection?.CloseAsync();
    NpgsqlConnection.ClearAllPools();
    
    await compose.Down();
  }
}
