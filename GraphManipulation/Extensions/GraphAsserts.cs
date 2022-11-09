using GraphManipulation.Models.Entity;
using GraphManipulation.Models.Structures;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace GraphManipulation.Extensions;

public static class GraphAsserts
{
    public static void AssertHasStructureTriple(this IGraph graph, Entity parent, Entity child)
    {
        graph.AssertSubjectPredicateObjectTriple(parent, "ddl:hasStructure", child);
    }

    public static void AssertTypeTriple(this IGraph graph, Entity entity)
    {
        var nodeType = graph.CreateUriNode(GraphDataType.GetGraphTypeString(entity.GetType()));
        graph.AssertSubjectPredicateObjectTriple(entity, "rdf:type", nodeType);
    }

    public static void AssertNameTriple(this IGraph graph, NamedEntity entity)
    {
        graph.AssertSubjectPredicateObjectTriple(entity, "ddl:hasName", entity.Name);
    }

    public static void AssertNamedEntityTriple(this IGraph graph, NamedEntity entity)
    {
        graph.AssertNameTriple(entity);
        graph.AssertTypeTriple(entity);
    }

    public static void AssertHasDataTypeTriple(this IGraph graph, Column column)
    {
        graph.AssertSubjectPredicateObjectTriple(column, "ddl:hasDataType", column.DataType);
    }

    public static void AssertIsNotNullTriple(this IGraph graph, Column column)
    {
        graph.AssertSubjectPredicateObjectTriple(
            column,
            "ddl:isNotNull",
            graph.CreateLiteralNode(column.IsNotNull.ToString().ToLower(),
                UriFactory.Create(XmlSpecsHelper.XmlSchemaDataTypeBoolean)));
    }

    public static void AssertHasStoreTriple(this IGraph graph, Structure structure)
    {
        graph.AssertSubjectPredicateObjectTriple(structure, "ddl:hasStore", structure.Store!);
    }

    public static void AssertOptionsTriple(this IGraph graph, Column column)
    {
        graph.AssertSubjectPredicateObjectTriple(column, "ddl:columnOptions", column.Options);
    }

    public static void AssertPrimaryKeys(this IGraph graph, Table table)
    {
        if (table.PrimaryKeys.Count == 0)
        {
            throw new DataStoreToGraphException("No primary keys when creating graph");
        }

        var subj = graph.CreateUriNode(table.Uri);
        var pred = graph.CreateUriNode("ddl:primaryKey");

        foreach (var primaryKey in table.PrimaryKeys)
        {
            var obj = graph.CreateUriNode(primaryKey.Uri);
            graph.Assert(subj, pred, obj);
        }
    }

    public static void AssertForeignKeys(this IGraph graph, Table table)
    {
        if (table.ForeignKeys.Count == 0)
        {
            return;
        }

        var tableUri = graph.CreateUriNode(table.Uri);
        var foreignKeyPredicate = graph.CreateUriNode("ddl:foreignKey");
        var referencesPredicate = graph.CreateUriNode("ddl:references");
        var foreignKeyOnDeletePredicate = graph.CreateUriNode("ddl:foreignKeyOnDelete");
        var foreignKeyOnUpdatePredicate = graph.CreateUriNode("ddl:foreignKeyOnUpdate");

        foreach (var foreignKey in table.ForeignKeys)
        {
            var from = graph.CreateUriNode(foreignKey.From.Uri);
            var to = graph.CreateUriNode(foreignKey.To.Uri);

            graph.Assert(tableUri, foreignKeyPredicate, from);
            graph.Assert(from, referencesPredicate, to);

            var onDelete = graph.CreateLiteralNode(foreignKey.OnDeleteString);
            var onUpdate = graph.CreateLiteralNode(foreignKey.OnUpdateString);

            graph.Assert(from, foreignKeyOnDeletePredicate, onDelete);
            graph.Assert(from, foreignKeyOnUpdatePredicate, onUpdate);
        }
    }

    private static void AssertSubjectPredicateObjectTriple(this IGraph graph, Entity entity, string predicate,
        string literal)
    {
        var subj = graph.CreateUriNode(entity.Uri);
        var pred = graph.CreateUriNode(predicate);
        var obj = graph.CreateLiteralNode(literal);

        graph.Assert(subj, pred, obj);
    }

    private static void AssertSubjectPredicateObjectTriple(this IGraph graph, Entity from, string predicate,
        Entity to)
    {
        var subj = graph.CreateUriNode(from.Uri);
        var pred = graph.CreateUriNode(predicate);
        var obj = graph.CreateUriNode(to.Uri);

        graph.Assert(subj, pred, obj);
    }

    private static void AssertSubjectPredicateObjectTriple(this IGraph graph, Entity from, string predicate, INode obj)
    {
        var subj = graph.CreateUriNode(from.Uri);
        var pred = graph.CreateUriNode(predicate);

        graph.Assert(subj, pred, obj);
    }
}