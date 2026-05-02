using Microsoft.Extensions.Options;
using MongoDB.Driver;
using AttendanceAPI.Models;

namespace AttendanceAPI.Services;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}

public class MongoDbService
{
    private readonly IMongoDatabase _db;

    public MongoDbService(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _db = client.GetDatabase(settings.Value.DatabaseName);
        SeedData();
    }

    public IMongoCollection<Student>           Students   => _db.GetCollection<Student>("students");
    public IMongoCollection<Lecturer>          Lecturers  => _db.GetCollection<Lecturer>("lecturers");
    public IMongoCollection<Course>            Courses    => _db.GetCollection<Course>("courses");
    public IMongoCollection<AttendanceRecord>  Attendance => _db.GetCollection<AttendanceRecord>("attendance");
    public IMongoCollection<AttendanceSession> Sessions   => _db.GetCollection<AttendanceSession>("sessions");

    // ── Auto-expire sessions ──────────────────────────────
    // Call this before any session query to flip expired ones
    public void ExpireOldSessions()
    {
        var now = DateTime.UtcNow;
        var filter = Builders<AttendanceSession>.Filter.And(
            Builders<AttendanceSession>.Filter.Eq(s => s.Status, "open"),
            Builders<AttendanceSession>.Filter.Lt(s => s.AutoCloseAt, now)
        );
        var update = Builders<AttendanceSession>.Update
            .Set(s => s.Status, "expired")
            .Set(s => s.ClosedAt, now);
        Sessions.UpdateMany(filter, update);
    }

    // ── Seed ─────────────────────────────────────────────
    private void SeedData()
    {
        if (!Lecturers.Find(_ => true).Any())
        {
            Lecturers.InsertMany(new List<Lecturer> {
                new() { Username="admin",       PasswordHash=BCrypt.Net.BCrypt.HashPassword("admin123"),
                        Name="Admin Officer",   Title="Mr.",   Department="Registry",          Role="admin" },
                new() { Username="dr.kamau",    PasswordHash=BCrypt.Net.BCrypt.HashPassword("pass123"),
                        Name="James Kamau",     Title="Dr.",   Department="Computer Science",  Role="lecturer" },
                new() { Username="prof.akinyi", PasswordHash=BCrypt.Net.BCrypt.HashPassword("pass123"),
                        Name="Mary Akinyi",     Title="Prof.", Department="Mathematics",       Role="lecturer" },
            });
        }

        if (!Courses.Find(_ => true).Any())
        {
            Courses.InsertMany(new List<Course> {
                new() { Code="SCS 201", Name="Data Structures",      Department="Computer Science",      Year="Year 2" },
                new() { Code="SCS 301", Name="Operating Systems",    Department="Computer Science",      Year="Year 3" },
                new() { Code="ITS 202", Name="Database Systems",     Department="Information Technology",Year="Year 2" },
                new() { Code="MAT 102", Name="Calculus II",          Department="Mathematics",           Year="Year 1" },
                new() { Code="MAT 201", Name="Linear Algebra",       Department="Mathematics",           Year="Year 2" },
            });
        }

        if (!Students.Find(_ => true).Any())
        {
            Students.InsertMany(new List<Student> {
                new() { RegNo="EG/2022/001", Name="Amara Njoroge",     Department="Computer Science",      Year="Y2", Gender="Female", Intake="2022" },
                new() { RegNo="EG/2022/002", Name="Brian Otieno",      Department="Computer Science",      Year="Y2", Gender="Male",   Intake="2022" },
                new() { RegNo="EG/2022/003", Name="Catherine Wanjiku", Department="Computer Science",      Year="Y2", Gender="Female", Intake="2022" },
                new() { RegNo="EG/2022/004", Name="Daniel Kipchoge",   Department="Computer Science",      Year="Y2", Gender="Male",   Intake="2022" },
                new() { RegNo="EG/2022/005", Name="Esther Auma",       Department="Computer Science",      Year="Y2", Gender="Female", Intake="2022" },
                new() { RegNo="EG/2022/006", Name="Francis Mwangi",    Department="Information Technology",Year="Y2", Gender="Male",   Intake="2022" },
                new() { RegNo="EG/2022/007", Name="Grace Chebet",      Department="Information Technology",Year="Y2", Gender="Female", Intake="2022" },
                new() { RegNo="EG/2022/008", Name="Hassan Abdi",       Department="Information Technology",Year="Y2", Gender="Male",   Intake="2022" },
                new() { RegNo="EG/2022/009", Name="Irene Wambua",      Department="Mathematics",           Year="Y2", Gender="Female", Intake="2022" },
                new() { RegNo="EG/2022/010", Name="James Odhiambo",    Department="Mathematics",           Year="Y2", Gender="Male",   Intake="2022" },
            });
        }
    }
}