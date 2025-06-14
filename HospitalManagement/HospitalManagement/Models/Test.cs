﻿using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Test
{
    public int TestId { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public string Description { get; set; } = null!;

    public virtual ICollection<TestList> TestLists { get; set; } = new List<TestList>();
}
