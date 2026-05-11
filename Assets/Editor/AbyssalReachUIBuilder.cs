#if UNITY_EDITOR
// ════════════════════════════════════════════════════════════════════════════
//  AbyssalReachUIBuilder.cs
//  Generador automático del Menú Principal y la UI de Pausa para AbyssalReach.
//
//  COLOCACIÓN: este archivo DEBE estar dentro de cualquier carpeta llamada
//  "Editor" en tu proyecto, por ejemplo:  Assets/Editor/AbyssalReachUIBuilder.cs
//
//  USO: tras compilar, aparece el menú "AbyssalReach > Build UI" en la barra
//  superior de Unity. Ejecuta los items en orden 1 → 2 → 3.
//
//  Requisitos:
//    - Unity 6 (6000.x) o 2022.3+
//    - TextMeshPro importado
//    - Los scripts (PauseMenuController, MainMenuController, SaveSlotUI, etc.)
//      ya presentes y compilados en el proyecto.
// ════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace AbyssalReach.EditorTools
{
    public static class AbyssalReachUIBuilder
    {
        // ════════════════════════════════════════════════════════════════════
        //  CONSTANTES Y PALETA DE COLORES
        // ════════════════════════════════════════════════════════════════════
        private static readonly Color C_BG_DARK = Hex("#0A1929");
        private static readonly Color C_TEAL_BRIGHT = Hex("#5DCAA5");
        private static readonly Color C_TEAL_LIGHT = Hex("#9FE1CB");
        private static readonly Color C_TEAL_BORDER = Hex("#1D9E75");
        private static readonly Color C_TEAL_FAINT = new Color(0.365f, 0.792f, 0.647f, 0.10f);
        private static readonly Color C_DANGER = Hex("#F09595");
        private static readonly Color C_PANEL_BG = new Color(0.039f, 0.098f, 0.161f, 0.95f);
        private static readonly Color C_OVERLAY_BG = new Color(0.016f, 0.110f, 0.188f, 0.75f);
        private static readonly Color C_BTN_PRIMARY = new Color(0.365f, 0.792f, 0.647f, 0.18f);
        private static readonly Color C_BTN_GHOST = new Color(0, 0, 0, 0);
        private static readonly Color C_TEXT_DIM = new Color(0.624f, 0.882f, 0.796f, 0.6f);

        // Rutas
        private const string SCENES_PATH = "Assets/Scenes";
        private const string PREFABS_PATH = "Assets/Prefabs/UI";
        private const string MAIN_SCENE_NAME = "MainMenu";
        private const string PREFAB_SLOT_NAME = "SaveSlotUI";

        // Cache de tipos
        private static Type T_SaveManager, T_SceneLoader, T_AudioManager;
        private static Type T_MainMenuController, T_PauseMenuController;
        private static Type T_SettingsMenuController, T_SaveSlotUI, T_GameInitializer;

        // ════════════════════════════════════════════════════════════════════
        //  MENU ITEM 1: PREFAB SaveSlotUI
        // ════════════════════════════════════════════════════════════════════
        [MenuItem("AbyssalReach/Build UI/1. Crear Prefab SaveSlotUI", priority = 1)]
        public static void CreateSaveSlotPrefab()
        {
            if (!ResolveTypes()) return;
            EnsureFolder(PREFABS_PATH);

            GameObject root = BuildSaveSlotGO();
            string path = $"{PREFABS_PATH}/{PREFAB_SLOT_NAME}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("✓ Prefab creado",
                $"Guardado en:\n{path}\n\nPaso siguiente: item 2 del menú.", "OK");
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        // ════════════════════════════════════════════════════════════════════
        //  MENU ITEM 2: ESCENA MainMenu
        // ════════════════════════════════════════════════════════════════════
        [MenuItem("AbyssalReach/Build UI/2. Crear Escena MainMenu", priority = 2)]
        public static void CreateMainMenuScene()
        {
            if (!ResolveTypes()) return;

            // Verificar/crear prefab
            string prefabPath = $"{PREFABS_PATH}/{PREFAB_SLOT_NAME}.prefab";
            GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (slotPrefab == null)
            {
                if (!EditorUtility.DisplayDialog("Prefab no encontrado",
                    "No existe el prefab SaveSlotUI. ¿Crearlo ahora?",
                    "Sí, crearlo", "Cancelar")) return;
                CreateSaveSlotPrefab();
                slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (slotPrefab == null) return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // ─── Crear escena ────────────────────────────────────────────────
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ─── Managers ────────────────────────────────────────────────────
            GameObject managers = new GameObject("_Managers");
            CreateManager(managers.transform, "SaveManager", T_SaveManager);
            CreateManager(managers.transform, "SceneLoader", T_SceneLoader);
            CreateManager(managers.transform, "AudioManager", T_AudioManager);

            // ─── Cámara ──────────────────────────────────────────────────────
            GameObject camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            var cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = C_BG_DARK;
            cam.tag = "MainCamera";

            // ─── EventSystem ─────────────────────────────────────────────────
            CreateEventSystem();

            // ─── Canvas ──────────────────────────────────────────────────────
            GameObject canvasGO = CreateCanvas("Canvas_MainMenu");

            // ─── Fondo de toda la pantalla ───────────────────────────────────
            CreateFullScreenPanel("Background", canvasGO.transform, C_BG_DARK);

            // ─── Controller GameObject ───────────────────────────────────────
            GameObject controller = new GameObject("MainMenuController");
            controller.transform.SetParent(canvasGO.transform, false);
            var mainCtrl = controller.AddComponent(T_MainMenuController);

            // ─── PanelMain ───────────────────────────────────────────────────
            GameObject panelMain = CreateFullScreenPanel("PanelMain", canvasGO.transform, C_BG_DARK);
            var mainVlg = panelMain.AddComponent<VerticalLayoutGroup>();
            mainVlg.padding = new RectOffset(0, 0, 200, 200);
            mainVlg.spacing = 10;
            mainVlg.childAlignment = TextAnchor.MiddleCenter;
            mainVlg.childControlWidth = false;
            mainVlg.childControlHeight = false;
            mainVlg.childForceExpandWidth = false;
            mainVlg.childForceExpandHeight = false;

            CreateTitle("TitleText", panelMain.transform, "ABYSSAL REACH", 56, C_TEAL_BRIGHT, true);
            CreateTitle("Subtitle", panelMain.transform, "— ESTUDIO INDIE —", 16, C_TEAL_LIGHT);
            AddSpacer(panelMain.transform, 30);

            GameObject btnPlay = CreateMenuButton("BtnPlay", panelMain.transform, "▶  Jugar", true);
            GameObject btnSettings = CreateMenuButton("BtnSettings", panelMain.transform, "⚙  Ajustes", false);
            GameObject btnQuit = CreateMenuButton("BtnQuit", panelMain.transform, "✕  Salir", false);

            // ─── PanelSlots ──────────────────────────────────────────────────
            GameObject panelSlots = CreateFullScreenPanel("PanelSlots", canvasGO.transform, C_BG_DARK);
            var slotsVlg = panelSlots.AddComponent<VerticalLayoutGroup>();
            slotsVlg.padding = new RectOffset(400, 400, 120, 120);
            slotsVlg.spacing = 14;
            slotsVlg.childAlignment = TextAnchor.UpperCenter;
            slotsVlg.childControlWidth = true;
            slotsVlg.childControlHeight = false;
            slotsVlg.childForceExpandWidth = true;
            slotsVlg.childForceExpandHeight = false;

            CreateTitle("Title", panelSlots.transform, "Selecciona partida", 32, C_TEAL_LIGHT, true);
            AddSpacer(panelSlots.transform, 20);

            GameObject[] slotInstances = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                slotInstances[i] = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, panelSlots.transform);
                slotInstances[i].name = $"SaveSlot_{i}";
            }
            AddSpacer(panelSlots.transform, 10);
            GameObject btnBackFromSlots = CreateMenuButton("BtnBackFromSlots", panelSlots.transform, "← Volver", false);

            // ─── PanelSettings ───────────────────────────────────────────────
            GameObject panelSettings = BuildSettingsPanel(canvasGO.transform);
            var settingsCtrl = panelSettings.GetComponent(T_SettingsMenuController);
            var btnBackFromSettings = panelSettings.transform.Find("Dialog/BtnBackFromSettings").GetComponent<Button>();

            // ─── PanelConfirmDelete ──────────────────────────────────────────
            GameObject panelConfirmDelete = BuildConfirmDeletePanel(canvasGO.transform);
            var btnConfirmDelete = panelConfirmDelete.transform.Find("Dialog/Buttons/BtnConfirmDelete").GetComponent<Button>();
            var btnCancelDelete = panelConfirmDelete.transform.Find("Dialog/Buttons/BtnCancelDelete").GetComponent<Button>();

            // ─── Conectar referencias del MainMenuController ─────────────────
            var so = new SerializedObject(mainCtrl);
            SetObjRef(so, "panelMain", panelMain);
            SetObjRef(so, "panelSlots", panelSlots);
            SetObjRef(so, "panelSettings", panelSettings);
            SetObjRef(so, "panelConfirmDelete", panelConfirmDelete);
            SetObjRef(so, "btnPlay", btnPlay.GetComponent<Button>());
            SetObjRef(so, "btnSettings", btnSettings.GetComponent<Button>());
            SetObjRef(so, "btnQuit", btnQuit.GetComponent<Button>());
            SetObjRef(so, "btnBackFromSlots", btnBackFromSlots.GetComponent<Button>());
            SetObjRef(so, "settingsController", settingsCtrl);
            SetObjRef(so, "btnBackFromSettings", btnBackFromSettings);
            SetObjRef(so, "btnConfirmDelete", btnConfirmDelete);
            SetObjRef(so, "btnCancelDelete", btnCancelDelete);

            var saveSlotComponents = slotInstances
                .Select(g => g.GetComponent(T_SaveSlotUI))
                .Cast<UnityEngine.Object>().ToArray();
            SetObjArray(so, "saveSlots", saveSlotComponents);
            so.ApplyModifiedProperties();

            // Estado inicial: solo PanelMain activo
            panelSlots.SetActive(false);
            panelSettings.SetActive(false);
            panelConfirmDelete.SetActive(false);

            // ─── Guardar escena ──────────────────────────────────────────────
            EnsureFolder(SCENES_PATH);
            string scenePath = $"{SCENES_PATH}/{MAIN_SCENE_NAME}.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            EditorUtility.DisplayDialog("✓ Escena MainMenu creada",
                $"Guardada en:\n{scenePath}\n\n" +
                "Pasos siguientes:\n" +
                "1. File > Build Settings... y añade esta escena.\n" +
                "2. Abre tu escena de gameplay y ejecuta el item 3 del menú.",
                "OK");
        }

        // ════════════════════════════════════════════════════════════════════
        //  MENU ITEM 3: AÑADIR PAUSE UI A LA ESCENA ACTUAL
        // ════════════════════════════════════════════════════════════════════
        [MenuItem("AbyssalReach/Build UI/3. Añadir Pause UI a Escena Actual", priority = 3)]
        public static void AddPauseUIToCurrentScene()
        {
            if (!ResolveTypes()) return;

            string prefabPath = $"{PREFABS_PATH}/{PREFAB_SLOT_NAME}.prefab";
            GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (slotPrefab == null)
            {
                EditorUtility.DisplayDialog("Falta el prefab",
                    "Ejecuta primero el paso 1 (Crear Prefab SaveSlotUI).", "OK");
                return;
            }

            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
                CreateEventSystem();

            // ─── Canvas de pausa ─────────────────────────────────────────────
            GameObject canvasGO = CreateCanvas("Canvas_PauseUI");
            canvasGO.GetComponent<Canvas>().sortingOrder = 100;

            GameObject controllerGO = new GameObject("PauseMenuController");
            controllerGO.transform.SetParent(canvasGO.transform, false);
            var pauseCtrl = controllerGO.AddComponent(T_PauseMenuController);

            // ─── PanelPause ──────────────────────────────────────────────────
            GameObject panelPause = CreateOverlayPanel("PanelPause", canvasGO.transform);
            GameObject pauseDialog = BuildDialogBox(panelPause.transform, "— PAUSA —", 380, 380);
            GameObject btnResume = CreateMenuButton("BtnResume", pauseDialog.transform, "▶  Continuar", true);
            GameObject btnSave = CreateMenuButton("BtnSave", pauseDialog.transform, "💾  Guardar partida", false);
            GameObject btnSettingsP = CreateMenuButton("BtnSettings", pauseDialog.transform, "⚙  Ajustes", false);
            GameObject btnMainMenu = CreateMenuButton("BtnMainMenu", pauseDialog.transform, "⌂  Menú principal", false, true);

            // ─── PanelSettings ───────────────────────────────────────────────
            GameObject panelSettings = BuildSettingsPanel(canvasGO.transform);
            var settingsCtrl = panelSettings.GetComponent(T_SettingsMenuController);
            var btnBackFromSettings = panelSettings.transform.Find("Dialog/BtnBackFromSettings").GetComponent<Button>();

            // ─── PanelSave ───────────────────────────────────────────────────
            GameObject panelSave = CreateOverlayPanel("PanelSave", canvasGO.transform);
            GameObject saveDialog = BuildDialogBox(panelSave.transform, "Guardar partida", 900, 540);
            var saveVlg = saveDialog.GetComponent<VerticalLayoutGroup>();
            saveVlg.spacing = 12;

            GameObject[] slotInstances = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                slotInstances[i] = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, saveDialog.transform);
                slotInstances[i].name = $"SaveSlot_{i}";

                // En la pausa NO hay borrado: quitamos la referencia y ocultamos el botón
                var slotUI = slotInstances[i].GetComponent(T_SaveSlotUI);
                var slotSO = new SerializedObject(slotUI);
                SetObjRef(slotSO, "btnDelete", null);
                slotSO.ApplyModifiedProperties();
                var btnDeleteTr = slotInstances[i].transform.Find("BtnDelete");
                if (btnDeleteTr) btnDeleteTr.gameObject.SetActive(false);
            }
            AddSpacer(saveDialog.transform, 10);
            GameObject btnBackFromSave = CreateMenuButton("BtnBackFromSave", saveDialog.transform, "← Volver", false);

            // ─── Conectar referencias del PauseMenuController ────────────────
            var so = new SerializedObject(pauseCtrl);
            SetObjRef(so, "panelPause", panelPause);
            SetObjRef(so, "panelSettings", panelSettings);
            SetObjRef(so, "panelSave", panelSave);
            SetObjRef(so, "btnResume", btnResume.GetComponent<Button>());
            SetObjRef(so, "btnSave", btnSave.GetComponent<Button>());
            SetObjRef(so, "btnSettings", btnSettingsP.GetComponent<Button>());
            SetObjRef(so, "btnMainMenu", btnMainMenu.GetComponent<Button>());
            SetObjRef(so, "settingsController", settingsCtrl);
            SetObjRef(so, "btnBackFromSettings", btnBackFromSettings);
            SetObjRef(so, "btnBackFromSave", btnBackFromSave.GetComponent<Button>());
            var saveSlotComponents = slotInstances
                .Select(g => g.GetComponent(T_SaveSlotUI))
                .Cast<UnityEngine.Object>().ToArray();
            SetObjArray(so, "saveSlots", saveSlotComponents);
            so.ApplyModifiedProperties();

            // GameInitializer si no existe
            if (T_GameInitializer != null && UnityEngine.Object.FindFirstObjectByType(T_GameInitializer) == null)
            {
                GameObject gi = new GameObject("GameInitializer");
                gi.AddComponent(T_GameInitializer);
            }

            // Estado inicial: todos los paneles ocultos
            panelPause.SetActive(false);
            panelSettings.SetActive(false);
            panelSave.SetActive(false);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("✓ Pause UI añadida",
                "Se ha añadido Canvas_PauseUI a la escena.\n\n" +
                "FALTA POR HACER MANUALMENTE:\n\n" +
                "1. Guardar la escena (Ctrl+S).\n" +
                "2. En tu AbyssalReachControls.inputactions, añade al Action Map\n" +
                "   'Global' una acción llamada 'Pause' con binding <Keyboard>/escape.\n" +
                "3. En PauseMenuController.cs cambia las dos líneas:\n" +
                "     controls.UI.Cancel.performed  →  controls.Global.Pause.performed", "OK");
        }

        // ════════════════════════════════════════════════════════════════════
        //  BUILDERS PRINCIPALES
        // ════════════════════════════════════════════════════════════════════
        private static GameObject BuildSaveSlotGO()
        {
            // Root con HorizontalLayoutGroup
            GameObject root = new GameObject("SaveSlotUI",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(HorizontalLayoutGroup), typeof(LayoutElement));

            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(800, 80);

            var img = root.GetComponent<Image>();
            img.color = C_TEAL_FAINT;
            img.sprite = GetSprite("UI/Skin/Background.psd");
            img.type = Image.Type.Sliced;

            var hlg = root.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(16, 16, 12, 12);
            hlg.spacing = 14;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false; hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

            root.GetComponent<LayoutElement>().preferredHeight = 80;

            // InfoArea (parte flexible)
            GameObject infoArea = new GameObject("InfoArea",
                typeof(RectTransform), typeof(LayoutElement));
            infoArea.transform.SetParent(root.transform, false);
            var infoLE = infoArea.GetComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            infoLE.preferredHeight = 56;
            infoArea.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 56);

            // GroupEmpty
            GameObject groupEmpty = new GameObject("GroupEmpty", typeof(RectTransform));
            groupEmpty.transform.SetParent(infoArea.transform, false);
            FillParent(groupEmpty);
            GameObject emptyLabel = CreateText("EmptyLabel", groupEmpty.transform,
                "Slot vacío", 18, C_TEXT_DIM, TextAlignmentOptions.MidlineLeft);
            FillParent(emptyLabel);
            emptyLabel.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Italic;

            // GroupFilled
            GameObject groupFilled = new GameObject("GroupFilled",
                typeof(RectTransform), typeof(VerticalLayoutGroup));
            groupFilled.transform.SetParent(infoArea.transform, false);
            FillParent(groupFilled);
            var gfVlg = groupFilled.GetComponent<VerticalLayoutGroup>();
            gfVlg.spacing = 2;
            gfVlg.childAlignment = TextAnchor.MiddleLeft;
            gfVlg.childControlWidth = true; gfVlg.childControlHeight = false;
            gfVlg.childForceExpandWidth = true; gfVlg.childForceExpandHeight = false;

            GameObject txtSlotName = CreateText("TxtSlotName", groupFilled.transform,
                "Partida 1", 18, C_TEAL_LIGHT, TextAlignmentOptions.MidlineLeft);
            txtSlotName.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            GameObject infoRow = new GameObject("InfoRow",
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            infoRow.transform.SetParent(groupFilled.transform, false);
            var rowHlg = infoRow.GetComponent<HorizontalLayoutGroup>();
            rowHlg.spacing = 14;
            rowHlg.childAlignment = TextAnchor.MiddleLeft;
            rowHlg.childControlWidth = false; rowHlg.childControlHeight = false;
            rowHlg.childForceExpandWidth = false; rowHlg.childForceExpandHeight = false;
            infoRow.GetComponent<LayoutElement>().preferredHeight = 18;

            GameObject txtDate = CreateText("TxtDate", infoRow.transform, "14/05/2026 19:42", 13, C_TEXT_DIM, TextAlignmentOptions.MidlineLeft);
            GameObject txtPlayTime = CreateText("TxtPlayTime", infoRow.transform, "2h 35m", 13, C_TEXT_DIM, TextAlignmentOptions.MidlineLeft);
            GameObject txtGold = CreateText("TxtGold", infoRow.transform, "1240 ☽", 13, C_TEXT_DIM, TextAlignmentOptions.MidlineLeft);
            txtDate.GetComponent<RectTransform>().sizeDelta = new Vector2(140, 18);
            txtPlayTime.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 18);
            txtGold.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 18);

            // BtnAction y BtnDelete (siempre presentes en el prefab; el script de
            // SaveSlotUI desactiva BtnDelete según el contexto)
            GameObject btnAction = CreatePillButton("BtnAction", root.transform, "Jugar", 130, 40, true, false);
            GameObject btnDelete = CreatePillButton("BtnDelete", root.transform, "🗑", 44, 40, false, true);

            // Script y referencias
            var slotComp = root.AddComponent(T_SaveSlotUI);
            var so = new SerializedObject(slotComp);
            SetObjRef(so, "groupEmpty", groupEmpty);
            SetObjRef(so, "groupFilled", groupFilled);
            SetObjRef(so, "txtSlotName", txtSlotName.GetComponent<TextMeshProUGUI>());
            SetObjRef(so, "txtDate", txtDate.GetComponent<TextMeshProUGUI>());
            SetObjRef(so, "txtPlayTime", txtPlayTime.GetComponent<TextMeshProUGUI>());
            SetObjRef(so, "txtGold", txtGold.GetComponent<TextMeshProUGUI>());
            SetObjRef(so, "btnAction", btnAction.GetComponent<Button>());
            SetObjRef(so, "btnDelete", btnDelete.GetComponent<Button>());
            so.ApplyModifiedProperties();

            return root;
        }

        private static GameObject BuildSettingsPanel(Transform parent)
        {
            GameObject panel = CreateOverlayPanel("PanelSettings", parent);
            GameObject dialog = BuildDialogBox(panel.transform, "Ajustes", 640, 480);

            // Sliders
            var sMaster = CreateSliderRow(dialog.transform, "Master", "General", out var lMaster);
            var sMusic = CreateSliderRow(dialog.transform, "Music", "Música", out var lMusic);
            var sSFX = CreateSliderRow(dialog.transform, "SFX", "Efectos", out var lSFX);

            // Dropdown calidad
            GameObject dropRow = new GameObject("QualityRow",
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            dropRow.transform.SetParent(dialog.transform, false);
            var qHlg = dropRow.GetComponent<HorizontalLayoutGroup>();
            qHlg.spacing = 14;
            qHlg.childAlignment = TextAnchor.MiddleLeft;
            qHlg.childControlWidth = false; qHlg.childControlHeight = false;
            qHlg.childForceExpandWidth = false;
            dropRow.GetComponent<LayoutElement>().preferredHeight = 40;

            GameObject lbl = CreateText("Label", dropRow.transform, "Calidad", 16, C_TEAL_LIGHT, TextAlignmentOptions.MidlineLeft);
            lbl.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 30);

            TMP_Dropdown dropdown = CreateTMPDropdown("DropdownQuality", dropRow.transform);

            AddSpacer(dialog.transform, 20);
            CreateMenuButton("BtnBackFromSettings", dialog.transform, "← Volver", false);

            // Script
            var settingsComp = panel.AddComponent(T_SettingsMenuController);
            var so = new SerializedObject(settingsComp);
            SetObjRef(so, "sliderMaster", sMaster);
            SetObjRef(so, "sliderMusic", sMusic);
            SetObjRef(so, "sliderSFX", sSFX);
            SetObjRef(so, "lblMaster", lMaster);
            SetObjRef(so, "lblMusic", lMusic);
            SetObjRef(so, "lblSFX", lSFX);
            SetObjRef(so, "dropQuality", dropdown);
            so.ApplyModifiedProperties();

            return panel;
        }

        private static GameObject BuildConfirmDeletePanel(Transform parent)
        {
            GameObject panel = CreateOverlayPanel("PanelConfirmDelete", parent);
            GameObject dialog = BuildDialogBox(panel.transform, "¿Eliminar esta partida?", 460, 220);
            dialog.transform.Find("Title").GetComponent<TextMeshProUGUI>().color = C_DANGER;

            GameObject btnRow = new GameObject("Buttons",
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            btnRow.transform.SetParent(dialog.transform, false);
            var hlg = btnRow.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false; hlg.childControlHeight = false;
            btnRow.GetComponent<LayoutElement>().preferredHeight = 50;

            CreatePillButton("BtnConfirmDelete", btnRow.transform, "Eliminar", 150, 44, false, true);
            CreatePillButton("BtnCancelDelete", btnRow.transform, "Cancelar", 150, 44, false, false);

            return panel;
        }

        // ════════════════════════════════════════════════════════════════════
        //  HELPERS DE UI
        // ════════════════════════════════════════════════════════════════════
        private static GameObject CreateCanvas(string name)
        {
            GameObject go = new GameObject(name,
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var cs = go.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            cs.matchWidthOrHeight = 0.5f;
            return go;
        }

        private static void CreateEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
            GameObject es = new GameObject("EventSystem", typeof(EventSystem));
            // Unity 6 + new Input System
            var inputModule = FindType("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
            if (inputModule != null) es.AddComponent(inputModule);
            else es.AddComponent<StandaloneInputModule>();
        }

        private static GameObject CreateManager(Transform parent, string name, Type t)
        {
            if (t == null) return null;
            var existing = UnityEngine.Object.FindFirstObjectByType(t);
            if (existing != null) return ((Component)existing).gameObject;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent(t);
            return go;
        }

        private static GameObject CreateFullScreenPanel(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            FillParent(go);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static GameObject CreateOverlayPanel(string name, Transform parent)
        {
            // Panel completo con un fondo translúcido oscuro (para usar como wrapper
            // de un Dialog centrado). Es lo que el script controla con SetActive.
            GameObject go = CreateFullScreenPanel(name, parent, C_OVERLAY_BG);
            return go;
        }

        private static GameObject BuildDialogBox(Transform parent, string title, float width, float height)
        {
            GameObject dialog = new GameObject("Dialog",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(VerticalLayoutGroup));
            dialog.transform.SetParent(parent, false);
            var rt = dialog.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = Vector2.zero;

            var img = dialog.GetComponent<Image>();
            img.color = C_PANEL_BG;
            img.sprite = GetSprite("UI/Skin/Background.psd");
            img.type = Image.Type.Sliced;

            // Borde con Outline
            var outline = dialog.AddComponent<Outline>();
            outline.effectColor = new Color(0.365f, 0.792f, 0.647f, 0.3f);
            outline.effectDistance = new Vector2(2, -2);

            var vlg = dialog.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(30, 30, 25, 25);
            vlg.spacing = 8;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = false; vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;

            if (!string.IsNullOrEmpty(title))
            {
                CreateTitle("Title", dialog.transform, title, 22, C_TEAL_LIGHT, true);
                AddSpacer(dialog.transform, 10);
            }
            return dialog;
        }

        private static GameObject CreateMenuButton(string name, Transform parent, string label, bool primary, bool danger = false)
        {
            return CreatePillButton(name, parent, label, 300, 50, primary, danger);
        }

        private static GameObject CreatePillButton(string name, Transform parent, string label,
            float width, float height, bool primary, bool danger)
        {
            GameObject go = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);

            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;

            var img = go.GetComponent<Image>();
            img.color = primary ? C_BTN_PRIMARY : C_BTN_GHOST;
            img.sprite = GetSprite("UI/Skin/Background.psd");
            img.type = Image.Type.Sliced;

            // Borde
            var outline = go.AddComponent<Outline>();
            if (danger)
                outline.effectColor = new Color(0.94f, 0.58f, 0.58f, 0.45f);
            else if (primary)
                outline.effectColor = C_TEAL_BORDER;
            else
                outline.effectColor = new Color(0.624f, 0.882f, 0.796f, 0.3f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Colores hover
            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1, 1, 1, 0.88f);
            colors.pressedColor = new Color(0.8f, 0.95f, 0.88f, 1f);
            btn.colors = colors;

            // Label
            GameObject lblGo = new GameObject("Label",
                typeof(RectTransform), typeof(CanvasRenderer));
            lblGo.transform.SetParent(go.transform, false);
            FillParent(lblGo);
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = danger ? C_DANGER : C_TEAL_LIGHT;
            tmp.raycastTarget = false;

            return go;
        }

        private static Slider CreateSliderRow(Transform parent, string id, string labelText, out TextMeshProUGUI percent)
        {
            GameObject row = new GameObject($"{id}Row",
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 14;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false; hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            row.GetComponent<LayoutElement>().preferredHeight = 36;

            // Label izquierda
            GameObject lbl = CreateText("Label", row.transform, labelText, 16, C_TEAL_LIGHT, TextAlignmentOptions.MidlineLeft);
            lbl.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 28);

            // Slider
            GameObject sliderGO = BuildSlider($"Slider{id}", row.transform, 380);

            // Label %
            GameObject pct = CreateText($"Lbl{id}", row.transform, "100%", 14, C_TEAL_LIGHT, TextAlignmentOptions.MidlineRight);
            pct.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 28);
            percent = pct.GetComponent<TextMeshProUGUI>();

            return sliderGO.GetComponent<Slider>();
        }

        private static GameObject BuildSlider(string name, Transform parent, float width)
        {
            GameObject root = new GameObject(name,
                typeof(RectTransform), typeof(LayoutElement));
            root.transform.SetParent(parent, false);
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 20);
            var le = root.GetComponent<LayoutElement>();
            le.preferredWidth = width; le.preferredHeight = 20;

            // Background
            GameObject bg = new GameObject("Background",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bg.transform.SetParent(root.transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.4f);
            bgRt.anchorMax = new Vector2(1, 0.6f);
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            var bgImg = bg.GetComponent<Image>();
            bgImg.color = new Color(0.624f, 0.882f, 0.796f, 0.15f);
            bgImg.sprite = GetSprite("UI/Skin/Background.psd");
            bgImg.type = Image.Type.Sliced;

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            var faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0, 0.4f); faRt.anchorMax = new Vector2(1, 0.6f);
            faRt.offsetMin = new Vector2(5, 0); faRt.offsetMax = new Vector2(-15, 0);

            GameObject fill = new GameObject("Fill",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fRt = fill.GetComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero; fRt.anchorMax = Vector2.one;
            fRt.offsetMin = Vector2.zero; fRt.offsetMax = Vector2.zero;
            fRt.sizeDelta = new Vector2(10, 0);
            var fImg = fill.GetComponent<Image>();
            fImg.color = C_TEAL_BORDER;
            fImg.sprite = GetSprite("UI/Skin/Background.psd");
            fImg.type = Image.Type.Sliced;

            // Handle Slide Area
            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(root.transform, false);
            var haRt = handleArea.GetComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one;
            haRt.offsetMin = new Vector2(10, 0); haRt.offsetMax = new Vector2(-10, 0);

            GameObject handle = new GameObject("Handle",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var hRt = handle.GetComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(20, 20);
            var hImg = handle.GetComponent<Image>();
            hImg.color = C_TEAL_BRIGHT;
            hImg.sprite = GetSprite("UI/Skin/Knob.psd");

            var slider = root.AddComponent<Slider>();
            slider.targetGraphic = hImg;
            slider.fillRect = fRt;
            slider.handleRect = hRt;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;

            return root;
        }

        private static TMP_Dropdown CreateTMPDropdown(string name, Transform parent)
        {
            GameObject go = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(TMP_Dropdown), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(380, 36);
            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = 380; le.preferredHeight = 36;

            var img = go.GetComponent<Image>();
            img.color = C_BTN_PRIMARY;
            img.sprite = GetSprite("UI/Skin/Background.psd");
            img.type = Image.Type.Sliced;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.624f, 0.882f, 0.796f, 0.3f);
            outline.effectDistance = new Vector2(1, -1);

            // Caption
            GameObject caption = CreateText("Label", go.transform, "Alta", 14, C_TEAL_LIGHT, TextAlignmentOptions.MidlineLeft);
            var crt = caption.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = new Vector2(12, 0); crt.offsetMax = new Vector2(-28, 0);

            // Arrow
            GameObject arrow = CreateText("Arrow", go.transform, "▾", 16, C_TEAL_LIGHT, TextAlignmentOptions.Center);
            var art = arrow.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(1, 0.5f); art.anchorMax = new Vector2(1, 0.5f);
            art.anchoredPosition = new Vector2(-15, 0);
            art.sizeDelta = new Vector2(20, 20);

            // Template
            GameObject template = new GameObject("Template",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            template.transform.SetParent(go.transform, false);
            var trt = template.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0); trt.anchorMax = new Vector2(1, 0);
            trt.pivot = new Vector2(0.5f, 1);
            trt.sizeDelta = new Vector2(0, 150);
            var tImg = template.GetComponent<Image>();
            tImg.color = C_PANEL_BG;
            tImg.sprite = GetSprite("UI/Skin/Background.psd");
            tImg.type = Image.Type.Sliced;
            template.SetActive(false);

            // Viewport
            GameObject viewport = new GameObject("Viewport",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(template.transform, false);
            FillParent(viewport);
            viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            // Content
            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var conRt = content.GetComponent<RectTransform>();
            conRt.anchorMin = new Vector2(0, 1); conRt.anchorMax = new Vector2(1, 1);
            conRt.pivot = new Vector2(0.5f, 1);
            conRt.sizeDelta = new Vector2(0, 28);

            // Item
            GameObject item = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            item.transform.SetParent(content.transform, false);
            var itemRt = item.GetComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0, 0.5f); itemRt.anchorMax = new Vector2(1, 0.5f);
            itemRt.sizeDelta = new Vector2(0, 22);

            GameObject itemBg = new GameObject("Item Background",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            itemBg.transform.SetParent(item.transform, false);
            FillParent(itemBg);
            itemBg.GetComponent<Image>().color = new Color(0.365f, 0.792f, 0.647f, 0.2f);

            GameObject itemLbl = CreateText("Item Label", item.transform, "Option", 14, C_TEAL_LIGHT, TextAlignmentOptions.MidlineLeft);
            FillParent(itemLbl);
            var ilRt = itemLbl.GetComponent<RectTransform>();
            ilRt.offsetMin = new Vector2(10, 0);

            item.GetComponent<Toggle>().targetGraphic = itemBg.GetComponent<Image>();

            var scroll = template.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = content.GetComponent<RectTransform>();
            scroll.horizontal = false; scroll.vertical = true;

            var dd = go.GetComponent<TMP_Dropdown>();
            dd.template = trt;
            dd.captionText = caption.GetComponent<TextMeshProUGUI>();
            dd.itemText = itemLbl.GetComponent<TextMeshProUGUI>();
            return dd;
        }

        private static GameObject CreateText(string name, Transform parent, string text,
            int size, Color color, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, size + 8);
            return go;
        }

        private static GameObject CreateTitle(string name, Transform parent, string text,
            int size, Color color, bool bold = false)
        {
            GameObject go = CreateText(name, parent, text, size, color, TextAlignmentOptions.Center);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (bold) tmp.fontStyle = FontStyles.Bold;
            tmp.characterSpacing = 4;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(800, size + 20);
            go.AddComponent<LayoutElement>().preferredHeight = size + 20;
            return go;
        }

        private static void AddSpacer(Transform parent, float height)
        {
            GameObject spacer = new GameObject("Spacer",
                typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(parent, false);
            spacer.GetComponent<LayoutElement>().preferredHeight = height;
            spacer.GetComponent<RectTransform>().sizeDelta = new Vector2(10, height);
        }

        private static void FillParent(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ════════════════════════════════════════════════════════════════════
        //  UTILIDADES
        // ════════════════════════════════════════════════════════════════════
        private static bool ResolveTypes()
        {
            T_SaveManager = FindType("AbyssalReach.Core.SaveManager");
            T_SceneLoader = FindType("AbyssalReach.Core.SceneLoader");
            T_AudioManager = FindType("AbyssalReach.Core.AudioManager");
            T_MainMenuController = FindType("AbyssalReach.UI.MainMenu.MainMenuController");
            T_PauseMenuController = FindType("AbyssalReach.UI.Pause.PauseMenuController");
            T_SettingsMenuController = FindType("AbyssalReach.UI.SettingsMenuController");
            T_SaveSlotUI = FindType("AbyssalReach.UI.SaveSlotUI");
            T_GameInitializer = FindType("AbyssalReach.Core.GameInitializer");

            var missing = new List<string>();
            if (T_SaveManager == null) missing.Add("AbyssalReach.Core.SaveManager");
            if (T_SceneLoader == null) missing.Add("AbyssalReach.Core.SceneLoader");
            if (T_AudioManager == null) missing.Add("AbyssalReach.Core.AudioManager");
            if (T_MainMenuController == null) missing.Add("AbyssalReach.UI.MainMenu.MainMenuController");
            if (T_PauseMenuController == null) missing.Add("AbyssalReach.UI.Pause.PauseMenuController");
            if (T_SettingsMenuController == null) missing.Add("AbyssalReach.UI.SettingsMenuController");
            if (T_SaveSlotUI == null) missing.Add("AbyssalReach.UI.SaveSlotUI");

            if (missing.Count > 0)
            {
                EditorUtility.DisplayDialog("Scripts no encontrados",
                    "Faltan estos scripts compilados:\n\n• " +
                    string.Join("\n• ", missing) +
                    "\n\nAsegúrate de que están en el proyecto y compilan sin errores.",
                    "OK");
                return false;
            }
            return true;
        }

        private static Type FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = asm.GetType(fullName);
                    if (t != null) return t;
                }
                catch { }
            }
            return null;
        }

        private static void SetObjRef(SerializedObject so, string fieldName, UnityEngine.Object value)
        {
            var p = so.FindProperty(fieldName);
            if (p == null)
            {
                Debug.LogWarning($"[Builder] Campo '{fieldName}' no encontrado en {so.targetObject.GetType().Name}");
                return;
            }
            p.objectReferenceValue = value;
        }

        private static void SetObjArray(SerializedObject so, string fieldName, UnityEngine.Object[] values)
        {
            var p = so.FindProperty(fieldName);
            if (p == null || !p.isArray)
            {
                Debug.LogWarning($"[Builder] Array '{fieldName}' no encontrado en {so.targetObject.GetType().Name}");
                return;
            }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{cur}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        private static Color Hex(string s)
        {
            ColorUtility.TryParseHtmlString(s, out var c);
            return c;
        }

        private static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
        private static Sprite GetSprite(string resourcePath)
        {
            if (_spriteCache.TryGetValue(resourcePath, out var cached)) return cached;
            var s = AssetDatabase.GetBuiltinExtraResource<Sprite>(resourcePath);
            if (s != null) _spriteCache[resourcePath] = s;
            return s;
        }
    }
}
#endif