using Alba;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

// File cấu hình môi trường Test "Thực Mát" (IntegrationTestFixture).
// Dành để dựng máy chủ (Host) thử nghiệm và mở một Database thử nghiệm.

namespace Tests.Infrastructure;

// [CollectionDefinition] đánh dấu đây là một nhóm các bài test (Collection).
// Tất cả các class test khai báo "[Collection(IntegrationTestCollection.Name)]" 
// sẽ dùng chung một bảng thiết lập (IntegrationTestFixture) được tạo ra MỘT lần duy nhất.
// DisableParallelization = true nghĩa là các test trong nhóm này sẽ chạy lần lượt từng cái một,
// không chạy song song, để tránh trường hợp đụng độ dữ liệu (do chung 1 Database).
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string Name = "integration-tests";
}

// IAsyncLifetime cho phép class này thực hiện tự động các việc bất đồng bộ (async): 
// - Chạy trước khi TẤT CẢ các test bắt đầu (hàm InitializeAsync)
// - Dọn dẹp sau khi TẤT CẢ các test kết thúc (hàm DisposeAsync).
public sealed class IntegrationTestFixture : IAsyncLifetime
{
    // Khởi tạo một Docker container thông qua thư viện Testcontainers.
    // Lợi ích: Mỗi khi chạy bộ test, thư viện tự động tải một Database PostgreSQL "sạch sẽ" và "thực sự ngon nghẻ" 
    // lên Docker để test (chạy hệt như môi trường thật). Không lo cấu hình môi trường lằng nhằng trên máy thật.
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("tododb")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithCleanUp(true)
        .Build();

    public IAlbaHost Host { get; private set; } = null!;
    public IDocumentStore Store => Host.Services.GetRequiredService<IDocumentStore>();
    public string ConnectionString => _postgresContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__tododb", ConnectionString);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        Host = await AlbaHost.For<Program>();

        await Store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
        await ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        if (Host is not null)
        {
            await Host.DisposeAsync();
        }

        await _postgresContainer.DisposeAsync();
    }

    // Hàm này sẽ xóa sạch dữ liệu thực tế (TRUNCATE TẤT CẢ CÁC BẢNG) trong DB.
    // Việc này sẽ được gọi THƯỜNG XUYÊN ở đầu mỗi bài test riêng lẻ 
    // nhằm đảm bảo một test case chạy sau không sử dụng nhầm "dữ liệu rác" của test case chạy trước nó.
    public async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        const string truncateSql = """
            DO $$
            DECLARE
                rec record;
            BEGIN
                FOR rec IN
                    SELECT schemaname, tablename
                    FROM pg_tables
                    WHERE schemaname NOT IN ('pg_catalog', 'information_schema')
                LOOP
                    EXECUTE format(
                        'TRUNCATE TABLE %I.%I CASCADE',
                        rec.schemaname,
                        rec.tablename
                    );
                END LOOP;
            END $$;
            """;

        await using var command = new NpgsqlCommand(truncateSql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
