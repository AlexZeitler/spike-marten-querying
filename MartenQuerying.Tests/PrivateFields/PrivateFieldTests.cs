using DockerCompose;
using Marten;
using Newtonsoft.Json;
using Npgsql;
using static WaitForPostgres.Database;

namespace MartenQuerying.Tests.PrivateFields;

public class ScannedDocument
{
  public Guid Id { get; set; }
  public string PdfFilename { get; set; }
}

public class IndexedDocument
{
  public Guid Id { get; private set; }
  public string PdfFilename { get; private set; }

  [JsonConstructor]
  private IndexedDocument(Guid id, string pdfFilename)
  {
    Id = id;
    PdfFilename = pdfFilename;
  }

  public IndexedDocument(ScannedDocument document)
  {
    Id = document.Id;
    PdfFilename = document.PdfFilename;
  }
}

public class PrivateFieldTests
{
  [Fact]
  public async Task ShouldSetPrivateFieldsOnLoad()
  {
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
    var compose = new Compose(directory);

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

    using var store = DocumentStore.For(_ =>
    {
      _.Connection(connectionString);
      _.UseDefaultSerialization(nonPublicMembersStorage: NonPublicMembersStorage.NonPublicSetters);
    });

    await compose.Up();

    await WaitForConnection(connectionString);

    var scan = new ScannedDocument() { Id = Guid.NewGuid(), PdfFilename = "scan.pdf" };
    var document = new IndexedDocument(scan);

    await using var session = store.LightweightSession();
    session.Store(document);
    await session.SaveChangesAsync();

    var readDocument = await session.LoadAsync<IndexedDocument>(document.Id);
    Assert.Equal(document.PdfFilename, readDocument?.PdfFilename);
    
    await session.Connection?.CloseAsync();
    NpgsqlConnection.ClearAllPools();

    await compose.Down();
  }
}
