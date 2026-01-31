using FluentAssertions;
using IntelliDoc.Modules.Extraction.Endpoints;
using IntelliDoc.Modules.Extraction.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using IntelliDoc.Modules.Extraction.Entities;

namespace IntelliDoc.Tests;

public class AnalyticsTests
{
    [Fact]
    public async Task Dashboard_Stats_Should_Calculate_TotalSpend_Correctly()
    {
        // 1. ARRANGE
        var options = new DbContextOptionsBuilder<ExtractionDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var db = new ExtractionDbContext(options);

        var userId = "user-123";

        // Seed Mock Data (2 Invoices)
        db.ExtractionResults.Add(new ExtractionResult
        {
            Id = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            UserId = userId,
            // JSON content with Amount: 100
            JsonData = "{ \"extracted_data\": { \"Entities\": { \"Amount\": 100, \"Sender\": \"Turkcell\" } } }"
        });

        db.ExtractionResults.Add(new ExtractionResult
        {
            Id = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            UserId = userId,
            // JSON content with Amount: 250.50
            JsonData = "{ \"extracted_data\": { \"Entities\": { \"Amount\": 250.50, \"Sender\": \"Amazon\" } } }"
        });

        await db.SaveChangesAsync();

        // Mock the Controller Context (Simulate User Login)
        var controller = new AnalyticsController(db);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
        }, "mock"));

        controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };

        // 2. ACT
        var actionResult = await controller.GetDashboardStats();

        // 3. ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var stats = okResult.Value.Should().BeOfType<AnalyticsDashboardDto>().Subject;

        // Expectation: 100 + 250.50 = 350.50
        stats.TotalSpend.Should().Be(350.50m);
        stats.TotalDocuments.Should().Be(2);

        // Check Vendor Normalization (Should be Uppercase)
        stats.TopVendors.Should().Contain(v => v.Name == "TURKCELL");
    }
}