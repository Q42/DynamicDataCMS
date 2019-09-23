﻿using System.IO;
using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace QMS.Storage.CosmosDB
{
    internal sealed class SystemTextJsonCosmosSerializer : CosmosSerializer
    {
        private readonly JsonSerializerOptions _options;

        internal SystemTextJsonCosmosSerializer(JsonSerializerOptions options)
        {
            _options = options;
        }

        /// <inheritdoc />
        public override T FromStream<T>(Stream stream)
        {
            // Have to dispose of the stream, otherwise the Cosmos SDK throws.
            // https://github.com/Azure/azure-cosmos-dotnet-v3/blob/0843cae3c252dd49aa8e392623d7eaaed7eb712b/Microsoft.Azure.Cosmos/src/Serializer/CosmosJsonSerializerWrapper.cs#L22
            // https://github.com/Azure/azure-cosmos-dotnet-v3/blob/0843cae3c252dd49aa8e392623d7eaaed7eb712b/Microsoft.Azure.Cosmos/src/Serializer/CosmosJsonDotNetSerializer.cs#L73
            using (stream)
            {
                // TODO Would be more efficient if CosmosSerializer supported async
                // See https://github.com/Azure/azure-cosmos-dotnet-v3/issues/715
                using var memory = new MemoryStream((int)stream.Length);
                stream.CopyTo(memory);

                byte[] utf8Json = memory.ToArray();

                return JsonSerializer.Deserialize<T>(utf8Json, _options);
            }
        }

        /// <inheritdoc />
        public override Stream ToStream<T>(T input)
        {
            byte[] utf8Json = JsonSerializer.SerializeToUtf8Bytes(input, _options);
            return new MemoryStream(utf8Json);
        }
    }
}
