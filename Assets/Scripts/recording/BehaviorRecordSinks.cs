using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

// Todo: Move sinks to own namespace recording.sink

namespace recording
{
    public interface IBehaviorRecordSink : IDisposable
    {
        void Append(BehaviorRecord record);
        void Flush();
    }

    public sealed class InMemoryBehaviorRecordSink : IBehaviorRecordSink
    {
        private readonly BehaviorRecord[] _buffer;
        private int _start;
        private int _count;

        public int Capacity => _buffer.Length;
        public int Count => _count;
        public IReadOnlyList<BehaviorRecord> Records
        {
            get
            {
                List<BehaviorRecord> snapshot = new List<BehaviorRecord>(_count);
                for (int i = 0; i < _count; i++)
                {
                    snapshot.Add(_buffer[(_start + i) % _buffer.Length]);
                }
                return snapshot;
            }
        }

        public InMemoryBehaviorRecordSink(int capacity = 4096)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _buffer = new BehaviorRecord[capacity];
        }

        public void Append(BehaviorRecord record)
        {
            if (_count < _buffer.Length)
            {
                _buffer[(_start + _count) % _buffer.Length] = record;
                _count++;
                return;
            }

            _buffer[_start] = record;
            _start = (_start + 1) % _buffer.Length;
        }

        public void Flush()
        {
        }

        public void Dispose()
        {
        }
    }

    public sealed class JsonLinesBehaviorRecordSink : IBehaviorRecordSink
    {
        private readonly StreamWriter _writer;
        private readonly JsonSerializerSettings _settings;
        private bool _disposed;

        public string FilePath { get; }

        public JsonLinesBehaviorRecordSink(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("A JSONL file path is required.", nameof(filePath));
            }

            string directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            FilePath = filePath;
            _settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None,
            };
            _settings.Converters.Add(new StringEnumConverter());
            _writer = new StreamWriter(
                new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read),
                new UTF8Encoding(false));
        }

        public void Append(BehaviorRecord record)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(JsonLinesBehaviorRecordSink));
            }

            _writer.WriteLine(JsonConvert.SerializeObject(record, _settings));
        }

        public void Flush()
        {
            if (!_disposed)
            {
                _writer.Flush();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                _writer.Dispose();
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}
