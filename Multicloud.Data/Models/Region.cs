using System;
using System.Collections.Generic;

namespace Multicloud.Data.Models;

public partial class Region
{
    public int Id { get; set; }

    public string? DisplayName { get; set; }

    public string? Name { get; set; }

    public string? RegionalDisplayName { get; set; }
}
