using System;
using System.Collections.Generic;
using System.Linq;
using GraphManipulation.Extensions;
using GraphManipulation.Manipulation;
using GraphManipulation.Models.Stores;
using GraphManipulation.Models.Structures;
using VDS.RDF;
using Xunit;

namespace Test;

public class GraphManipulatorTest
{
    private const string BaseUri = "http://www.test.com/";

    [Fact]
    public void MoveColumnChangesItsParent()
    {
        var tds = CreateDatastore();
        var graphManipulator = new GraphManipulator<Sqlite>(tds.ExpectedSqlite.ToGraph());
        
        var columnUriBefore = tds.ExpectedColumn.Uri.ToString();
        tds.ExpectedTable2.AddStructure(tds.ExpectedColumn);
        var columnUriAfter = tds.ExpectedColumn.Uri.ToString();
        
        graphManipulator.Move(new Uri(columnUriBefore), tds.ExpectedColumn);
        
        var subj = graphManipulator.Graph.CreateUriNode(tds.ExpectedTable2.Uri);
        var pred = graphManipulator.Graph.CreateUriNode("ddl:hasStructure");
        var obj = graphManipulator.Graph.CreateUriNode(tds.ExpectedColumn.Uri);

        Assert.Contains(new Triple(subj, pred, obj), graphManipulator.Graph.Triples);
        
        Assert.Single(graphManipulator.Changes);
        Assert.Contains($"MOVE({columnUriBefore}, {columnUriAfter})", graphManipulator.Changes);
    }

    [Fact]
    public void MoveColumnKeepsAttributesSuchAsColumnType()
    {
        var tds = CreateDatastore();
        var graphManipulator = new GraphManipulator<Sqlite>(tds.ExpectedSqlite.ToGraph());

        var columnUriBefore = tds.ExpectedColumn.Uri.ToString();
        tds.ExpectedTable2.AddStructure(tds.ExpectedColumn);
        
        graphManipulator.Move(new Uri(columnUriBefore), tds.ExpectedColumn);
        
        var subj = graphManipulator.Graph.CreateUriNode(tds.ExpectedColumn.Uri);
        var pred = graphManipulator.Graph.CreateUriNode("ddl:hasDataType");
        var obj = graphManipulator.Graph.CreateLiteralNode("INT");

        Assert.Contains(new Triple(subj, pred, obj), graphManipulator.Graph.Triples);
    }

    [Fact (Skip = "Future work")]
    public void MoveTableChildrenMoved()
    {
        
    }

    [Fact]
    public void MoveToNewParentMovesStructure()
    {
        var tds = CreateDatastore();
        var graphManipulator = new GraphManipulator<Sqlite>(tds.ExpectedSqlite.ToGraph());
        
        var columnUriBefore = tds.ExpectedColumn.Uri.ToString();
        tds.ExpectedTable2.AddStructure(tds.ExpectedColumn);
        var columnUriAfter = tds.ExpectedColumn.Uri.ToString();
        
        graphManipulator.MoveToNewParent(new Uri(columnUriBefore), tds.ExpectedTable2);
        
        var subj = graphManipulator.Graph.CreateUriNode(tds.ExpectedTable2.Uri);
        var pred = graphManipulator.Graph.CreateUriNode("ddl:hasStructure");
        var obj = graphManipulator.Graph.CreateUriNode(tds.ExpectedColumn.Uri);

        Assert.Contains(new Triple(subj, pred, obj), graphManipulator.Graph.Triples);
        
        Assert.Single(graphManipulator.Changes);
        Assert.Contains($"MOVE({columnUriBefore}, {columnUriAfter})", graphManipulator.Changes);
    }

    [Fact]
    public void RenameRenamesStructure()
    {
        var tds = CreateDatastore();
        var graphManipulator = new GraphManipulator<Sqlite>(tds.ExpectedSqlite.ToGraph());
        
        var columnUriBefore = tds.ExpectedColumn.Uri.ToString();
        var newName = "NewName";
        tds.ExpectedColumn.UpdateName(newName);
        
        graphManipulator.Rename(new Uri(columnUriBefore), newName);
        
        var subj = graphManipulator.Graph.CreateUriNode(tds.ExpectedColumn.Uri);
        var pred = graphManipulator.Graph.CreateUriNode("ddl:hasName");
        var obj = graphManipulator.Graph.CreateLiteralNode(newName);
        
        Assert.Contains(new Triple(subj, pred, obj), graphManipulator.Graph.Triples);
    }

    [Fact]
    public void RenameAddsChange()
    {
        var tds = CreateDatastore();
        var graphManipulator = new GraphManipulator<Sqlite>(tds.ExpectedSqlite.ToGraph());
        
        var newName = "NewName";
        
        var columnUriBefore = tds.ExpectedColumn.Uri.ToString();
        tds.ExpectedColumn.UpdateName(newName);
        var columnUriAfter = tds.ExpectedColumn.Uri.ToString();

        graphManipulator.Rename(new Uri(columnUriBefore), newName);
        
        Assert.Single(graphManipulator.Changes);
        Assert.Contains($"RENAME({columnUriBefore}, {columnUriAfter})", graphManipulator.Changes);
    }

