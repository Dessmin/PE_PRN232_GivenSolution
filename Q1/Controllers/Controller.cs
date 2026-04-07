using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Q1.Models;

namespace Q1.Controllers
{
    [Route("api")]
    [ApiController]
    public class Controller : ControllerBase
    {
        private readonly PE_PRN_26SP_11Context _context;

        public Controller(PE_PRN_26SP_11Context context)
        {
            _context = context;
        }

        // De Count

        [HttpGet("sections")]
        public IActionResult GetSections()
        {
            var entities =  _context.ClassSections
                .Include(e => e.Course)
                .Include(e => e.Enrollments)
                .ToList();

            var list = entities.Select(e => new ClassSectionDTO
                {
                    SectionId = e.SectionId,
                    CourseName = e.Course.CourseName,
                    RoomNumber = e.RoomNumber,
                    MaxCapacity = e.MaxCapacity,
                    EnrolledCount = e.Enrollments.Count,
                });

            return Ok(list);
        }

        [HttpGet("sections/search")]
        public IActionResult GetPaginSections([FromQuery] string semester, [FromQuery] bool isFull, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {

            if (page <= 0 || pageSize <= 0) return BadRequest();

            var entities = _context.ClassSections.AsQueryable();

            if (!string.IsNullOrWhiteSpace(semester)) entities = entities.Where(e => e.Semester == semester);

            if (isFull) entities = entities.Where(e => e.Enrollments.Count >= e.MaxCapacity);
            else entities = entities.Where(e => e.Enrollments.Count < e.MaxCapacity);

            var totalCount =  entities.Count();

            var list = entities
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new ClassSectionDTO
                {
                    SectionId = e.SectionId,
                    CourseName = e.Course.CourseName,
                    RoomNumber = e.RoomNumber,
                    MaxCapacity = e.MaxCapacity,
                    EnrolledCount = e.Enrollments.Count,
                })
                .ToList();

            return Ok(new Pagination<ClassSectionDTO>(list, totalCount, page, pageSize));
        }

        [HttpPut("sections/{sectionsId}")]
        public IActionResult PutSections(int sectionsId, [FromBody] RequestPut input)
        {
            var entity = _context.ClassSections
                .Include(e => e.Enrollments)
                .Include(e => e.Course)
                .FirstOrDefault(e => e.SectionId == sectionsId);

            if (entity == null) return NotFound();

            if (input.MaxCapacity < entity.Enrollments.Count) 
                return BadRequest( new BadResponse
                {
                    Message = "Max capacity cannot be less than current enrolled students",
                    CurrentEnrolled = entity.Enrollments.Count,
                    RequestedCapacity = input.MaxCapacity
                });

            entity.RoomNumber = input.RoomNumber;
            entity.MaxCapacity = input.MaxCapacity;

            _context.ClassSections.Update(entity);
            _context.SaveChanges();

            var availableSeats = entity.MaxCapacity - entity.Enrollments.Count;

            return Ok( new PutSectionDTO
            {
                Message = "Section update successfully",
                SectionId = entity.SectionId,
                CourseId = entity.CourseId,
                CourseName = entity.Course.CourseName,
                RoomNumber= entity.RoomNumber,
                MaxCapacity= entity.MaxCapacity,
                EnrolledCount = entity.Enrollments.Count,
                AvailableSeats = (int)availableSeats
            });
        }

        [HttpPost("enrollments")]
        public IActionResult PostEnrollments([FromBody] RequestPost input)
        {
            var section = _context.ClassSections
                .Include(e => e.Enrollments)
                .FirstOrDefault(e => e.SectionId == input.SectionId);

            var student = _context.Students.FirstOrDefault(s => s.StudentId == input.StudentId);

            if (student == null || section == null) return NotFound();

            if (section.Enrollments.Count >= section.MaxCapacity) return BadRequest();

            var enrollment = new Enrollment
            {
                StudentId = input.StudentId,
                SectionId = input.SectionId,
                RegistrationDate = DateOnly.FromDateTime(DateTime.Now)
            };

            _context.Enrollments.Add(enrollment);
            _context.SaveChanges();

            return Ok( new ResponsePost
            {
                EnrollmentId = enrollment.EnrollmentId,
                StudentId = enrollment.StudentId,
                SectionId = enrollment.SectionId,
                RegistrationDate = enrollment.RegistrationDate
            });
        }

        // De Avg

        [HttpGet("students")]
        public IActionResult GetStudents()
        {
            var entities = _context.Students
                .Include(s => s.Enrollments)
                .ToList();

            var list = entities.Select(entities => new StudentDTO
            {
                StudentId = entities.StudentId,
                StudentName = entities.StudentName,
                Email = entities.Email,
                gpa = entities.Enrollments.Where(e => e.Grade.HasValue).Average(e => e.Grade.Value)
            });

            return Ok(list);
        }

