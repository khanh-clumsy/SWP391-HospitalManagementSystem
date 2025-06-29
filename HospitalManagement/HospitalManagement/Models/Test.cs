using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Test
{
    public int TestId { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public string? RoomType { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<PackageTest> PackageTests { get; set; } = new List<PackageTest>();

    public virtual ICollection<TestList> TestLists { get; set; } = new List<TestList>();
}
