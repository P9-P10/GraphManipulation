﻿// See https://aka.ms/new-console-template for more information

using System.Data.SQLite;
using GraphManipulation.Extensions;
using GraphManipulation.Models.Stores;
using GraphManipulation.Ontologies;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Writing;

namespace GraphManipulation;

public static class Program
{
    private const string OptimizedDatabaseName = "OptimizedAdvancedDatabase.sqlite";
    private const string SimpleDatabaseName = "SimpleDatabase.sqlite";
    private const string OptimizedDatabasePath = $"/home/ane/Documents/GitHub/Legeplads/Databases/{OptimizedDatabaseName}";
    private const string SimpleDatabasePath = $"/home/ane/Documents/GitHub/Legeplads/Databases/{SimpleDatabaseName}";
    private const string BaseUri = "http://www.test.com/";
    private const string OutputFileName = "output.ttl";
    private const string OutputPath = $"/home/ane/Documents/GitHub/GraphManipulation/GraphManipulation/{OutputFileName}";
    private const string OntologyPath =
        "/home/ane/Documents/GitHub/GraphManipulation/GraphManipulation/Ontologies/datastore-description-language.ttl";
    
    public static void Main()
    {
        // Console.WriteLine();
        // var arguments = Environment.GetCommandLineArgs();
        // Console.WriteLine(string.Join(", ", arguments));
        CreateAndValidateGraph();
        // SparqlExperiment("SELECT * WHERE { ?s ?p ?o }");
        SparqlExperiment("SELECT ?datastore ?name WHERE { ?datastore a ddl:Datastore . ?datastore ddl:hasName ?name }");
        // SparqlExperiment("SELECT ?something ?name WHERE { ?something a ddl:Column . ?something ddl:Datastore ?name }");
    }

    private static void SparqlExperiment(string commandText)
    {
        var graph = new Graph();
        graph.LoadFromFile(OutputPath);
        
        IGraph ontology = new Graph();
        ontology.LoadFromFile(OntologyPath, new TurtleParser());

        graph.ValidateUsing(ontology);

        var queryString = new SparqlParameterizedString();
        queryString.Namespaces.AddNamespace(
            DataStoreDescriptionLanguage.OntologyPrefix, 
            DataStoreDescriptionLanguage.OntologyUri);
        queryString.CommandText = commandText;
        
        
        var parser = new SparqlQueryParser();
        var query = parser.ParseFromString(queryString);
        
        var tripleStore = new TripleStore();
        tripleStore.Add(graph);
        
        var dataset = new InMemoryDataset(tripleStore);
        
        var processor = new LeviathanQueryProcessor(dataset);
        
        var results = (processor.ProcessQuery(query) as SparqlResultSet)!;

        foreach (var result in results)
        {
            Console.WriteLine(result);
        }
    }

    private static void CreateAndValidateGraph()
    {
        using var optimizedConn = new SQLiteConnection($"Data Source={OptimizedDatabasePath}");
        using var simpleConn = new SQLiteConnection($"Data Source={SimpleDatabasePath}");

        var optimizedSqlite = new Sqlite("", BaseUri, optimizedConn);
        var simpleSqlite = new Sqlite("", BaseUri, simpleConn);

        optimizedSqlite.BuildFromDataSource();
        simpleSqlite.BuildFromDataSource();

        var optimizedGraph = optimizedSqlite.ToGraph();
        var simpleGraph = simpleSqlite.ToGraph();

        var combinedGraph = new Graph();
        combinedGraph.Merge(optimizedGraph);
        combinedGraph.Merge(simpleGraph);

        var writer = new CompressingTurtleWriter();
        
        writer.Save(combinedGraph, OutputPath);

        IGraph dataGraph = new Graph();
        dataGraph.LoadFromFile(OutputPath);

        IGraph ontology = new Graph();
        ontology.LoadFromFile(OntologyPath, new TurtleParser());

        var report = dataGraph.ValidateUsing(ontology);

        Validation.PrintValidationReport(report);
    }
}