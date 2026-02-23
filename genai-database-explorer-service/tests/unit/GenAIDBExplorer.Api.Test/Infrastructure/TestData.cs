using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Api.Test.Infrastructure;

/// <summary>
/// Shared test data factory for creating domain model instances in tests.
/// </summary>
public static class TestData
{
    public static SemanticModel CreateTestModel(string name = "TestModel")
    {
        var model = new SemanticModel(name, "TestSource", "Test description");

        var table1 = new SemanticModelTable("SalesLT", "Product", "Product table");
        table1.SetSemanticDescription("Contains product information");
        table1.AddColumn(new SemanticModelColumn("SalesLT", "ProductID", "Primary key") { IsPrimaryKey = true });
        table1.AddColumn(new SemanticModelColumn("SalesLT", "Name", "Product name"));
        model.AddTable(table1);

        var table2 = new SemanticModelTable("SalesLT", "Customer", "Customer table");
        model.AddTable(table2);

        var view1 = new SemanticModelView("SalesLT", "vProductAndDescription", "Product view");
        view1.AddColumn(new SemanticModelColumn("SalesLT", "ProductID", "Primary key"));
        model.AddView(view1);

        var sproc1 = new SemanticModelStoredProcedure("dbo", "uspGetCustomers", "SELECT * FROM Customers", "@Param1 INT", "Get customers");
        model.AddStoredProcedure(sproc1);

        return model;
    }
}