    [Fact]
    public void RenameAddsChangesForChildrenIdRecomputes()
    {
        var tds = CreateDatastore();
        var graphManipulator = new GraphManipulator<Sqlite>(tds.ExpectedSqlite.ToGraph());
        
        var newName = "NewName";

        var tableUriBefore = tds.ExpectedTable1.Uri.ToString();
        var column1UriBefore = tds.ExpectedColumn.Uri.ToString();
        var column2UriBefore = tds.ExpectedPrimaryColumn1.Uri.ToString();
        tds.ExpectedTable1.UpdateName(newName);
        var tableUriAfter = tds.ExpectedTable1.Uri.ToString();
        var column1UriAfter = tds.ExpectedColumn.Uri.ToString();
        var column2UriAfter = tds.ExpectedPrimaryColumn1.Uri.ToString();
        
        graphManipulator.Rename(new Uri(tableUriBefore), newName);

        var rename = $"RENAME({tableUriBefore}, {tableUriAfter})";
        var move1 = $"MOVE({column1UriBefore}, {column1UriAfter})";
        var move2 = $"MOVE({column2UriBefore}, {column2UriAfter})";
        var expectedChanges = new List<string> { rename, move1, move2 };
        
        Assert.Equal(3, graphManipulator.Changes.Count);
        Assert.Contains(rename, graphManipulator.Changes);
        Assert.Contains(move1, graphManipulator.Changes);
        Assert.Contains(move2, graphManipulator.Changes);
        Assert.True(expectedChanges.SequenceEqual(graphManipulator.Changes));
    }

    [Fact]
    public void RenameAddsChangesForChildrensChildrenIdRecomputes()
    {
        var tds = CreateDatastore();
        var graphManipulator = new GraphManipulator<Sqlite>(tds.ExpectedSqlite.ToGraph());
        
        var newName = "NewName";

        var schemaUriBefore = tds.ExpectedSchema.Uri.ToString();
        var table1UriBefore = tds.ExpectedTable1.Uri.ToString();
        var table2UriBefore = tds.ExpectedTable2.Uri.ToString();
        var column1UriBefore = tds.ExpectedColumn.Uri.ToString();
        var column2UriBefore = tds.ExpectedPrimaryColumn1.Uri.ToString();
        var column3UriBefore = tds.ExpectedPrimaryColumn2.Uri.ToString();
        tds.ExpectedSchema.UpdateName(newName);
        var schemaUriAfter = tds.ExpectedSchema.Uri.ToString();
        var table1UriAfter = tds.ExpectedTable1.Uri.ToString();
        var table2UriAfter = tds.ExpectedTable2.Uri.ToString();
        var column1UriAfter = tds.ExpectedColumn.Uri.ToString();
        var column2UriAfter = tds.ExpectedPrimaryColumn1.Uri.ToString();
        var column3UriAfter = tds.ExpectedPrimaryColumn2.Uri.ToString();
        
        graphManipulator.Rename(new Uri(schemaUriBefore), newName);

        var rename = $"RENAME({schemaUriBefore}, {schemaUriAfter})";
        var move1 = $"MOVE({table1UriBefore}, {table1UriAfter})";
        var move2 = $"MOVE({column1UriBefore}, {column1UriAfter})";
        var move3 = $"MOVE({column2UriBefore}, {column2UriAfter})";
        var move4 = $"MOVE({table2UriBefore}, {table2UriAfter})";
        var move5 = $"MOVE({column3UriBefore}, {column3UriAfter})";
        var expectedChanges = new List<string> { rename, move1, move2, move3, move4, move5 };
        
        Assert.Equal(6, graphManipulator.Changes.Count);
        Assert.Contains(rename, graphManipulator.Changes);
        Assert.Contains(move1, graphManipulator.Changes);
        Assert.Contains(move2, graphManipulator.Changes);
        Assert.Contains(move3, graphManipulator.Changes);
        Assert.Contains(move4, graphManipulator.Changes);
        Assert.Contains(move5, graphManipulator.Changes);
        Assert.True(expectedChanges.SequenceEqual(graphManipulator.Changes));
    }

    private TestDataStoreFixture CreateDatastore()
    {
        return new TestDataStoreFixture();
    }

    public class TestDataStoreFixture
    {
        private const string ExpectedSqliteName = "TestSqlite";
        private const string ExpectedSchemaName = "TestSchema";
        private const string ExpectedTable1Name = "TestTable1";
        private const string ExpectedTable2Name = "TestTable2";
        private const string ExpectedColumnName = "TestColumn";
        private const string ExpectedPrimaryColumn1Name = "PrimaryColumn1";
        private const string ExpectedPrimaryColumn2Name = "PrimaryColumn2";

        public readonly Sqlite ExpectedSqlite;
        public readonly Schema ExpectedSchema;
        public readonly Table ExpectedTable1;
        public readonly Table ExpectedTable2;
        public readonly Column ExpectedColumn;
        public readonly Column ExpectedPrimaryColumn1;
        public readonly Column ExpectedPrimaryColumn2;

        public TestDataStoreFixture()
        {
            ExpectedSqlite = new Sqlite(ExpectedSqliteName, BaseUri);
            ExpectedSchema = new Schema(ExpectedSchemaName);
            ExpectedTable1 = new Table(ExpectedTable1Name);
            ExpectedTable2 = new Table(ExpectedTable2Name);
            ExpectedColumn = new Column(ExpectedColumnName, "INT");
            ExpectedPrimaryColumn1 = new Column(ExpectedPrimaryColumn1Name);
            ExpectedPrimaryColumn2 = new Column(ExpectedPrimaryColumn2Name);
            
            ExpectedSqlite.AddStructure(ExpectedSchema);
            ExpectedSchema.AddStructure(ExpectedTable1);
            ExpectedSchema.AddStructure(ExpectedTable2);
            ExpectedTable1.AddStructure(ExpectedColumn);
            
            ExpectedTable1.AddStructure(ExpectedPrimaryColumn1);
            ExpectedTable2.AddStructure(ExpectedPrimaryColumn2);
            ExpectedTable1.AddPrimaryKey(ExpectedPrimaryColumn1);
            ExpectedTable2.AddPrimaryKey(ExpectedPrimaryColumn2);
        }
    }
}