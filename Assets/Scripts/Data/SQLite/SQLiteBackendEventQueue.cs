using System;
using System.Collections.Generic;
using System.Linq;
using SQLite4Unity3d;
using UnityEngine;
using NavAR.Infrastructure.Backend;

namespace NavAR.Data.SQLite
{
    public sealed class SQLiteBackendEventQueue : IBackendEventQueue
    {
        private readonly SQLiteConnection _db;

        public SQLiteBackendEventQueue()
        {
            var dbPath = SQLitePaths.GetDatabasePath();
            _db = new SQLiteConnection(dbPath);
            _db.CreateTable<DbPendingBackendEvent>();
        }

        public void Enqueue(string eventType, string endpoint, string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                return;
            }

            var record = new DbPendingBackendEvent
            {
                event_type = eventType ?? string.Empty,
                endpoint = endpoint ?? string.Empty,
                payload_json = payloadJson,
                created_utc = DateTime.UtcNow.ToString("o"),
                last_attempt_utc = null,
                attempt_count = 0
            };

            _db.Insert(record);
        }

        public List<BackendQueuedEvent> DequeueBatch(int maxCount)
        {
            var records = _db.Table<DbPendingBackendEvent>()
                .ToList()
                .OrderBy(r => r.pending_id)
                .Take(Mathf.Max(1, maxCount))
                .ToList();

            var result = new List<BackendQueuedEvent>();
            foreach (var record in records)
            {
                result.Add(new BackendQueuedEvent
                {
                    Id = record.pending_id,
                    EventType = record.event_type,
                    Endpoint = record.endpoint,
                    PayloadJson = record.payload_json,
                    AttemptCount = record.attempt_count
                });
            }

            return result;
        }

        public void MarkAttempt(int id)
        {
            var record = _db.Table<DbPendingBackendEvent>()
                .FirstOrDefault(r => r.pending_id == id);
            if (record == null)
            {
                return;
            }

            record.attempt_count += 1;
            record.last_attempt_utc = DateTime.UtcNow.ToString("o");
            _db.Update(record);
        }

        public void Delete(int id)
        {
            _db.Execute("DELETE FROM pending_backend_events WHERE pending_id = ?", id);
        }

        public int Count()
        {
            return _db.Table<DbPendingBackendEvent>().Count();
        }
    }
}
