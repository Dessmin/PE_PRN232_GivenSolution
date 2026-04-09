using Microsoft.AspNetCore.Mvc;

namespace Q2;

[Route("Instructor")]
public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _baseUrl;

    public HomeController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _baseUrl = configuration["GivenAPIBaseUrl"]
            ?? throw new InvalidOperationException("GivenAPIBaseUrl is not configured in appsettings.json");
    }

    [HttpGet("")]
    public async Task<IActionResult> List([FromQuery] string? name, [FromQuery] string? expertise)
    {
        var client = _httpClientFactory.CreateClient();

        var url = $"{_baseUrl}/api/instructors/search?name=" + name + "&expertise=" + expertise;

        var response = await client.GetAsync(url);

        var instructors = await response.Content.ReadFromJsonAsync<List<Instructor>>();

        return View("~/Instructor/list.cshtml", instructors);
    }

    [HttpGet("{InstructorId}")]
    public async Task<IActionResult> Details(int InstructorId)
    {
        var client = _httpClientFactory.CreateClient();

        var url = $"{_baseUrl}/api/instructors/{InstructorId}";

        var response = await client.GetAsync(url);

        var instructor = await response.Content.ReadFromJsonAsync<InstructorDetails>();

        return View("~/Instructor/detail.cshtml", instructor);
    }

    public class Instructor
    {
        public int InstructorId { get; set; }

        public string FullName { get; set; }

        public string Expertise { get; set; }

        public DateTime? HireDate { get; set; }

        public int TotalCourses { get; set; }
    }

    public class Course
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; }
        public int Credits { get; set; }
    }

    public class InstructorDetails 
    {
        public int InstructorId { get; set; }
        public string FullName { get; set; }
        public string Expertise { get; set; }
        public List<Course> Courses { get; set; } = new List<Course>();
    }
}
