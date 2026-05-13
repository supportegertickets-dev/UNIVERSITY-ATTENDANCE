// ── AuthController.cs ────────────────────────────────────
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AttendanceAPI.Models;
using AttendanceAPI.Services;
using MongoDB.Driver;

namespace AttendanceAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(MongoDbService db, IConfiguration config) : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok", message = "API is running" });

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        var lecturer = db.Lecturers.Find(l => l.Username == req.Username).FirstOrDefault();
        if (lecturer == null || !BCrypt.Net.BCrypt.Verify(req.Password, lecturer.PasswordHash))
            return Unauthorized(new { message = "Invalid username or password." });

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, lecturer.Id!),
            new Claim(ClaimTypes.Name, lecturer.Username),
            new Claim(ClaimTypes.Role, lecturer.Role),
        };
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"], audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(double.Parse(config["Jwt:ExpiresInHours"]!)),
            signingCredentials: creds
        );
        return Ok(new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            lecturer.Name, lecturer.Role, lecturer.Title));
    }
}

// ── SessionsController.cs ────────────────────────────────

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionsController(MongoDbService db) : ControllerBase
{
    // Open a new attendance session
    [HttpPost("open")]
    public IActionResult Open([FromBody] OpenSessionRequest req)
    {
        db.ExpireOldSessions();
        var lecturerId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var lecturerName = User.FindFirstValue(ClaimTypes.Name)!;

        // Prevent duplicate open session for same course today
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var duplicate = db.Sessions.Find(s =>
            s.CourseCode == req.CourseCode &&
            s.Date       == today &&
            s.Status     == "open").FirstOrDefault();
        if (duplicate != null)
            return Conflict(new { message = $"A session for {req.CourseCode} is already open.", session = duplicate });

        var lecturer = db.Lecturers.Find(l => l.Id == lecturerId).FirstOrDefault();
        var session = new AttendanceSession {
            CourseCode   = req.CourseCode,
            CourseName   = req.CourseName,
            Department   = req.Department,
            Year         = req.Year,
            Room         = req.Room,
            LecturerId   = lecturerId,
            LecturerName = (lecturer?.Title + " " + lecturer?.Name).Trim(),
            DurationMins = req.DurationMins,
            OpenedAt     = DateTime.UtcNow,
            AutoCloseAt  = DateTime.UtcNow.AddMinutes(req.DurationMins),
            Status       = "open",
            Date         = today
        };
        db.Sessions.InsertOne(session);
        return Ok(session);
    }

    // Close session manually
    [HttpPost("{id}/close")]
    public IActionResult Close(string id)
    {
        var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var session    = db.Sessions.Find(s => s.Id == id).FirstOrDefault();
        if (session == null) return NotFound();

        // Only the lecturer who opened it (or admin) can close
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (session.LecturerId != lecturerId && role != "admin")
            return Forbid();

        var update = Builders<AttendanceSession>.Update
            .Set(s => s.Status,   "closed")
            .Set(s => s.ClosedAt, DateTime.UtcNow);
        db.Sessions.UpdateOne(s => s.Id == id, update);
        session.Status   = "closed";
        session.ClosedAt = DateTime.UtcNow;
        return Ok(session);
    }

    // Extend session duration
    [HttpPost("{id}/extend")]
    public IActionResult Extend(string id, [FromQuery] int minutes = 10)
    {
        var session = db.Sessions.Find(s => s.Id == id).FirstOrDefault();
        if (session == null) return NotFound();
        var newClose = (session.AutoCloseAt < DateTime.UtcNow ? DateTime.UtcNow : session.AutoCloseAt)
                       .AddMinutes(minutes);
        var update = Builders<AttendanceSession>.Update
            .Set(s => s.AutoCloseAt, newClose)
            .Set(s => s.Status,      "open")
            .Inc(s => s.DurationMins, minutes);
        db.Sessions.UpdateOne(s => s.Id == id, update);
        return Ok(new { message = $"Session extended by {minutes} min.", autoCloseAt = newClose });
    }

    // Get active sessions
    [HttpGet("active")]
    public IActionResult Active()
    {
        db.ExpireOldSessions();
        var sessions = db.Sessions.Find(s => s.Status == "open")
                                  .SortByDescending(s => s.OpenedAt).ToList();
        // Attach seconds remaining
        var result = sessions.Select(s => new {
            s.Id, s.CourseCode, s.CourseName, s.Department, s.Year,
            s.Room, s.LecturerName, s.OpenedAt, s.AutoCloseAt,
            s.Status, s.ScansCount, s.Date,
            secondsLeft = Math.Max(0, (int)(s.AutoCloseAt - DateTime.UtcNow).TotalSeconds)
        });
        return Ok(result);
    }

    // Get all sessions (history)
    [HttpGet]
    public IActionResult GetAll([FromQuery] string? date, [FromQuery] string? status)
    {
        db.ExpireOldSessions();
        var filter = Builders<AttendanceSession>.Filter.Empty;
        if (!string.IsNullOrEmpty(date))   filter &= Builders<AttendanceSession>.Filter.Eq(s => s.Date, date);
        if (!string.IsNullOrEmpty(status)) filter &= Builders<AttendanceSession>.Filter.Eq(s => s.Status, status);
        return Ok(db.Sessions.Find(filter).SortByDescending(s => s.OpenedAt).ToList());
    }
}