        [HttpGet("students-performance")]
        public IActionResult GetStudentsPagination([FromQuery] int? minGpa, [FromQuery] string? studentName, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page <= 0 || pageSize <= 0) return BadRequest();

            var entities = _context.Students
                .Include(s => s.Enrollments)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(studentName)) 
                entities = entities.Where(e => e.StudentName.Contains(studentName));

            var list = new List<StudentDTO>();

            foreach (var entity in entities)
            {
                var gpa = entity.Enrollments.Where(e => e.Grade.HasValue).Average(e => e.Grade.Value);

                if (minGpa.HasValue && gpa < minGpa) continue;

                list.Add(new StudentDTO
                {
                    StudentId = entity.StudentId,
                    StudentName = entity.StudentName,
                    Email = entity.Email,
                    gpa = gpa
                });
            }

            list = list
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new Pagination<StudentDTO>(list, list.Count, page, pageSize));
        }

        [HttpPut("enrollments/{enrollmentId}/grade")]
        public IActionResult PutEnrollmentGrade(int enrollmentId, [FromBody] float grade)
        {
            if (grade < 0 || grade > 10) return BadRequest("Grade must be between 0 and 10");

            var enrollment = _context.Enrollments.Include(e =>e.Student).FirstOrDefault(e => e.EnrollmentId == enrollmentId);

            if (enrollment == null) return NotFound();

            enrollment.Grade = grade;

            _context.Enrollments.Update(enrollment);
            _context.SaveChanges();

            return Ok(new GradeUpdateDTO
            {
                EnrollmentId = enrollment.EnrollmentId,
                StudentId = enrollment.StudentId,
                Grade = enrollment.Grade
            });
        }

        [HttpDelete("enrollments/{enrollmentId}")]
        public IActionResult DeleteEnrollment(int enrollmentId)
        {
            var enrollment = _context.Enrollments.FirstOrDefault(e => e.EnrollmentId == enrollmentId);

            if (enrollment == null) return NotFound("No enrollment found with provided EnrollmentId");

            if (enrollment.Grade.HasValue) return BadRequest("Cannot cancel an enrollment that has been graded");

            _context.Enrollments.Remove(enrollment);
            _context.SaveChanges();

            return NoContent();
        }

        // DTOs

        // Dto de Count

        public class ResponsePost
        {
            public int EnrollmentId { get; set; }
            public int? StudentId { get; set; }
            public int? SectionId { get; set; }
            public DateOnly? RegistrationDate { get; set; }
        }

        public class RequestPost
        {
            public int StudentId { get; set; }
            public int SectionId { get; set; }
        }

        public class RequestPut
        {
            public required string RoomNumber { get; set; }
            public int MaxCapacity { get; set; }

        }

        public class BadResponse
        {
            public string? Message { get; set; }
            public int CurrentEnrolled { get; set; }
            public int RequestedCapacity { get; set; }
        }

        public class PutSectionDTO
        {
            public string? Message { get; set; }
            public int SectionId { get; set; }
            public int? CourseId { get; set; }
            public string CourseName { get; set; }
            public string RoomNumber { get; set; }
            public int MaxCapacity { get; set; }
            public int EnrolledCount { get; set; }
            public int AvailableSeats { get; set; }
        }

        public class ClassSectionDTO
        {
            public int SectionId { get; set; }
            public string CourseName { get; set; }
            public string RoomNumber { get; set; }
            public int? MaxCapacity { get; set; }
            public int EnrolledCount { get; set; }
        }

        // Dto de Avg

        public class StudentDTO
        {
            public int StudentId { get; set; }
            public string StudentName { get; set; }
            public string Email { get; set; }
            public double gpa { get; set; }
        }

        public class GradeUpdateDTO
        {
            public int EnrollmentId { get; set; }
            public int? StudentId { get; set; }
            public double? Grade { get; set; }
        }

        // Pagination class
        public class Pagination<T>
        {
            public Pagination(List<T> items, int count, int pageNumber, int pageSize)
            {
                TotalCount = count;
                PageSize = pageSize;
                CurrentPage = pageNumber;
                TotalPages = (int)Math.Ceiling(count / (double)pageSize);
                Items = items;
            }

            public List<T> Items { get; }
            public int CurrentPage { get; }
            public int TotalPages { get; }
            public int PageSize { get; private set; }
            public int TotalCount { get; private set; }

            //public bool HasPrevious => CurrentPage > 1;
            //public bool HasNext => CurrentPage < TotalPages;
        }
    }
}
