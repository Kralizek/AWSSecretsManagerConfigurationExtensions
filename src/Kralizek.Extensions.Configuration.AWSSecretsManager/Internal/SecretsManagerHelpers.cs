using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Amazon.Runtime;
using Amazon.SecretsManager.Model;

using Microsoft.Extensions.Configuration;

namespace Kralizek.Extensions.Configuration.Internal
{
    internal static class SecretsManagerHelpers
    {
        internal static bool TryParseJson(string data, out JsonElement? jsonElement)
        {
            jsonElement = null;
            data = data.TrimStart();
            var firstChar = data.FirstOrDefault();
            if (firstChar != '[' && firstChar != '{') return false;
            try
            {
                using var doc = JsonDocument.Parse(data);
                jsonElement = doc.RootElement.Clone();
                return true;
            }
            catch (JsonException) { return false; }
        }

        internal static IEnumerable<(string key, string value)> ExtractValues(JsonElement? jsonElement, string prefix)
        {
            if (jsonElement == null) yield break;
            var element = jsonElement.Value;
            switch (element.ValueKind)
            {
                case JsonValueKind.Array:
                    {
                        var i = 0;
                        foreach (var el in element.EnumerateArray())
                        {
                            var k = $"{prefix}{ConfigurationPath.KeyDelimiter}{i++}";
                            foreach (var pair in ExtractValues(el, k)) yield return pair;
                        }
                        break;
                    }
                case JsonValueKind.Number:
                    yield return (prefix, element.GetRawText());
                    break;
                case JsonValueKind.String:
                    yield return (prefix, element.GetString() ?? "");
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    yield return (prefix, element.GetBoolean().ToString());
                    break;
                case JsonValueKind.Object:
                    foreach (var prop in element.EnumerateObject())
                    {
                        var k = $"{prefix}{ConfigurationPath.KeyDelimiter}{prop.Name}";
                        foreach (var pair in ExtractValues(prop.Value, k)) yield return pair;
                    }
                    break;
                case JsonValueKind.Undefined:
                case JsonValueKind.Null:
                default:
                    throw new FormatException("unsupported json token");
            }
        }

        internal static bool DictionaryEquals(Dictionary<string, string?> a, Dictionary<string, string?> b)
        {
            if (a.Count != b.Count) return false;
            foreach (var kvp in a)
            {
                if (!b.TryGetValue(kvp.Key, out var bValue) || kvp.Value != bValue) return false;
            }
            return true;
        }

        internal static List<List<T>> ChunkList<T>(IEnumerable<T> source, int chunkSize)
            => source
                .Select((item, idx) => (item, idx))
                .GroupBy(x => x.idx / chunkSize)
                .Select(g => g.Select(x => x.item).ToList())
                .ToList();

        internal static List<Exception> HandleBatchErrors(BatchGetSecretValueResponse response)
        {
            return response.Errors.Select<APIErrorType, Exception>(err => err.ErrorCode switch
            {
                "DecryptionFailure" => new DecryptionFailureException(err.Message, ErrorType.Unknown,
                    err.ErrorCode, response.ResponseMetadata.RequestId, response.HttpStatusCode),
                "InternalServiceError" => new InternalServiceErrorException(err.Message, ErrorType.Unknown,
                    err.ErrorCode, response.ResponseMetadata.RequestId, response.HttpStatusCode),
                "InvalidParameterException" => new InvalidParameterException(err.Message, ErrorType.Unknown,
                    err.ErrorCode, response.ResponseMetadata.RequestId, response.HttpStatusCode),
                "InvalidRequestException" => new InvalidRequestException(err.Message, ErrorType.Unknown,
                    err.ErrorCode, response.ResponseMetadata.RequestId, response.HttpStatusCode),
                "ResourceNotFoundException" => new MissingSecretValueException(err.Message, err.SecretId, err.SecretId,
                    new ResourceNotFoundException(err.Message, ErrorType.Unknown, err.ErrorCode,
                        response.ResponseMetadata.RequestId, response.HttpStatusCode)),
                _ => new AmazonServiceException(err.Message, ErrorType.Unknown, err.ErrorCode,
                    response.ResponseMetadata.RequestId, response.HttpStatusCode)
            }).ToList();
        }
    }
}