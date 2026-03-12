var builder = DistributedApplication.CreateBuilder(args);

var pgServer = builder.AddPostgres("pgserver")
    .WithPgWeb();

var todoDb = pgServer.AddDatabase("tododb");

builder.AddProject<Projects.ApiHost>("apihost")
    .WithReference(todoDb)
    .WaitFor(todoDb);

builder.Build().Run();
