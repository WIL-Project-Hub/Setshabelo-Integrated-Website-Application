using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SetshabeloAdmissionSystem.Models;

namespace SetshabeloAdmissionSystem.Services
{
    public class SupabaseService
    {
        private readonly HttpClient _http;
        private readonly string _supabaseUrl;
        private readonly string _anonKey;
        private readonly string _serviceRoleKey;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        public SupabaseService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _supabaseUrl = config["Supabase:Url"]!;
            _anonKey = config["Supabase:AnonKey"]!;
            _serviceRoleKey = config["Supabase:ServiceRoleKey"]!;
        }

        private void SetHeaders(bool useServiceRole = false)
        {
            _http.DefaultRequestHeaders.Clear();
            var key = useServiceRole ? _serviceRoleKey : _anonKey;
            _http.DefaultRequestHeaders.Add("apikey", key);
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {key}");
        }

        private void SetAuthHeader(string accessToken)
        {
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("apikey", _anonKey);
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
        }

        // 
        // AUTH
        // 

        public async Task<AuthResult?> SignInAsync(string email, string password)
        {
            SetHeaders();
            var body = new { email, password };
            var content = new StringContent(JsonSerializer.Serialize(body),
                Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(
                $"{_supabaseUrl}/auth/v1/token?grant_type=password", content);

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new AuthResult
            {
                AccessToken = root.GetProperty("access_token").GetString()!,
                UserId = root.GetProperty("user").GetProperty("id").GetString()!,
                Email = root.GetProperty("user").GetProperty("email").GetString()!
            };
        }

        public async Task<AuthResult?> SignUpAsync(string email, string password)
        {
            SetHeaders();
            var body = new { email, password };
            var content = new StringContent(JsonSerializer.Serialize(body),
                Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await _http.PostAsync(
                    $"{_supabaseUrl}/auth/v1/signup", content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SIGNUP] NETWORK ERROR: {ex.Message}");
                return null;
            }

            var rawJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[SIGNUP] FAILED — Status: {(int)response.StatusCode}");
                Console.WriteLine($"[SIGNUP] FAILED — Body: {rawJson}");
                return null;
            }

            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("access_token", out var tokenEl) &&
                root.TryGetProperty("user", out var userEl))
            {
                return new AuthResult
                {
                    AccessToken = tokenEl.GetString()!,
                    UserId = userEl.GetProperty("id").GetString()!,
                    Email = userEl.GetProperty("email").GetString()!
                };
            }

            if (root.TryGetProperty("id", out var idEl))
            {
                return new AuthResult
                {
                    AccessToken = string.Empty,
                    UserId = idEl.GetString()!,
                    Email = email
                };
            }