// ── StudentsController.cs ────────────────────────────────

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentsController(MongoDbService db) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll() => Ok(db.Students.Find(_ => true).ToList());

    [HttpGet("{regNo}")]
    public IActionResult Get(string regNo)
    {
        var s = db.Students.Find(x => x.RegNo == regNo).FirstOrDefault();
        return s == null ? NotFound() : Ok(s);
    }

    [HttpGet("by-card/{uid}")]
    public IActionResult GetByCard(string uid)
    {
        var s = db.Students.Find(x => x.CardUid == uid).FirstOrDefault();
        return s == null ? NotFound(new { message = "No student enrolled for this card." }) : Ok(s);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public IActionResult Create([FromBody] Student student)
    {
        if (db.Students.Find(s => s.RegNo == student.RegNo).Any())
            return Conflict(new { message = "Registration number already exists." });
        db.Students.InsertOne(student);
        return CreatedAtAction(nameof(Get), new { regNo = student.RegNo }, student);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public IActionResult Update(string id, [FromBody] Student updated)
    {
        var res = db.Students.ReplaceOne(s => s.Id == id, updated);
        return res.MatchedCount == 0 ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public IActionResult Delete(string id)
    {
        var res = db.Students.DeleteOne(s => s.Id == id);
        return res.DeletedCount == 0 ? NotFound() : Ok(new { message = "Deleted." });
    }

    [HttpPost("enroll-card")]
    [Authorize(Roles = "admin")]
    public IActionResult EnrollCard([FromBody] EnrollCardRequest req)
    {
        var existing = db.Students.Find(s => s.CardUid == req.CardUid).FirstOrDefault();
        if (existing != null && existing.Id != req.StudentId)
            return Conflict(new { message = $"Card already enrolled to {existing.Name}." });
        var update = Builders<Student>.Update
            .Set(s => s.CardUid, req.CardUid)
            .Set(s => s.CardEnrolledAt, DateTime.UtcNow);
        var res = db.Students.UpdateOne(s => s.Id == req.StudentId, update);
        if (res.MatchedCount == 0) return NotFound(new { message = "Student not found." });
        var student = db.Students.Find(s => s.Id == req.StudentId).FirstOrDefault();
        return Ok(new { message = $"Card enrolled for {student?.Name}.", student });
    }

    [HttpDelete("{id}/card")]
    [Authorize(Roles = "admin")]
    public IActionResult RemoveCard(string id)
    {
        var update = Builders<Student>.Update.Unset(s => s.CardUid).Unset(s => s.CardEnrolledAt);
        var res = db.Students.UpdateOne(s => s.Id == id, update);
        return res.MatchedCount == 0 ? NotFound() : Ok(new { message = "Card removed." });
    }
}

// ── LecturersController.cs ───────────────────────────────

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class LecturersController(MongoDbService db) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll()
    {
        var list = db.Lecturers.Find(_ => true).ToList()
            .Select(l => new { l.Id, l.Username, l.Name, l.Title, l.Department, l.Role });
        return Ok(list);
    }

    [HttpPost]
    public IActionResult Create([FromBody] Lecturer lecturer)
    {
        if (db.Lecturers.Find(l => l.Username == lecturer.Username).Any())
            return Conflict(new { message = "Username already exists." });
        lecturer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(lecturer.PasswordHash);
        db.Lecturers.InsertOne(lecturer);
        return Ok(new { lecturer.Id, lecturer.Username, lecturer.Name, lecturer.Role });
    }

    [HttpPut("{id}")]
    public IActionResult Update(string id, [FromBody] Lecturer updated)
    {
        var existing = db.Lecturers.Find(l => l.Id == id).FirstOrDefault();
        if (existing == null) return NotFound();
        updated.PasswordHash = string.IsNullOrWhiteSpace(updated.PasswordHash)
            ? existing.PasswordHash
            : BCrypt.Net.BCrypt.HashPassword(updated.PasswordHash);
        db.Lecturers.ReplaceOne(l => l.Id == id, updated);
        return Ok(new { updated.Id, updated.Username, updated.Name, updated.Role });
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(string id)
    {
        var res = db.Lecturers.DeleteOne(l => l.Id == id);
        return res.DeletedCount == 0 ? NotFound() : Ok(new { message = "Deleted." });
    }
}

// ── CoursesController.cs ─────────────────────────────────

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoursesController(MongoDbService db) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll() => Ok(db.Courses.Find(_ => true).ToList());

    [HttpPost]
    [Authorize(Roles = "admin")]
    public IActionResult Create([FromBody] Course course)
    {
        db.Courses.InsertOne(course);
        return Ok(course);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public IActionResult Update(string id, [FromBody] Course updated)
    {
        updated.Id = id; // Preserve the original ID
        var res = db.Courses.ReplaceOne(c => c.Id == id, updated);
        return res.MatchedCount == 0 ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public IActionResult Delete(string id)
    {
        var res = db.Courses.DeleteOne(c => c.Id == id);
        return res.DeletedCount == 0 ? NotFound() : Ok(new { message = "Deleted." });
    }
}

// ── AttendanceController.cs ──────────────────────────────

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController(MongoDbService db) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll([FromQuery] string? date, [FromQuery] string? dept, [FromQuery] string? status)
    {
        var filter = Builders<AttendanceRecord>.Filter.Empty;
        if (!string.IsNullOrEmpty(date))   filter &= Builders<AttendanceRecord>.Filter.Eq(r => r.Date, date);
        if (!string.IsNullOrEmpty(dept))   filter &= Builders<AttendanceRecord>.Filter.Eq(r => r.Department, dept);
        if (!string.IsNullOrEmpty(status)) filter &= Builders<AttendanceRecord>.Filter.Eq(r => r.Status, status);
        return Ok(db.Attendance.Find(filter).SortByDescending(r => r.CreatedAt).ToList());
    }

    [HttpPost("mark")]
    public IActionResult Mark([FromBody] MarkAttendanceRequest req)
    {
        db.ExpireOldSessions();
        var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ── Resolve student ───────────────────────────────
        Student? student = null;
        if (!string.IsNullOrEmpty(req.CardUid))
        {
            student = db.Students.Find(s => s.CardUid == req.CardUid).FirstOrDefault();
            if (student == null)
                return NotFound(new { message = "Card not enrolled. Please register this card first." });
        }
        else if (!string.IsNullOrEmpty(req.StudentRegNo))
        {
            student = db.Students.Find(s => s.RegNo == req.StudentRegNo).FirstOrDefault();
            if (student == null)
                return NotFound(new { message = "Student not found." });
        }
        else return BadRequest(new { message = "Provide CardUid or StudentRegNo." });

        // ── Check active session for this course ──────────
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var session = db.Sessions.Find(s =>
            s.CourseCode == req.CourseCode &&
            s.Date       == today &&
            s.Status     == "open").FirstOrDefault();

        if (session == null)
            return BadRequest(new { message = $"No active session for {req.CourseCode}. Ask your lecturer to open attendance." });

        // ── Prevent duplicate ─────────────────────────────
        var exists = db.Attendance.Find(r =>
            r.StudentRegNo == student.RegNo &&
            r.SessionId    == session.Id &&
            r.Date         == today).Any();
        if (exists)
            return Conflict(new { message = $"{student.Name} already marked for this session." });

        var record = new AttendanceRecord {
            StudentRegNo = student.RegNo,
            StudentName  = student.Name,
            Department   = student.Department,
            Year         = student.Year,
            CourseCode   = req.CourseCode,
            CourseName   = req.CourseName,
            LecturerId   = lecturerId,
            SessionId    = session.Id!,
            CardUid      = req.CardUid,
            Status       = req.Status,
            Date         = today,
            TimeIn       = DateTime.UtcNow.ToString("HH:mm:ss")
        };
        db.Attendance.InsertOne(record);

        // Increment scan count on session
        db.Sessions.UpdateOne(s => s.Id == session.Id,
            Builders<AttendanceSession>.Update.Inc(s => s.ScansCount, 1));

        return Ok(record);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public IActionResult Delete(string id)
    {
        var res = db.Attendance.DeleteOne(r => r.Id == id);
        return res.DeletedCount == 0 ? NotFound() : Ok(new { message = "Deleted." });
    }

    [HttpGet("stats")]
    public IActionResult Stats([FromQuery] string? date)
    {
        db.ExpireOldSessions();
        var d = date ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
        var records       = db.Attendance.Find(r => r.Date == d).ToList();
        var totalStudents = db.Students.CountDocuments(_ => true);
        var activeSessions= db.Sessions.CountDocuments(s => s.Status == "open");
        var enrolledCards = db.Students.CountDocuments(s => s.CardUid != null && s.CardUid != "");
        return Ok(new {
            date = d, totalStudents, activeSessions, enrolledCards,
            present = records.Count(r => r.Status == "Present"),
            absent  = records.Count(r => r.Status == "Absent"),
            byDept  = records.GroupBy(r => r.Department)
                             .Select(g => new { dept = g.Key, count = g.Count() })
        });
    }
}