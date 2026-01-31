using FluentAssertions;
using IntelliDoc.Modules.Audit.Entities;
using IntelliDoc.Modules.Intake.Entities;
using NetArchTest.Rules;
using Xunit;

namespace IntelliDoc.Tests;

public class ArchitectureTests
{
    [Fact]
    public void Audit_Module_Should_Not_Depend_On_Api()
    {
        var assembly = typeof(AuditRecord).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOn("IntelliDoc.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void All_Controller_Classes_Should_End_With_Controller_Suffix()
    {
        // Arrange
        // Picking the assembly where controllers reside (e.g. Audit Module)
        var assembly = typeof(IntelliDoc.Modules.Audit.Endpoints.AuditController).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .Inherit(typeof(Microsoft.AspNetCore.Mvc.ControllerBase))
            .Should()
            .HaveNameEndingWith("Controller")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("By convention, all API endpoints classes should end with 'Controller'.");
    }
}