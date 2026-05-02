// ── Student.cs ──────────────────────────────────────────
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AttendanceAPI.Models;

public class Student
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    [BsonElement("regNo")]      public string RegNo { get; set; } = string.Empty;
    [BsonElement("name")]       public string Name { get; set; } = string.Empty;
    [BsonElement("department")] public string Department { get; set; } = string.Empty;
    [BsonElement("year")]       public string Year { get; set; } = string.Empty;
    [BsonElement("gender")]     public string Gender { get; set; } = string.Empty;
    [BsonElement("intake")]     public string Intake { get; set; } = string.Empty;
    [BsonElement("cardUid")]          public string? CardUid { get; set; }
    [BsonElement("cardEnrolledAt")]   public DateTime? CardEnrolledAt { get; set; }
    [BsonElement("createdAt")]        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ── Lecturer.cs ─────────────────────────────────────────

public class Lecturer
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    [BsonElement("username")]     public string Username { get; set; } = string.Empty;
    [BsonElement("passwordHash")] public string PasswordHash { get; set; } = string.Empty;
    [BsonElement("name")]         public string Name { get; set; } = string.Empty;
    [BsonElement("title")]        public string Title { get; set; } = string.Empty;
    [BsonElement("department")]   public string Department { get; set; } = string.Empty;
    [BsonElement("role")]         public string Role { get; set; } = "lecturer";
    [BsonElement("createdAt")]    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ── Course.cs ────────────────────────────────────────────

public class Course
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    [BsonElement("code")]       public string Code { get; set; } = string.Empty;
    [BsonElement("name")]       public string Name { get; set; } = string.Empty;
    [BsonElement("department")] public string Department { get; set; } = string.Empty;
    [BsonElement("year")]       public string Year { get; set; } = string.Empty;
}

// ── AttendanceSession.cs ─────────────────────────────────

public class AttendanceSession
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("courseCode")]   public string CourseCode { get; set; } = string.Empty;
    [BsonElement("courseName")]   public string CourseName { get; set; } = string.Empty;
    [BsonElement("department")]   public string Department { get; set; } = string.Empty;
    [BsonElement("year")]         public string Year { get; set; } = string.Empty;
    [BsonElement("lecturerId")]   public string LecturerId { get; set; } = string.Empty;
    [BsonElement("lecturerName")] public string LecturerName { get; set; } = string.Empty;
    [BsonElement("room")]         public string Room { get; set; } = string.Empty;

    // timing
    [BsonElement("openedAt")]     public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    [BsonElement("closedAt")]     public DateTime? ClosedAt { get; set; }
    [BsonElement("durationMins")] public int DurationMins { get; set; } = 15;
    [BsonElement("autoCloseAt")]  public DateTime AutoCloseAt { get; set; }

    // state:  "open" | "closed" | "expired"
    [BsonElement("status")]       public string Status { get; set; } = "open";

    [BsonElement("date")]         public string Date { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
    [BsonElement("scansCount")]   public int ScansCount { get; set; } = 0;
}

// ── AttendanceRecord.cs ──────────────────────────────────

public class AttendanceRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    [BsonElement("studentRegNo")] public string StudentRegNo { get; set; } = string.Empty;
    [BsonElement("studentName")]  public string StudentName { get; set; } = string.Empty;
    [BsonElement("department")]   public string Department { get; set; } = string.Empty;
    [BsonElement("year")]         public string Year { get; set; } = string.Empty;
    [BsonElement("courseCode")]   public string CourseCode { get; set; } = string.Empty;
    [BsonElement("courseName")]   public string CourseName { get; set; } = string.Empty;
    [BsonElement("lecturerId")]   public string LecturerId { get; set; } = string.Empty;
    [BsonElement("sessionId")]    public string SessionId { get; set; } = string.Empty;
    [BsonElement("cardUid")]      public string? CardUid { get; set; }
    [BsonElement("status")]       public string Status { get; set; } = "Present";
    [BsonElement("date")]         public string Date { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
    [BsonElement("timeIn")]       public string TimeIn { get; set; } = DateTime.UtcNow.ToString("HH:mm:ss");
    [BsonElement("createdAt")]    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ── DTOs ─────────────────────────────────────────────────

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, string Name, string Role, string Title);

public record OpenSessionRequest(
    string CourseCode,
    string CourseName,
    string Department,
    string Year,
    string Room,
    int DurationMins         // auto-close after this many minutes
);

public record MarkAttendanceRequest(
    string? StudentRegNo,
    string? CardUid,
    string CourseCode,
    string CourseName,
    string Status
);

public record EnrollCardRequest(string StudentId, string CardUid);