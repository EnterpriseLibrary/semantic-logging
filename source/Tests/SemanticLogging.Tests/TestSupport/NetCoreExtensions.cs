using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;

namespace EnterpriseLibrary.SemanticLogging.Tests
{
    public static class NetCoreExtensions
    {
        public static bool CreateIfNotExists(this CloudTable table, TableRequestOptions requestOptions = null, OperationContext operationContext = null) =>
            table.CreateIfNotExistsAsync(requestOptions, operationContext).Result;

        public static void Delete(this CloudTable table, TableRequestOptions requestOptions = null, OperationContext operationContext = null) =>
            table.DeleteAsync(requestOptions, operationContext).Wait();

        public static bool DeleteIfExists(this CloudTable table, TableRequestOptions requestOptions = null, OperationContext operationContext = null) =>
            table.DeleteIfExistsAsync(requestOptions, operationContext).Result;

        public static IEnumerable<TElement> ExecuteQuery<TElement>(this CloudTable table, TableQuery<TElement> query, TableRequestOptions requestOptions = null, OperationContext operationContext = null) where TElement : ITableEntity, new()
        {
            var results = new List<TElement>();
            var segment = table.ExecuteQuerySegmentedAsync(query, null, requestOptions, operationContext).Result;
            results.AddRange(segment.Results);
            while (segment.ContinuationToken != null)
            {
                segment = table.ExecuteQuerySegmentedAsync(query, segment.ContinuationToken, requestOptions, operationContext).Result;
                results.AddRange(segment.Results);
            }

            return results;
        }
    }
}
