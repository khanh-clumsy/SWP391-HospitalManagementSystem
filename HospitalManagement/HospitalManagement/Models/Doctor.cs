using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
namespace HospitalManagement.Models;

public partial class Doctor
{
    public int DoctorId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string DepartmentName { get; set; } = null!;

    public bool IsDepartmentHead { get; set; }

    public int ExperienceYear { get; set; }

    public string? Degree { get; set; }

    public bool IsActive { get; set; }

    public bool IsSpecial { get; set; }

    public string? Gender { get; set; }

    public string? ProfileImage { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<News> News { get; set; } = new List<News>();

    public virtual ICollection<ScheduleChangeRequest> ScheduleChangeRequests { get; set; } = new List<ScheduleChangeRequest>();

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public string GetFullGender()
    {
        if (this.Gender == "M")
        {
            return "Male";
        }
        else if (this.Gender == "F")
        {
            return "Female";
        }
        else
        {
            return "Other";
        }
    }

    public string GenerateDoctorCode()
    {
        string ans = "";
        string s = this.FullName;
        var part = s.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < part.Length; i++)
        {
            string x = part[i];
            if (i < part.Length - 1)
            {
                // ans += char.ToUpper(x[0]);
                var stringNotSign = RemoveDiacritics(x);
                ans += char.ToUpper(stringNotSign[0]);
            }
            else ans = x + ans;
        }
        ans += this.DoctorId.ToString();
        return ans;
    }
    public static string RemoveDiacritics(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    // public string GenerateDoctorCode()
    // {
    //     var parts = this.FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    //     if (parts.Length == 0) return "Unknown" + this.DoctorId;

    //     var last = parts[^1];
    //     var initials = string.Join("", parts.Take(parts.Length - 1).Select(p => p[0]));

    //     return $"{RemoveDiacritics(last)}{initials.ToUpper()}{this.DoctorId}";
    // }


}
