using Q2;

var builder = WebApplication.CreateBuilder(args);

//Initialize UrlUtilities with configuration
//DO NOT change this code
Utilities.Initialize(builder.Configuration);
//End

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
