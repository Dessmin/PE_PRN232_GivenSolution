using Q2;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

//Initialize UrlUtilities with configuration
//DO NOT change this code
Utilities.Initialize(builder.Configuration);
//End

var app = builder.Build();

app.UseRouting();

app.MapGet("/", () => Results.Redirect("/Instructor"));

app.MapControllers();

app.Run();
