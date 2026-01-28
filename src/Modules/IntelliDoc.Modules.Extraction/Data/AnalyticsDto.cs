using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelliDoc.Modules.Extraction.Data;

public class AnalyticsDashboardDto
{
    public int TotalDocuments { get; set; }
    public decimal TotalSpend { get; set; }
    public List<MonthlySpend> MonthlyTrend { get; set; } = new();
    public List<VendorSpend> TopVendors { get; set; } = new();
}

public class MonthlySpend
{
    public string Month { get; set; } // "2024-01"
    public decimal Amount { get; set; }
}

public class VendorSpend
{
    public string Name { get; set; }
    public decimal Amount { get; set; }
    public int DocCount { get; set; }
}