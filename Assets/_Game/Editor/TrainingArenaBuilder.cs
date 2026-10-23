using System.IO;
using System.Collections.Generic;
using PixelBrawl.Abilities;
using PixelBrawl.Combat;
using PixelBrawl.Economy;
using PixelBrawl.Inventory;
using PixelBrawl.Match;
using PixelBrawl.Player;
using PixelBrawl.Shop;
using PixelBrawl.UI;
using PixelBrawl.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace PixelBrawl.Editor
{
    [InitializeOnLoad]
    internal static class TrainingArenaBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/TrainingArena.unity";
        private const string MaterialsFolder = "Assets/_Game/Materials";
        private const string RifleDataPath = "Assets/_Game/Data/Weapons/StarterRifle.asset";
        private const string WeaponsDataFolder = "Assets/_Game/Data/Weapons";
        private const string ShopDataFolder = "Assets/_Game/Data/Shop";

        static TrainingArenaBuilder()
        {
            if (!File.Exists(Path.GetFullPath(ScenePath)))
            {
                EditorApplication.delayCall += BuildScene;
            }
            else
            {
                EditorApplication.delayCall += UpgradeExistingScene;
            }
        }

        [MenuItem("Pixel Brawl/Rebuild Training Arena")]
        private static void RebuildScene()
        {
            if (EditorUtility.DisplayDialog(
                    "Rebuild Training Arena",
                    "This replaces the generated TrainingArena scene. Continue?",
                    "Rebuild",
                    "Cancel"))
            {
                BuildScene();
            }
        }

        private static void BuildScene()
        {
            Scene previousScene = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            scene.name = "TrainingArena";
            SceneManager.SetActiveScene(scene);

            Material floorMaterial = CreateOrLoadMaterial("ArenaFloor", new Color(0.12f, 0.16f, 0.22f));
            Material wallMaterial = CreateOrLoadMaterial("ArenaWalls", new Color(0.22f, 0.28f, 0.36f));
            Material obstacleMaterial = CreateOrLoadMaterial("ArenaObstacles", new Color(0.95f, 0.42f, 0.16f));

            CreateEnvironment(floorMaterial, wallMaterial, obstacleMaterial);
            CreateLighting();
            CreatePlayer();
            CreateCombatStage(scene);
            CreateAbilityStage(scene);
            CreateEconomyStage(scene);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();

            if (previousScene.IsValid() && previousScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousScene);
            }

            EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Pixel Brawl training arena created at {ScenePath}");
        }

        private static void UpgradeExistingScene()
        {
            Scene previousScene = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool wasAlreadyLoaded = scene.IsValid() && scene.isLoaded;

            if (!wasAlreadyLoaded)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            bool sceneChanged = false;
            if (FindRoot(scene, "CombatStage2") == null)
            {
                SceneManager.SetActiveScene(scene);
                CreateCombatStage(scene);
                sceneChanged = true;
                Debug.Log("Pixel Brawl combat stage added to TrainingArena.");
            }

            if (FindRoot(scene, "AbilityStage3") == null)
            {
                SceneManager.SetActiveScene(scene);
                CreateAbilityStage(scene);
                sceneChanged = true;
                Debug.Log("Pixel Brawl ability stage added to TrainingArena.");
            }

            if (FindRoot(scene, "EconomyStage4") == null)
            {
                SceneManager.SetActiveScene(scene);
                CreateEconomyStage(scene);
                sceneChanged = true;
                Debug.Log("Pixel Brawl economy, shop and round stage added to TrainingArena.");
            }

            if (sceneChanged)
            {
                EditorSceneManager.SaveScene(scene);
                AddSceneToBuildSettings();
                AssetDatabase.SaveAssets();
            }

            if (previousScene.IsValid() && previousScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousScene);
            }

            if (!wasAlreadyLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void CreateEnvironment(Material floorMaterial, Material wallMaterial, Material obstacleMaterial)
        {
            GameObject environment = new GameObject("Environment");

            CreateCube("Floor", new Vector3(0f, -0.25f, 0f), new Vector3(30f, 0.5f, 30f), floorMaterial, environment.transform);
            CreateCube("NorthWall", new Vector3(0f, 2f, 15.25f), new Vector3(30.5f, 4f, 0.5f), wallMaterial, environment.transform);
            CreateCube("SouthWall", new Vector3(0f, 2f, -15.25f), new Vector3(30.5f, 4f, 0.5f), wallMaterial, environment.transform);
            CreateCube("EastWall", new Vector3(15.25f, 2f, 0f), new Vector3(0.5f, 4f, 30.5f), wallMaterial, environment.transform);
            CreateCube("WestWall", new Vector3(-15.25f, 2f, 0f), new Vector3(0.5f, 4f, 30.5f), wallMaterial, environment.transform);

            GameObject obstacles = new GameObject("Obstacles");
            obstacles.transform.SetParent(environment.transform);
            CreateCube("CenterCover", new Vector3(0f, 1f, 2f), new Vector3(5f, 2f, 1f), obstacleMaterial, obstacles.transform);
            CreateCube("WestCover", new Vector3(-7f, 1.25f, -4f), new Vector3(2f, 2.5f, 4f), obstacleMaterial, obstacles.transform);
            CreateCube("EastCover", new Vector3(7f, 0.75f, -2f), new Vector3(4f, 1.5f, 2f), obstacleMaterial, obstacles.transform);
            CreateCube("NorthCover", new Vector3(4f, 1.5f, 8f), new Vector3(2f, 3f, 2f), obstacleMaterial, obstacles.transform);
        }

        private static void CreateLighting()
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light directionalLight = lightObject.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            directionalLight.intensity = 1.2f;
            directionalLight.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.35f, 0.42f, 0.55f);
            RenderSettings.ambientEquatorColor = new Color(0.18f, 0.2f, 0.25f);
            RenderSettings.ambientGroundColor = new Color(0.08f, 0.09f, 0.12f);
        }

        private static void CreatePlayer()
        {
            Vector3 spawnPosition = new Vector3(0f, 0.02f, -10f);

            GameObject spawnPoint = new GameObject("PlayerSpawn");
            spawnPoint.transform.position = spawnPosition;

            GameObject player = new GameObject("Player");
            player.transform.position = spawnPosition;
            player.tag = "Player";

            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 50f;

            GameObject cameraObject = new GameObject("FirstPersonCamera");
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 75f;
            camera.nearClipPlane = 0.05f;
            cameraObject.AddComponent<AudioListener>();

            player.AddComponent<FpsMovement>();
            player.AddComponent<FpsLook>();
            player.AddComponent<CursorLockController>();
        }

        private static void CreateCombatStage(Scene scene)
        {
            GameObject player = FindRoot(scene, "Player");
            if (player == null)
            {
                Debug.LogError("TrainingArena does not contain a root Player object. Combat stage was not added.");
                return;
            }

            Camera playerCamera = player.GetComponentInChildren<Camera>();
            FpsLook fpsLook = player.GetComponent<FpsLook>();
            if (playerCamera == null || fpsLook == null)
            {
                Debug.LogError("Player camera or FpsLook is missing. Combat stage was not added.");
                return;
            }

            GameObject stageRoot = new GameObject("CombatStage2");
            SceneManager.MoveGameObjectToScene(stageRoot, scene);

            Health playerHealth = player.GetComponent<Health>();
            if (playerHealth == null)
            {
                playerHealth = player.AddComponent<Health>();
            }

            WeaponData rifleData = CreateOrLoadRifleData();
            HitscanWeapon rifle = CreateRifle(playerCamera.transform, fpsLook, rifleData);
            CreateTargets(stageRoot.transform);
            CreateHud(stageRoot.transform, rifle, playerHealth);
        }

        private static WeaponData CreateOrLoadRifleData()
        {
            WeaponData data = AssetDatabase.LoadAssetAtPath<WeaponData>(RifleDataPath);
            if (data != null)
            {
                return data;
            }

            data = ScriptableObject.CreateInstance<WeaponData>();
            data.name = "StarterRifle";
            AssetDatabase.CreateAsset(data, RifleDataPath);
            return data;
        }

        private static HitscanWeapon CreateRifle(Transform cameraTransform, FpsLook fpsLook, WeaponData rifleData)
        {
            Material rifleMaterial = CreateOrLoadMaterial("StarterRifle", new Color(0.12f, 0.13f, 0.16f));
            Material accentMaterial = CreateOrLoadMaterial("StarterRifleAccent", new Color(0.15f, 0.65f, 0.95f));
            Material flashMaterial = CreateOrLoadMaterial("MuzzleFlash", new Color(1f, 0.65f, 0.08f));
            Material impactMaterial = CreateOrLoadMaterial("BulletImpact", new Color(1f, 0.85f, 0.25f));

            GameObject rifleObject = new GameObject("StarterRifle");
            rifleObject.transform.SetParent(cameraTransform, false);
            rifleObject.transform.localPosition = new Vector3(0.32f, -0.28f, 0.65f);

            CreateVisualCube("Receiver", new Vector3(0f, 0f, 0.2f), new Vector3(0.18f, 0.16f, 0.55f), rifleMaterial, rifleObject.transform);
            CreateVisualCube("Barrel", new Vector3(0f, 0.02f, 0.65f), new Vector3(0.07f, 0.07f, 0.45f), accentMaterial, rifleObject.transform);
            CreateVisualCube("Grip", new Vector3(0f, -0.16f, 0.1f), new Vector3(0.1f, 0.25f, 0.13f), rifleMaterial, rifleObject.transform);

            GameObject muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(rifleObject.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, 0.02f, 0.9f);

            GameObject muzzleFlash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            muzzleFlash.name = "MuzzleFlash";
            muzzleFlash.transform.SetParent(muzzle.transform, false);
            muzzleFlash.transform.localScale = new Vector3(0.13f, 0.13f, 0.22f);
            muzzleFlash.GetComponent<Renderer>().sharedMaterial = flashMaterial;
            Object.DestroyImmediate(muzzleFlash.GetComponent<Collider>());
            muzzleFlash.SetActive(false);

            HitscanWeapon rifle = rifleObject.AddComponent<HitscanWeapon>();
            SerializedObject serializedRifle = new SerializedObject(rifle);
            serializedRifle.FindProperty("weaponData").objectReferenceValue = rifleData;
            serializedRifle.FindProperty("playerCamera").objectReferenceValue = cameraTransform.GetComponent<Camera>();
            serializedRifle.FindProperty("weaponVisual").objectReferenceValue = rifleObject.transform;
            serializedRifle.FindProperty("muzzle").objectReferenceValue = muzzle.transform;
            serializedRifle.FindProperty("muzzleFlash").objectReferenceValue = muzzleFlash;
            serializedRifle.FindProperty("impactMaterial").objectReferenceValue = impactMaterial;
            serializedRifle.FindProperty("fpsLook").objectReferenceValue = fpsLook;
            serializedRifle.ApplyModifiedPropertiesWithoutUndo();
            return rifle;
        }

        private static void CreateTargets(Transform stageRoot)
        {
            GameObject targetsRoot = new GameObject("TrainingTargets");
            targetsRoot.transform.SetParent(stageRoot);

            CreateTarget("StandardTarget_100HP", new Vector3(-7f, 1.25f, 7f), 100f, 0f, targetsRoot.transform);
            CreateTarget("ArmoredTarget_200HP", new Vector3(0f, 1.25f, 10f), 200f, 0f, targetsRoot.transform);
            CreateTarget("RespawningTarget_100HP", new Vector3(7f, 1.25f, 7f), 100f, 3f, targetsRoot.transform);
        }

        private static void CreateTarget(string name, Vector3 position, float maximumHealth, float respawnDelay, Transform parent)
        {
            Material targetMaterial = CreateOrLoadMaterial("TrainingTarget", new Color(0.15f, 0.8f, 0.3f));
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            target.name = name;
            target.transform.SetParent(parent);
            target.transform.position = position;
            target.transform.localScale = new Vector3(1.15f, 1.25f, 0.55f);
            target.GetComponent<Renderer>().sharedMaterial = targetMaterial;

            Health health = target.AddComponent<Health>();
            SerializedObject serializedHealth = new SerializedObject(health);
            serializedHealth.FindProperty("maximumHealth").floatValue = maximumHealth;
            serializedHealth.ApplyModifiedPropertiesWithoutUndo();

            TrainingTarget trainingTarget = target.AddComponent<TrainingTarget>();
            SerializedObject serializedTarget = new SerializedObject(trainingTarget);
            serializedTarget.FindProperty("respawnDelay").floatValue = respawnDelay;
            serializedTarget.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateHud(Transform stageRoot, HitscanWeapon weapon, Health playerHealth)
        {
            GameObject canvasObject = new GameObject("CombatHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(stageRoot);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text crosshair = CreateText(canvasObject.transform, "Crosshair", "+", font, 30, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));
            crosshair.color = Color.white;

            Text hitmarker = CreateText(canvasObject.transform, "Hitmarker", "×", font, 42, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 80f));
            hitmarker.color = Color.white;

            Text healthText = CreateText(canvasObject.transform, "HealthText", "HP  100 / 100", font, 28, TextAnchor.MiddleLeft,
                Vector2.zero, new Vector2(140f, 55f), new Vector2(260f, 55f));
            Text ammoText = CreateText(canvasObject.transform, "AmmoText", "30 / 090", font, 34, TextAnchor.MiddleRight,
                new Vector2(1f, 0f), new Vector2(-150f, 60f), new Vector2(260f, 60f));
            Text reloadText = CreateText(canvasObject.transform, "ReloadText", "RELOADING...", font, 26, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(300f, 50f));
            reloadText.color = new Color(1f, 0.75f, 0.2f);
            Text eliminationText = CreateText(canvasObject.transform, "EliminationText", "TARGET ELIMINATED", font, 28, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(420f, 55f));
            eliminationText.color = new Color(1f, 0.35f, 0.2f);

            CombatHud hud = canvasObject.AddComponent<CombatHud>();
            SerializedObject serializedHud = new SerializedObject(hud);
            serializedHud.FindProperty("weapon").objectReferenceValue = weapon;
            serializedHud.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            serializedHud.FindProperty("healthText").objectReferenceValue = healthText;
            serializedHud.FindProperty("ammoText").objectReferenceValue = ammoText;
            serializedHud.FindProperty("reloadText").objectReferenceValue = reloadText;
            serializedHud.FindProperty("hitmarkerText").objectReferenceValue = hitmarker;
            serializedHud.FindProperty("eliminationText").objectReferenceValue = eliminationText;
            serializedHud.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateAbilityStage(Scene scene)
        {
            GameObject player = FindRoot(scene, "Player");
            HitscanWeapon rifle = FindComponentInScene<HitscanWeapon>(scene);
            CombatHud hud = FindComponentInScene<CombatHud>(scene);
            Camera playerCamera = player != null ? player.GetComponentInChildren<Camera>() : null;

            if (player == null || rifle == null || hud == null || playerCamera == null)
            {
                Debug.LogError("Stage 3 requires Player, StarterRifle, CombatHud and FirstPersonCamera from the earlier stages.");
                return;
            }

            GameObject stageRoot = new GameObject("AbilityStage3");
            SceneManager.MoveGameObjectToScene(stageRoot, scene);

            DashAbility dashAbility = player.GetComponent<DashAbility>();
            if (dashAbility == null)
            {
                dashAbility = player.AddComponent<DashAbility>();
            }

            GameObject shieldGlow = new GameObject("ShieldGlow");
            shieldGlow.transform.SetParent(player.transform, false);
            shieldGlow.transform.localPosition = new Vector3(0f, 1f, 0f);
            Light shieldLight = shieldGlow.AddComponent<Light>();
            shieldLight.type = LightType.Point;
            shieldLight.color = new Color(0.15f, 0.65f, 1f);
            shieldLight.intensity = 2f;
            shieldLight.range = 3f;
            shieldGlow.SetActive(false);

            ShieldAbility shieldAbility = player.GetComponent<ShieldAbility>();
            if (shieldAbility == null)
            {
                shieldAbility = player.AddComponent<ShieldAbility>();
            }
            SerializedObject serializedShield = new SerializedObject(shieldAbility);
            serializedShield.FindProperty("shieldVisual").objectReferenceValue = shieldGlow;
            serializedShield.ApplyModifiedPropertiesWithoutUndo();

            Material armMaterial = CreateOrLoadMaterial("MeleeArm", new Color(0.72f, 0.42f, 0.25f));
            GameObject meleeArm = CreateVisualCube("MeleeArm", new Vector3(-0.28f, -0.3f, 0.55f),
                new Vector3(0.18f, 0.18f, 0.6f), armMaterial, playerCamera.transform);
            meleeArm.SetActive(false);

            MeleeAttack meleeAttack = player.GetComponent<MeleeAttack>();
            if (meleeAttack == null)
            {
                meleeAttack = player.AddComponent<MeleeAttack>();
            }
            SerializedObject serializedMelee = new SerializedObject(meleeAttack);
            serializedMelee.FindProperty("playerCamera").objectReferenceValue = playerCamera;
            serializedMelee.FindProperty("weapon").objectReferenceValue = rifle;
            serializedMelee.FindProperty("visualArm").objectReferenceValue = meleeArm.transform;
            serializedMelee.ApplyModifiedPropertiesWithoutUndo();

            CreateDamageTestZone(stageRoot.transform);
            ExtendHudForAbilities(hud, dashAbility, shieldAbility, meleeAttack);
        }

        private static void CreateDamageTestZone(Transform stageRoot)
        {
            Material damageMaterial = CreateOrLoadMaterial("DamageTestZone", new Color(0.8f, 0.05f, 0.05f));
            GameObject zone = CreateCube("DamageTestZone", new Vector3(0f, 0.06f, -5.5f),
                new Vector3(3f, 0.12f, 3f), damageMaterial, stageRoot);
            BoxCollider collider = zone.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            Rigidbody body = zone.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            zone.AddComponent<TrainingDamageZone>();
        }

        private static void ExtendHudForAbilities(CombatHud hud, DashAbility dashAbility, ShieldAbility shieldAbility,
            MeleeAttack meleeAttack)
        {
            Transform canvasTransform = hud.transform;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Text shieldText = CreateText(canvasTransform, "ShieldText", "SHIELD  0 / 50", font, 24,
                TextAnchor.MiddleLeft, Vector2.zero, new Vector2(150f, 95f), new Vector2(300f, 45f));
            shieldText.color = new Color(0.2f, 0.75f, 1f);

            Text dashText = CreateText(canvasTransform, "DashStatusText", "SHIFT  DASH READY", font, 22,
                TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(170f, -55f), new Vector2(320f, 45f));
            Text shieldStatusText = CreateText(canvasTransform, "ShieldStatusText", "Q  SHIELD READY", font, 22,
                TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(170f, -95f), new Vector2(320f, 45f));
            shieldStatusText.color = new Color(0.2f, 0.75f, 1f);

            Text meleeFeedback = CreateText(canvasTransform, "MeleeFeedbackText", "PUNCH HIT", font, 24,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -155f), new Vector2(300f, 45f));
            meleeFeedback.color = new Color(1f, 0.65f, 0.2f);

            SerializedObject serializedHud = new SerializedObject(hud);
            serializedHud.FindProperty("dashAbility").objectReferenceValue = dashAbility;
            serializedHud.FindProperty("shieldAbility").objectReferenceValue = shieldAbility;
            serializedHud.FindProperty("meleeAttack").objectReferenceValue = meleeAttack;
            serializedHud.FindProperty("shieldText").objectReferenceValue = shieldText;
            serializedHud.FindProperty("dashStatusText").objectReferenceValue = dashText;
            serializedHud.FindProperty("shieldStatusText").objectReferenceValue = shieldStatusText;
            serializedHud.FindProperty("meleeFeedbackText").objectReferenceValue = meleeFeedback;
            serializedHud.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateEconomyStage(Scene scene)
        {
            GameObject player = FindRoot(scene, "Player");
            GameObject spawnPoint = FindRoot(scene, "PlayerSpawn");
            HitscanWeapon weapon = FindComponentInScene<HitscanWeapon>(scene);
            CombatHud combatHud = FindComponentInScene<CombatHud>(scene);
            DashAbility dashAbility = player != null ? player.GetComponent<DashAbility>() : null;
            ShieldAbility shieldAbility = player != null ? player.GetComponent<ShieldAbility>() : null;
            Health playerHealth = player != null ? player.GetComponent<Health>() : null;

            if (player == null || spawnPoint == null || weapon == null || combatHud == null || dashAbility == null ||
                shieldAbility == null || playerHealth == null)
            {
                Debug.LogError("Stage 4 requires the completed Player, combat HUD and ability components.");
                return;
            }

            GameObject stageRoot = new GameObject("EconomyStage4");
            SceneManager.MoveGameObjectToScene(stageRoot, scene);

            PlayerActionGate actionGate = player.GetComponent<PlayerActionGate>() ?? player.AddComponent<PlayerActionGate>();
            EquipmentShield equipmentShield = player.GetComponent<EquipmentShield>() ?? player.AddComponent<EquipmentShield>();

            WeaponData basicPistol = CreateOrUpdateWeaponData("BasicPistol", "Basic Pistol", 20f, 400f, 80f, 12, 48, 1.6f, 1, 0.2f, 0.45f);
            WeaponData heavyPistol = CreateOrUpdateWeaponData("HeavyPistol", "Heavy Pistol", 45f, 240f, 80f, 8, 32, 1.8f, 1, 0.1f, 1.2f);
            WeaponData smg = CreateOrUpdateWeaponData("Smg", "SMG", 18f, 850f, 70f, 30, 120, 1.8f, 1, 1f, 0.5f);
            WeaponData shotgun = CreateOrUpdateWeaponData("Shotgun", "Shotgun", 12f, 75f, 35f, 6, 24, 2.5f, 8, 5f, 1.8f);
            WeaponData rifle = CreateOrUpdateWeaponData("StarterRifle", "Rifle", 25f, 600f, 100f, 30, 90, 2f, 1, 0.15f, 0.75f);

            InventoryController inventory = player.GetComponent<InventoryController>() ?? player.AddComponent<InventoryController>();
            SerializedObject serializedInventory = new SerializedObject(inventory);
            serializedInventory.FindProperty("weapon").objectReferenceValue = weapon;
            serializedInventory.FindProperty("startingSecondaryWeapon").objectReferenceValue = basicPistol;
            serializedInventory.FindProperty("actionGate").objectReferenceValue = actionGate;
            serializedInventory.ApplyModifiedPropertiesWithoutUndo();

            ShopItemData[] items =
            {
                CreateOrUpdateShopItem("BasicPistol", "Basic Pistol", ShopCategory.Pistols, ShopPurchaseType.Weapon, 0, basicPistol, WeaponSlot.Secondary, 0f, 1),
                CreateOrUpdateShopItem("HeavyPistol", "Heavy Pistol", ShopCategory.Pistols, ShopPurchaseType.Weapon, 800, heavyPistol, WeaponSlot.Secondary, 0f, 1),
                CreateOrUpdateShopItem("Smg", "SMG", ShopCategory.Smgs, ShopPurchaseType.Weapon, 1200, smg, WeaponSlot.Primary, 0f, 1),
                CreateOrUpdateShopItem("Shotgun", "Shotgun", ShopCategory.Shotguns, ShopPurchaseType.Weapon, 1500, shotgun, WeaponSlot.Primary, 0f, 1),
                CreateOrUpdateShopItem("Rifle", "Rifle", ShopCategory.Rifles, ShopPurchaseType.Weapon, 2900, rifle, WeaponSlot.Primary, 0f, 1),
                CreateOrUpdateShopItem("LightShield", "Light Shield", ShopCategory.Shields, ShopPurchaseType.EquipmentShield, 400, null, WeaponSlot.Melee, 25f, 1),
                CreateOrUpdateShopItem("HeavyShield", "Heavy Shield", ShopCategory.Shields, ShopPurchaseType.EquipmentShield, 1000, null, WeaponSlot.Melee, 50f, 1),
                CreateOrUpdateShopItem("DashCharge", "Dash Charge", ShopCategory.Abilities, ShopPurchaseType.DashCharge, 300, null, WeaponSlot.Melee, 0f, 1),
                CreateOrUpdateShopItem("ShieldCharge", "Special Shield Charge", ShopCategory.Abilities, ShopPurchaseType.SpecialShieldCharge, 500, null, WeaponSlot.Melee, 0f, 1)
            };

            EconomyManager economy = stageRoot.AddComponent<EconomyManager>();
            RoundManager roundManager = stageRoot.AddComponent<RoundManager>();
            ShopController shop = stageRoot.AddComponent<ShopController>();

            TrainingTarget[] targets = FindComponentsInScene<TrainingTarget>(scene);
            Health[] combatHealth = new Health[targets.Length + 1];
            combatHealth[0] = playerHealth;
            for (int index = 0; index < targets.Length; index++) combatHealth[index + 1] = targets[index].GetComponent<Health>();

            SerializedObject serializedRound = new SerializedObject(roundManager);
            serializedRound.FindProperty("economy").objectReferenceValue = economy;
            serializedRound.FindProperty("inventory").objectReferenceValue = inventory;
            serializedRound.FindProperty("actionGate").objectReferenceValue = actionGate;
            serializedRound.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            serializedRound.FindProperty("equipmentShield").objectReferenceValue = equipmentShield;
            serializedRound.FindProperty("shieldAbility").objectReferenceValue = shieldAbility;
            serializedRound.FindProperty("dashAbility").objectReferenceValue = dashAbility;
            serializedRound.FindProperty("weapon").objectReferenceValue = weapon;
            serializedRound.FindProperty("playerTransform").objectReferenceValue = player.transform;
            serializedRound.FindProperty("playerSpawn").objectReferenceValue = spawnPoint.transform;
            serializedRound.FindProperty("characterController").objectReferenceValue = player.GetComponent<CharacterController>();
            SetObjectArray(serializedRound.FindProperty("combatHealth"), combatHealth);
            SetObjectArray(serializedRound.FindProperty("trainingTargets"), targets);
            serializedRound.ApplyModifiedPropertiesWithoutUndo();

            CreateEventSystemIfMissing(scene, stageRoot.transform);
            CreateShopAndMatchHud(combatHud.transform, shop, roundManager, economy, inventory, equipmentShield,
                dashAbility, shieldAbility, actionGate, items);
        }

        private static void CreateShopAndMatchHud(Transform canvasTransform, ShopController shop, RoundManager roundManager,
            EconomyManager economy, InventoryController inventory, EquipmentShield equipmentShield,
            DashAbility dashAbility, ShieldAbility shieldAbility, PlayerActionGate actionGate, ShopItemData[] items)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject panel = new GameObject("ShopPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(canvasTransform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(1100f, 820f);
            panel.GetComponent<Image>().color = new Color(0.035f, 0.05f, 0.08f, 0.97f);

            CreateText(panel.transform, "ShopTitle", "TACTICAL SHOP  [B TO CLOSE]", font, 34, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -45f), new Vector2(700f, 55f));
            Text balance = CreateText(panel.transform, "ShopBalance", "BALANCE: 800 CR", font, 28, TextAnchor.MiddleRight,
                new Vector2(1f, 1f), new Vector2(-190f, -95f), new Vector2(360f, 50f));
            Text feedback = CreateText(panel.transform, "ShopFeedback", string.Empty, font, 24, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0f, 35f), new Vector2(700f, 45f));
            feedback.color = new Color(1f, 0.6f, 0.2f);

            SerializedObject serializedShop = new SerializedObject(shop);
            serializedShop.FindProperty("roundManager").objectReferenceValue = roundManager;
            serializedShop.FindProperty("economy").objectReferenceValue = economy;
            serializedShop.FindProperty("inventory").objectReferenceValue = inventory;
            serializedShop.FindProperty("equipmentShield").objectReferenceValue = equipmentShield;
            serializedShop.FindProperty("dashAbility").objectReferenceValue = dashAbility;
            serializedShop.FindProperty("shieldAbility").objectReferenceValue = shieldAbility;
            serializedShop.FindProperty("actionGate").objectReferenceValue = actionGate;
            serializedShop.FindProperty("shopPanel").objectReferenceValue = panel;
            serializedShop.FindProperty("balanceText").objectReferenceValue = balance;
            serializedShop.FindProperty("feedbackText").objectReferenceValue = feedback;
            SetObjectArray(serializedShop.FindProperty("items"), items);
            serializedShop.ApplyModifiedPropertiesWithoutUndo();

            for (int index = 0; index < items.Length; index++)
            {
                float y = -155f - index * 62f;
                CreateShopButton(panel.transform, shop, items[index], index, y, font);
            }

            Text credits = CreateText(canvasTransform, "CreditsText", "800 CR", font, 26, TextAnchor.MiddleRight,
                new Vector2(1f, 1f), new Vector2(-105f, -45f), new Vector2(200f, 45f));
            Text round = CreateText(canvasTransform, "RoundText", "ROUND 1", font, 24, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(-150f, -38f), new Vector2(220f, 40f));
            Text score = CreateText(canvasTransform, "ScoreText", "0  -  0", font, 30, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(180f, 45f));
            Text state = CreateText(canvasTransform, "MatchStateText", "BUY PHASE", font, 22, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(150f, -38f), new Vector2(240f, 40f));
            Text timer = CreateText(canvasTransform, "MatchTimerText", "00:20", font, 28, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(180f, 45f));
            Text weaponText = CreateText(canvasTransform, "EquippedWeaponText", "BASIC PISTOL", font, 22, TextAnchor.MiddleRight,
                new Vector2(1f, 0f), new Vector2(-155f, 110f), new Vector2(300f, 40f));
            Text armorText = CreateText(canvasTransform, "EquipmentShieldText", "ARMOR  0 / 0", font, 22, TextAnchor.MiddleLeft,
                Vector2.zero, new Vector2(150f, 135f), new Vector2(300f, 40f));
            armorText.color = new Color(0.35f, 0.85f, 1f);
            Text chargesText = CreateText(canvasTransform, "AbilityChargesText", "CHARGES  DASH 1/3  |  SHIELD 1/3", font, 20,
                TextAnchor.MiddleLeft, Vector2.zero, new Vector2(220f, 175f), new Vector2(440f, 40f));
            Text roundResult = CreateText(canvasTransform, "RoundResultText", "ROUND WON", font, 44, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(520f, 70f));
            Text debugHintText = CreateText(canvasTransform, "DebugControlsHint", "DEV: F5 START  F6 WIN  F7 LOSS  F8 +1000 CR  F9 RESET", font, 16,
                TextAnchor.MiddleRight, new Vector2(1f, 0f), new Vector2(-285f, 20f), new Vector2(560f, 30f));

            MatchHud matchHud = canvasTransform.gameObject.AddComponent<MatchHud>();
            SerializedObject serializedHud = new SerializedObject(matchHud);
            serializedHud.FindProperty("roundManager").objectReferenceValue = roundManager;
            serializedHud.FindProperty("economy").objectReferenceValue = economy;
            serializedHud.FindProperty("inventory").objectReferenceValue = inventory;
            serializedHud.FindProperty("equipmentShield").objectReferenceValue = equipmentShield;
            serializedHud.FindProperty("dashAbility").objectReferenceValue = dashAbility;
            serializedHud.FindProperty("shieldAbility").objectReferenceValue = shieldAbility;
            serializedHud.FindProperty("creditsText").objectReferenceValue = credits;
            serializedHud.FindProperty("roundText").objectReferenceValue = round;
            serializedHud.FindProperty("scoreText").objectReferenceValue = score;
            serializedHud.FindProperty("stateText").objectReferenceValue = state;
            serializedHud.FindProperty("timerText").objectReferenceValue = timer;
            serializedHud.FindProperty("weaponText").objectReferenceValue = weaponText;
            serializedHud.FindProperty("equipmentShieldText").objectReferenceValue = armorText;
            serializedHud.FindProperty("chargesText").objectReferenceValue = chargesText;
            serializedHud.FindProperty("roundResultText").objectReferenceValue = roundResult;
            serializedHud.FindProperty("debugHint").objectReferenceValue = debugHintText.gameObject;
            serializedHud.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
        }

        private static void CreateShopButton(Transform parent, ShopController shop, ShopItemData item, int index, float y, Font font)
        {
            GameObject buttonObject = new GameObject($"ShopItem_{index}_{item.name}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(940f, 52f);
            buttonObject.GetComponent<Image>().color = index % 2 == 0
                ? new Color(0.1f, 0.15f, 0.22f, 1f)
                : new Color(0.08f, 0.12f, 0.18f, 1f);

            Text nameText = CreateText(buttonObject.transform, "ItemName", item.DisplayName, font, 22, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.5f), new Vector2(260f, 0f), new Vector2(500f, 46f));
            Text statusText = CreateText(buttonObject.transform, "ItemStatus", item.Price == 0 ? "FREE" : $"{item.Price} CR", font, 22,
                TextAnchor.MiddleRight, new Vector2(1f, 0.5f), new Vector2(-145f, 0f), new Vector2(270f, 46f));
            statusText.color = new Color(0.25f, 0.85f, 1f);

            ShopItemButton shopButton = buttonObject.AddComponent<ShopItemButton>();
            SerializedObject serializedButton = new SerializedObject(shopButton);
            serializedButton.FindProperty("shop").objectReferenceValue = shop;
            serializedButton.FindProperty("item").objectReferenceValue = item;
            serializedButton.FindProperty("nameText").objectReferenceValue = nameText;
            serializedButton.FindProperty("statusText").objectReferenceValue = statusText;
            serializedButton.ApplyModifiedPropertiesWithoutUndo();
        }

        private static WeaponData CreateOrUpdateWeaponData(string assetName, string displayName, float damage, float rpm,
            float range, int magazine, int reserve, float reload, int pellets, float spread, float recoil)
        {
            string path = $"{WeaponsDataFolder}/{assetName}.asset";
            WeaponData data = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<WeaponData>();
                data.name = assetName;
                AssetDatabase.CreateAsset(data, path);
            }

            SerializedObject serialized = new SerializedObject(data);
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("baseDamage").floatValue = damage;
            serialized.FindProperty("roundsPerMinute").floatValue = rpm;
            serialized.FindProperty("range").floatValue = range;
            serialized.FindProperty("magazineSize").intValue = magazine;
            serialized.FindProperty("startingReserveAmmo").intValue = reserve;
            serialized.FindProperty("reloadDuration").floatValue = reload;
            serialized.FindProperty("pelletsPerShot").intValue = pellets;
            serialized.FindProperty("spreadAngle").floatValue = spread;
            serialized.FindProperty("recoilKick").floatValue = recoil;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static ShopItemData CreateOrUpdateShopItem(string assetName, string displayName, ShopCategory category,
            ShopPurchaseType purchaseType, int price, WeaponData weaponData, WeaponSlot slot, float shieldAmount, int charges)
        {
            string path = $"{ShopDataFolder}/{assetName}.asset";
            ShopItemData item = AssetDatabase.LoadAssetAtPath<ShopItemData>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ShopItemData>();
                item.name = assetName;
                AssetDatabase.CreateAsset(item, path);
            }

            SerializedObject serialized = new SerializedObject(item);
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("category").enumValueIndex = (int)category;
            serialized.FindProperty("purchaseType").enumValueIndex = (int)purchaseType;
            serialized.FindProperty("price").intValue = price;
            serialized.FindProperty("available").boolValue = true;
            serialized.FindProperty("weaponData").objectReferenceValue = weaponData;
            serialized.FindProperty("weaponSlot").enumValueIndex = (int)slot;
            serialized.FindProperty("shieldAmount").floatValue = shieldAmount;
            serialized.FindProperty("abilityCharges").intValue = charges;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            return item;
        }

        private static void CreateEventSystemIfMissing(Scene scene, Transform parent)
        {
            if (FindComponentInScene<EventSystem>(scene) != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            eventSystemObject.transform.SetParent(parent);
            InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }

        private static void SetObjectArray<T>(SerializedProperty property, T[] values) where T : Object
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        private static Text CreateText(Transform parent, string name, string value, Font font, int fontSize,
            TextAnchor alignment, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateVisualCube(string name, Vector3 localPosition, Vector3 localScale, Material material, Transform parent)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            return cube;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static T[] FindComponentsInScene<T>(Scene scene) where T : Component
        {
            List<T> components = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                components.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return components.ToArray();
        }

        private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
            return cube;
        }

        private static Material CreateOrLoadMaterial(string name, Color color)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader)
            {
                name = name,
                color = color
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void AddSceneToBuildSettings()
        {
            EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene existingScene in existingScenes)
            {
                if (existingScene.path == ScenePath)
                {
                    return;
                }
            }

            EditorBuildSettingsScene[] updatedScenes = new EditorBuildSettingsScene[existingScenes.Length + 1];
            existingScenes.CopyTo(updatedScenes, 0);
            updatedScenes[^1] = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = updatedScenes;
        }
    }
}