            return null;
        }

        // Create an admin user via Service Role (bypasses email confirmation)
        public async Task<AuthResult?> CreateAdminUserAsync(string email, string password)
        {
            SetHeaders(useServiceRole: true);
            var body = new { email, password, email_confirm = true };
            var content = new StringContent(JsonSerializer.Serialize(body),
                Encoding.UTF8, "application/json");

            var response = await _http.PostAsync($"{_supabaseUrl}/auth/v1/admin/users", content);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[CREATE ADMIN] FAILED — {(int)response.StatusCode}: {err}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new AuthResult
            {
                AccessToken = string.Empty,
                UserId = root.GetProperty("id").GetString()!,
                Email = root.GetProperty("email").GetString()!
            };
        }

        public async Task<bool> CreateProfileAndParentAsync(string userId,
            string email, string firstName, string lastName)
        {
            SetHeaders(useServiceRole: true);

            var profileBody = new { id = userId, email, role = "Parent" };
            var profileContent = new StringContent(
                JsonSerializer.Serialize(profileBody), Encoding.UTF8, "application/json");

            var profileReq = new HttpRequestMessage(
                HttpMethod.Post, $"{_supabaseUrl}/rest/v1/profiles")
            { Content = profileContent };
            profileReq.Headers.Add("Prefer", "return=minimal");

            var profileResp = await _http.SendAsync(profileReq);
            if (!profileResp.IsSuccessStatusCode) return false;

            var parentBody = new { user_id = userId, first_name = firstName, last_name = lastName };
            var parentContent = new StringContent(
                JsonSerializer.Serialize(parentBody), Encoding.UTF8, "application/json");

            var parentReq = new HttpRequestMessage(
                HttpMethod.Post, $"{_supabaseUrl}/rest/v1/parents")
            { Content = parentContent };
            parentReq.Headers.Add("Prefer", "return=minimal");

            var parentResp = await _http.SendAsync(parentReq);
            return parentResp.IsSuccessStatusCode;
        }

        // Create the profiles plus admins rows for a new admin
        public async Task<bool> CreateAdminProfileAsync(string userId, string email, string firstName, string lastName)
        {
            SetHeaders(useServiceRole: true);

            // 1) Insert into profiles (role = Admin)
            var profileBody = new { id = userId, email, role = "Admin" };
            var profileContent = new StringContent(
                JsonSerializer.Serialize(profileBody), Encoding.UTF8, "application/json");
            var profileReq = new HttpRequestMessage(
                HttpMethod.Post, $"{_supabaseUrl}/rest/v1/profiles")
            { Content = profileContent };
            profileReq.Headers.Add("Prefer", "return=minimal");

            var profileResp = await _http.SendAsync(profileReq);
            if (!profileResp.IsSuccessStatusCode)
            {
                var err = await profileResp.Content.ReadAsStringAsync();
                Console.WriteLine($"[CREATE ADMIN PROFILE] {err}");
                return false;
            }

            // 2) Insert into admins
            var adminBody = new { user_id = userId, first_name = firstName, last_name = lastName };
            var adminContent = new StringContent(
                JsonSerializer.Serialize(adminBody), Encoding.UTF8, "application/json");
            var adminReq = new HttpRequestMessage(
                HttpMethod.Post, $"{_supabaseUrl}/rest/v1/admins")
            { Content = adminContent };
            adminReq.Headers.Add("Prefer", "return=minimal");

            var adminResp = await _http.SendAsync(adminReq);
            return adminResp.IsSuccessStatusCode;
        }

        public async Task<Profile?> GetProfileAsync(string userId, string accessToken)
        {
            SetAuthHeader(accessToken);
            var response = await _http.GetAsync(
                $"{_supabaseUrl}/rest/v1/profiles?id=eq.{userId}&select=*");

            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<List<Profile>>(json, JsonOpts);
            return list?.FirstOrDefault();
        }

        public async Task<Parent?> GetParentByUserIdAsync(string userId)
        {
            SetHeaders(useServiceRole: true);
            var response = await _http.GetAsync(
                $"{_supabaseUrl}/rest/v1/parents?user_id=eq.{userId}&select=*");

            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<List<Parent>>(json, JsonOpts);
            return list?.FirstOrDefault();
        }

        public async Task<bool> UpdateParentNameAsync(Guid parentId, string firstName, string lastName)
        {
            SetHeaders(useServiceRole: true);
            var body = new { first_name = firstName, last_name = lastName };
            var content = new StringContent(JsonSerializer.Serialize(body),
                Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Patch,
                $"{_supabaseUrl}/rest/v1/parents?parent_id=eq.{parentId}")
            { Content = content };
            request.Headers.Add("Prefer", "return=minimal");

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<Admin?> GetAdminByUserIdAsync(string userId)
        {
            SetHeaders(useServiceRole: true);
            var response = await _http.GetAsync(
                $"{_supabaseUrl}/rest/v1/admins?user_id=eq.{userId}&select=*");

            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<List<Admin>>(json, JsonOpts);
            return list?.FirstOrDefault();
        }

        // 
        // APPLICATIONS
        // 

        public async Task<List<Application>> GetAllApplicationsAsync()
        {
            SetHeaders(useServiceRole: true);
            var response = await _http.GetAsync(
                $"{_supabaseUrl}/rest/v1/applications?select=*&order=submission_date.desc");

            if (!response.IsSuccessStatusCode) return new();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Application>>(json, JsonOpts) ?? new();
        }

        public async Task<Application?> GetApplicationByIdAsync(Guid id)
        {
            SetHeaders(useServiceRole: true);
            var response = await _http.GetAsync(
                $"{_supabaseUrl}/rest/v1/applications?application_id=eq.{id}&select=*");

            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<List<Application>>(json, JsonOpts);
            return list?.FirstOrDefault();
        }

        public async Task<bool> UpdateApplicationStatusAsync(Guid id, string status, Guid adminId)
        {
            SetHeaders(useServiceRole: true);
            var body = new { application_status = status, reviewed_by = adminId, reviewed_date = DateTime.UtcNow };
            var content = new StringContent(JsonSerializer.Serialize(body),
                Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Patch,
                $"{_supabaseUrl}/rest/v1/applications?application_id=eq.{id}")
            { Content = content };
            request.Headers.Add("Prefer", "return=minimal");

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<Application>> GetApplicationsByParentIdAsync(Guid parentId)
        {
            SetHeaders(useServiceRole: true);
            var response = await _http.GetAsync(
                $"{_supabaseUrl}/rest/v1/applications?parent_id=eq.{parentId}&select=*&order=submission_date.desc");

            if (!response.IsSuccessStatusCode) return new();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Application>>(json, JsonOpts) ?? new();
        }

        public async Task<Guid?> CreateApplicationAsync(Application app)
        {
            SetHeaders(useServiceRole: true);
            var body = new
            {
                parent_id = app.ParentId,
                parent_first_name = app.ParentFirstName,
                parent_last_name = app.ParentLastName,
                parent_title = app.ParentTitle,
                parent_initials = app.ParentInitials,
                parent_id_number = app.ParentIdNumber,
                parent_phone = app.ParentPhone,
                parent_email = app.ParentEmail,
                parent_address = app.ParentAddress,
                parent_relationship = app.ParentRelationship,
                parent_race = app.ParentRace,
                parent_home_language = app.ParentHomeLanguage,
                parent_occupation = app.ParentOccupation,
                parent_employer = app.ParentEmployer,
                child_full_name = app.ChildFullName,
                child_date_of_birth = app.ChildDateOfBirth,
                child_gender = app.ChildGender,
                child_race = app.ChildRace,
                child_id_number = app.ChildIdNumber,
                child_citizenship = app.ChildCitizenship,
                country_of_residence = app.CountryOfResidence,
                province = app.Province,
                child_home_language = app.ChildHomeLanguage,
                preferred_language = app.PreferredLanguage,
                grade_applying_for = app.GradeApplyingFor,
                pre_primary_education = app.PrePrimaryEducation,
                highest_grade_passed = app.HighestGradePassed,
                year_grade_passed = app.YearGradePassed,
                accession_no = app.AccessionNo,
                previous_school = app.PreviousSchool,
                previous_school_address = app.PreviousSchoolAddress,
                physical_address = app.PhysicalAddress,
                city_or_suburb = app.CityOrSuburb,
                postal_code = app.PostalCode,
                emergency_phone = app.EmergencyPhone,
                child_phone = app.ChildPhone,
                child_email = app.ChildEmail,
                mode_of_transport = app.ModeOfTransport,
                medical_aid_number = app.MedicalAidNumber,
                medical_aid = app.MedicalAid,
                medical_aid_main_member = app.MedicalAidMainMember,
                doctor_name = app.DoctorName,
                doctor_address = app.DoctorAddress,
                doctor_phone = app.DoctorPhone,
                medical_condition = app.MedicalCondition,
                special_problems = app.SpecialProblems,
                child_dexterity = app.ChildDexterity,
                application_status = "Pending"
            };
            var content = new StringContent(JsonSerializer.Serialize(body),
                Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_supabaseUrl}/rest/v1/applications")
            { Content = content };
            request.Headers.Add("Prefer", "return=representation");

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[CREATE APP FAILED] {(int)response.StatusCode} — {err}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<List<Application>>(json, JsonOpts);
            return list?.FirstOrDefault()?.ApplicationId;
        }

        public async Task<Dictionary<string, int>> GetStatusCountsAsync()
        {
            var apps = await GetAllApplicationsAsync();
            return new Dictionary<string, int>
            {
                { "Total", apps.Count },
                { "Pending", apps.Count(a => a.ApplicationStatus == "Pending") },
                { "Under Review", apps.Count(a => a.ApplicationStatus == "Under Review") },
                { "Accepted", apps.Count(a => a.ApplicationStatus == "Accepted") },
                { "Rejected", apps.Count(a => a.ApplicationStatus == "Rejected") }
            };
        }

        // 
        // DOCUMENTS
        // 

        public async Task<List<Document>> GetDocumentsForApplicationAsync(Guid applicationId)
        {
            SetHeaders(useServiceRole: true);
            var response = await _http.GetAsync(
                $"{_supabaseUrl}/rest/v1/documents?application_id=eq.{applicationId}&select=*");

            if (!response.IsSuccessStatusCode) return new();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Document>>(json, JsonOpts) ?? new();
        }

        public async Task<bool> CreateDocumentAsync(Guid applicationId, string fileUrl, string documentType)
        {
            SetHeaders(useServiceRole: true);
            var body = new { application_id = applicationId, file_url = fileUrl, document_type = documentType };
            var content = new StringContent(JsonSerializer.Serialize(body),
                Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(
                $"{_supabaseUrl}/rest/v1/documents", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<string?> UploadFileToStorageAsync(IFormFile file, string path)
        {
            SetHeaders(useServiceRole: true);
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var content = new ByteArrayContent(bytes);
            content.Headers.ContentType =
                new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");

            var response = await _http.PostAsync(
                $"{_supabaseUrl}/storage/v1/object/application_documents/{path}", content);

            if (!response.IsSuccessStatusCode) return null;

            return $"{_supabaseUrl}/storage/v1/object/public/application_documents/{path}";
        }

        public async Task<string?> UploadAchievementImageAsync(IFormFile file, string fileName)
        {
            SetHeaders(useServiceRole: true);
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var content = new ByteArrayContent(bytes);
            content.Headers.ContentType =
                new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");

            var response = await _http.PostAsync(
                $"{_supabaseUrl}/storage/v1/object/achievement_images/{fileName}", content);

            if (!response.IsSuccessStatusCode) return null;

            return $"{_supabaseUrl}/storage/v1/object/public/achievement_images/{fileName}";
        }

        // 
        // COMMENTS
        // 

        public async Task<List<Comment>> GetCommentsForApplicationAsync(Guid applicationId)
        {
            SetHeaders(useServiceRole: true);
            var response = await _http.GetAsync(
                $"{_supabaseUrl}/rest/v1/comments?application_id=eq.{applicationId}&select=*&order=created_at.desc");

            if (!response.IsSuccessStatusCode) return new();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Comment>>(json, JsonOpts) ?? new();
        }

        public async Task<bool> CreateCommentAsync(Guid applicationId, Guid adminId, string commentText)
        {
            SetHeaders(useServiceRole: true);
            var body = new { application_id = applicationId, admin_id = adminId, comment_text = commentText };
            var content = new StringContent(JsonSerializer.Serialize(body),
                Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(
                $"{_supabaseUrl}/rest/v1/comments", content);
            return response.IsSuccessStatusCode;
        }

        // 
        // SCHOOL UPDATES
        // 

        public async Task<List<SchoolUpdate>> GetSchoolUpdatesAsync()
        {
            SetHeaders(useServiceRole: true);
            var response = await _http.GetAsync(
                $"{_supabaseUrl}/rest/v1/school_updates?select=*&order=published_at.desc");

            if (!response.IsSuccessStatusCode) return new();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<SchoolUpdate>>(json, JsonOpts) ?? new();
        }

        public async Task<bool> CreateSchoolUpdateAsync(Guid adminId, string title, string contentText)
        {
            SetHeaders(useServiceRole: true);
            var payload = new { admin_id = adminId, title, content = contentText };
            var content = new StringContent(JsonSerializer.Serialize(payload),
                Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(
                $"{_supabaseUrl}/rest/v1/school_updates", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteSchoolUpdateAsync(Guid updateId)
        {
            SetHeaders(useServiceRole: true);
            var response = await _http.DeleteAsync(
                $"{_supabaseUrl}/rest/v1/school_updates?update_id=eq.{updateId}");
            return response.IsSuccessStatusCode;
        }

        // 
        // ACHIEVEMENTS
        // 

        public async Task<List<Achievement>> GetAchievementsAsync()
        {
            SetHeaders(useServiceRole: true);
            var response = await _http.GetAsync(
                $"{_supabaseUrl}/rest/v1/achievements?select=*&order=published_at.desc");

            if (!response.IsSuccessStatusCode) return new();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Achievement>>(json, JsonOpts) ?? new();
        }

        public async Task<Achievement?> GetAchievementByIdAsync(Guid id)
        {
            SetHeaders(useServiceRole: true);
            var response = await _http.GetAsync(
                $"{_supabaseUrl}/rest/v1/achievements?achievement_id=eq.{id}&select=*");

            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<List<Achievement>>(json, JsonOpts);
            return list?.FirstOrDefault();
        }

        public async Task<bool> CreateAchievementAsync(Guid adminId, string title,
            string? description, string? imageUrl)
        {
            SetHeaders(useServiceRole: true);
            var payload = new
            {
                admin_id = adminId,
                title,
                description = description ?? string.Empty,
                image_url = imageUrl ?? string.Empty,
                is_public = true
            };
            var content = new StringContent(JsonSerializer.Serialize(payload),
                Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(
                $"{_supabaseUrl}/rest/v1/achievements", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateAchievementAsync(Guid id, string title,
            string? description, string? imageUrl)
        {
            SetHeaders(useServiceRole: true);
            var payload = new
            {
                title,
                description = description ?? string.Empty,
                image_url = imageUrl ?? string.Empty
            };
            var content = new StringContent(JsonSerializer.Serialize(payload),
                Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Patch,
                $"{_supabaseUrl}/rest/v1/achievements?achievement_id=eq.{id}")
            { Content = content };
            request.Headers.Add("Prefer", "return=minimal");

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAchievementAsync(Guid id)
        {
            SetHeaders(useServiceRole: true);
            var response = await _http.DeleteAsync(
                $"{_supabaseUrl}/rest/v1/achievements?achievement_id=eq.{id}");
            return response.IsSuccessStatusCode;
        }
    }

    public class AuthResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
