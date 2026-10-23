using System.IO;
using PixelBrawl.AI;
using PixelBrawl.Combat;
using PixelBrawl.Match;
using PixelBrawl.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PixelBrawl.Editor
{
    [InitializeOnLoad]
    internal static class Stage5BotsBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/TrainingArena.unity";
        private const string PrefabPath = "Assets/_Game/Prefabs/EnemyBot.prefab";
        private const string MaterialPath = "Assets/_Game/Materials/EnemyBot.mat";

        static Stage5BotsBuilder()
        {
            EditorApplication.delayCall += UpgradeScene;
        }

        [MenuItem("Pixel Brawl/Stage 5/Install Bots and Objectives")]
        private static void UpgradeScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Path.GetFullPath(ScenePath))) return;

            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool alreadyLoaded = scene.IsValid() && scene.isLoaded;
            if (!alreadyLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            GameObject existingStage = FindRoot(scene, "BotsStage5");
            if (existingStage != null)
            {
                SceneManager.SetActiveScene(scene);
                CreateAndBakeNavMesh(existingStage.transform);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (!alreadyLoaded) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            SceneManager.SetActiveScene(scene);
            GameObject player = FindRoot(scene, "Player");
            RoundManager roundManager = FindComponent<RoundManager>(scene);
            MatchHud matchHud = FindComponent<MatchHud>(scene);
            Health playerHealth = player != null ? player.GetComponent<Health>() : null;
            if (player == null || playerHealth == null || roundManager == null || matchHud == null)
            {
                Debug.LogError("Stage 5 needs the existing Player, Health, RoundManager and MatchHud. No scene changes were made.");
                if (!alreadyLoaded) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            if (player.GetComponent<TeamMember>() == null)
            {
                TeamMember member = player.AddComponent<TeamMember>();
                SerializedObject serializedMember = new SerializedObject(member);
                serializedMember.FindProperty("team").enumValueIndex = (int)Team.Player;
                serializedMember.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject root = new GameObject("BotsStage5");
            SceneManager.MoveGameObjectToScene(root, scene);
            GameObject spawnsRoot = new GameObject("BotSpawns");
            spawnsRoot.transform.SetParent(root.transform);
            GameObject patrolRoot = new GameObject("PatrolPoints");
            patrolRoot.transform.SetParent(root.transform);

            Vector3[] spawnPositions =
            {
                new Vector3(-10f, 0f, 10f),
                new Vector3(0f, 0f, 12f),
                new Vector3(10f, 0f, 10f)
            };
            Transform[] spawns = new Transform[spawnPositions.Length];
            EnemyBotController[] bots = new EnemyBotController[spawnPositions.Length];
            GameObject prefab = CreateOrLoadBotPrefab();

            for (int index = 0; index < spawnPositions.Length; index++)
            {
                GameObject spawn = new GameObject($"EnemySpawn_{index + 1}");
                spawn.transform.SetParent(spawnsRoot.transform);
                spawn.transform.position = spawnPositions[index];
                spawn.transform.rotation = Quaternion.LookRotation((player.transform.position - spawn.transform.position).normalized);
                spawns[index] = spawn.transform;

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = $"EnemyBot_{index + 1}";
                instance.transform.SetParent(root.transform);
                instance.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
                bots[index] = instance.GetComponent<EnemyBotController>();

                Transform[] points = new Transform[3];
                Vector3[] offsets = { new Vector3(-3f, 0f, -2f), new Vector3(3f, 0f, -2f), new Vector3(0f, 0f, 2f) };
                for (int pointIndex = 0; pointIndex < points.Length; pointIndex++)
                {
                    GameObject point = new GameObject($"Bot{index + 1}_Patrol_{pointIndex + 1}");
                    point.transform.SetParent(patrolRoot.transform);
                    point.transform.position = spawnPositions[index] + offsets[pointIndex];
                    points[pointIndex] = point.transform;
                }
                SerializedObject serializedBot = new SerializedObject(bots[index]);
                SetObjectArray(serializedBot.FindProperty("patrolPoints"), points);
                serializedBot.ApplyModifiedPropertiesWithoutUndo();
            }

            RoundObjectiveController objectives = root.AddComponent<RoundObjectiveController>();
            SerializedObject serializedObjectives = new SerializedObject(objectives);
            serializedObjectives.FindProperty("roundManager").objectReferenceValue = roundManager;
            serializedObjectives.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            SetObjectArray(serializedObjectives.FindProperty("bots"), bots);
            SetObjectArray(serializedObjectives.FindProperty("botSpawns"), spawns);
            serializedObjectives.FindProperty("difficulty").enumValueIndex = (int)BotDifficulty.Normal;
            serializedObjectives.ApplyModifiedPropertiesWithoutUndo();

            CreateHud(matchHud.GetComponent<Canvas>().transform, objectives);
            CreateAndBakeNavMesh(root.transform);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (!alreadyLoaded) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("Pixel Brawl Stage 5 bots, objectives and baked NavMesh added to TrainingArena.");
        }

        [MenuItem("Pixel Brawl/Stage 5/Rebuild Arena NavMesh")]
        private static void RebuildArenaNavMesh()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before rebuilding the arena NavMesh.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            GameObject root = FindRoot(scene, "BotsStage5");
            if (root == null)
            {
                Debug.LogError("Install Stage 5 before rebuilding its NavMesh.");
                return;
            }

            CreateAndBakeNavMesh(root.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void CreateAndBakeNavMesh(Transform stageRoot)
        {
            EnemyBotController[] bots = stageRoot.GetComponentsInChildren<EnemyBotController>(true);
            NavMeshAgent[] agents = new NavMeshAgent[bots.Length];
            Collider[][] botColliders = new Collider[bots.Length][];
            for (int index = 0; index < bots.Length; index++)
            {
                agents[index] = bots[index].GetComponent<NavMeshAgent>();
                botColliders[index] = bots[index].GetComponentsInChildren<Collider>(true);
                if (agents[index] != null) agents[index].enabled = false;
                foreach (Collider botCollider in botColliders[index]) botCollider.enabled = false;
            }

            Transform existing = stageRoot.Find("Navigation");
            GameObject navigation = existing != null ? existing.gameObject : new GameObject("Navigation");
            navigation.transform.SetParent(stageRoot, false);
            NavMeshSurface surface = navigation.GetComponent<NavMeshSurface>();
            if (surface == null) surface = navigation.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            EditorUtility.SetDirty(surface);

            for (int index = 0; index < bots.Length; index++)
            {
                foreach (Collider botCollider in botColliders[index]) botCollider.enabled = true;
                if (agents[index] != null) agents[index].enabled = true;
            }
        }

        private static GameObject CreateOrLoadBotPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null) return existing;

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = "EnemyBot", color = new Color(0.85f, 0.18f, 0.22f) };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            GameObject root = new GameObject("EnemyBot");
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.center = Vector3.up;
            collider.height = 2f;
            collider.radius = 0.45f;
            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.enabled = false;
            agent.radius = 0.45f;
            agent.height = 2f;
            agent.angularSpeed = 540f;
            agent.acceleration = 16f;
            agent.stoppingDistance = 7f;

            Health health = root.AddComponent<Health>();
            SerializedObject serializedHealth = new SerializedObject(health);
            serializedHealth.FindProperty("maximumHealth").floatValue = 100f;
            serializedHealth.ApplyModifiedPropertiesWithoutUndo();
            TeamMember team = root.AddComponent<TeamMember>();
            SerializedObject serializedTeam = new SerializedObject(team);
            serializedTeam.FindProperty("team").enumValueIndex = (int)Team.Enemy;
            serializedTeam.ApplyModifiedPropertiesWithoutUndo();
            root.AddComponent<EnemyBotController>();

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.up;
            body.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            body.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(body.GetComponent<Collider>());
            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Visor";
            visor.transform.SetParent(root.transform, false);
            visor.transform.localPosition = new Vector3(0f, 1.55f, 0.38f);
            visor.transform.localScale = new Vector3(0.55f, 0.18f, 0.12f);
            visor.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(visor.GetComponent<Collider>());

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateHud(Transform canvas, RoundObjectiveController objectives)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text enemies = CreateText(canvas, "EnemiesAliveText", "ENEMIES  3 / 3", font, 22, new Vector2(0.5f, 1f), new Vector2(0f, -115f));
            Text difficulty = CreateText(canvas, "BotDifficultyText", "DIFFICULTY  NORMAL", font, 18, new Vector2(0.5f, 1f), new Vector2(0f, -145f));
            Text eliminated = CreateText(canvas, "BotEliminationText", "ENEMY ELIMINATED", font, 30, new Vector2(0.5f, 0.5f), new Vector2(0f, 145f));
            eliminated.color = new Color(1f, 0.35f, 0.2f);

            BotObjectiveHud hud = canvas.gameObject.AddComponent<BotObjectiveHud>();
            SerializedObject serialized = new SerializedObject(hud);
            serialized.FindProperty("objectives").objectReferenceValue = objectives;
            serialized.FindProperty("enemiesText").objectReferenceValue = enemies;
            serialized.FindProperty("difficultyText").objectReferenceValue = difficulty;
            serialized.FindProperty("eliminationText").objectReferenceValue = eliminated;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Text hint = CreateText(canvas, "Stage5DebugHint", "DEV: F10 COMBAT  F11 ELIMINATE BOTS  F12 RESTART ROUND  PGUP DIFFICULTY", font, 15, new Vector2(1f, 0f), new Vector2(-340f, 48f));
            hint.alignment = TextAnchor.MiddleRight;
        }

        private static Text CreateText(Transform parent, string name, string value, Font font, int size, Vector2 anchor, Vector2 position)
        {
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            item.transform.SetParent(parent, false);
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(680f, 38f);
            Text text = item.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root;
            return null;
        }

        private static T FindComponent<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T value = root.GetComponentInChildren<T>(true);
                if (value != null) return value;
            }
            return null;
        }

        private static void SetObjectArray<T>(SerializedProperty property, T[] values) where T : Object
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }
    }
}
