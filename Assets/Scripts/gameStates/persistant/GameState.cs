using action;
using System;
using System.Collections.Generic;
using System.IO;
using combat;
using enums;
using hook;
using random;
using recording;
using UnityEngine;

namespace gameStates.persistant
{
    public class GameState : IDisposable
    {
        public EnumDifficulty Difficulty;
        public SeedManager SeedManager = new SeedManager();
        public RandomManager RandomManager = new RandomManager();
        public GameActionManager GameActionManager;
        public BattleCommandManager BattleCommandManager;
        // Note: Holds no listeners of its own — just the scratch list it refills on every Fire.
        public HookManager HookManager = new HookManager();
        public ActionExecutor ActionExecutor;
        public BehaviorRecorder BehaviorRecorder;
        public InMemoryBehaviorRecordSink BehaviorRecordMemory;
        public bool PersistentBehaviorLogAvailable;
        public string BehaviorRecordFilePath;
        
        public bool IsInBattle = false;
        
        public void Init(MonoBehaviour unityBoostrap)
        {
            BehaviorRecorder?.Dispose();
            BehaviorRecordMemory = new InMemoryBehaviorRecordSink();
            PersistentBehaviorLogAvailable = false;
            BehaviorRecordFilePath = null;
            List<IBehaviorRecordSink> recordSinks = new List<IBehaviorRecordSink>
            {
                BehaviorRecordMemory,
            };

            try
            {
                string recordDirectory = Path.Combine(Application.persistentDataPath, "Records");
                string recordFileName = $"behavior-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}.jsonl";
                BehaviorRecordFilePath = Path.Combine(recordDirectory, recordFileName);
                recordSinks.Add(new JsonLinesBehaviorRecordSink(BehaviorRecordFilePath));
                PersistentBehaviorLogAvailable = true;
            }
            catch (Exception exception)
            {
                // Recording is observational: file-system failure falls back to the in-memory sink.
                BehaviorRecordFilePath = null;
                Debug.LogError($"[BehaviorRecorder] JSONL disabled: {exception.Message}");
            }

            BehaviorRecorder = new BehaviorRecorder(recordSinks.ToArray());
            GameActionManager = new GameActionManager(BehaviorRecorder);
            BattleCommandManager = new BattleCommandManager(BehaviorRecorder);
            ActionExecutor = new ActionExecutor(unityBoostrap, BehaviorRecorder);
            SeedManager.Init(41u); // Use 42u for testing. Todo: Let user choose the seed, or generate seed
            RandomManager.Init(SeedManager);
        }

        public void Dispose()
        {
            BehaviorRecorder?.Dispose();
        }
    }
}
