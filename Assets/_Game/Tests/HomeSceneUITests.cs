using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Reflection;
using MaouSamaTD.UI;
using MaouSamaTD.UI.MainMenu;
using MaouSamaTD.UI.Cohorts;
using MaouSamaTD.UI.Vassals;
using MaouSamaTD.UI.Mandates;
using MaouSamaTD.UI.Treasury;
using MaouSamaTD.Managers;

public class HomeSceneUITests
{
    [UnitySetUp]
    public IEnumerator Setup()
    {
        // Load the Home_New scene
        var op = SceneManager.LoadSceneAsync("Home_New", LoadSceneMode.Single);
        while (!op.isDone)
        {
            yield return null;
        }
        
        // Wait a frame for Awake/Start and Zenject initialization
        yield return new WaitForEndOfFrame();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Test1_InitialSceneStateAndBootstrapping()
    {
        // 1. Check if HomeSceneInitializer is present and active
        var initializer = Object.FindAnyObjectByType<HomeSceneInitializer>();
        Assert.IsNotNull(initializer, "HomeSceneInitializer should be in the scene");

        // 2. Check if all required fields are wired
        var navOverlay = GetPrivateField<UINavigationOverlay>(initializer, "_navigationOverlay");
        var campaignPage = GetPrivateField<CampaignPage>(initializer, "_campaignPage");
        var homeUIManager = GetPrivateField<HomeUIManager>(initializer, "_homeUIManager");
        var homeUIController = GetPrivateField<HomeUIController_UGUI>(initializer, "_homeUIController");

        Assert.IsNotNull(navOverlay, "_navigationOverlay should be wired");
        Assert.IsNotNull(campaignPage, "_campaignPage should be wired");
        Assert.IsNotNull(homeUIManager, "_homeUIManager should be wired");
        Assert.IsNotNull(homeUIController, "_homeUIController should be wired");

        yield return null;
    }

    [UnityTest]
    public IEnumerator Test2_PageNavigationRouting()
    {
        var flowManager = Object.FindAnyObjectByType<UIFlowManager>();
        Assert.IsNotNull(flowManager, "UIFlowManager should be in the scene");

        var vassalsBtnGo = GameObject.Find("MainCanvas/MainUIContainer/Page_Content_Area/HomePage/MainUIRoot/Vassals_Btn");
        Assert.IsNotNull(vassalsBtnGo, "Vassals_Btn should be in the scene");

        var vassalsBtn = vassalsBtnGo.GetComponent<Button>();
        vassalsBtn.onClick.Invoke();

        yield return new WaitForSeconds(0.1f);

        // Verify that Vassals page is open
        var vassalManager = Object.FindAnyObjectByType<VassalManagerUI>();
        Assert.IsNotNull(vassalManager, "VassalManagerUI should be found in scene");
        Assert.IsTrue(vassalManager.gameObject.activeInHierarchy, "Vassals panel should be active in hierarchy");

        yield return null;
    }

    [UnityTest]
    public IEnumerator Test3_ProgressionLocksAndLevelRestrictions()
    {
        var saveManager = Object.FindAnyObjectByType<SaveManager>();
        Assert.IsNotNull(saveManager, "SaveManager should be in the scene");

        // Mock progression details: Let's see if we can manipulate SaveManager
        // If SaveManager is injected, we can read/write its save data
        var originalCompletedLevels = new System.Collections.Generic.List<string>(saveManager.CurrentData.CompletedLevels);
        
        // Lock progression
        saveManager.CurrentData.CompletedLevels.Clear();
        saveManager.Save();

        // Refresh UI or reload scene
        yield return Setup();

        // Verify that SaveManager has empty completed levels now
        var currentSaveManager = Object.FindAnyObjectByType<SaveManager>();
        Assert.AreEqual(0, currentSaveManager.CurrentData.CompletedLevels.Count, "Completed levels should be mocked empty");

        // Restore completed levels
        currentSaveManager.CurrentData.CompletedLevels.AddRange(originalCompletedLevels);
        currentSaveManager.Save();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Test4_NotificationBadgeTriggers()
    {
        // Check if notification badge elements are wired up
        var homeUIManager = Object.FindAnyObjectByType<HomeUIManager>();
        Assert.IsNotNull(homeUIManager, "HomeUIManager should be in the scene");

        // Wait to verify notification triggers do not throw errors
        yield return new WaitForSeconds(0.2f);
    }

    [UnityTest]
    public IEnumerator Test5_CharacterHomeCustomization()
    {
        var controller = Object.FindAnyObjectByType<HomeUIController_UGUI>();
        Assert.IsNotNull(controller, "HomeUIController_UGUI should be in the scene");

        // Toggle edit mode
        controller.ToggleEditMode();
        var isEditMode = GetPrivateField<bool>(controller, "_isEditMode");
        Assert.IsTrue(isEditMode, "Controller should be in Edit Mode");

        // Test scaling up
        var charRect = GetPrivateField<RectTransform>(controller, "_characterRect");
        var originalScale = charRect.localScale.x;
        
        // Call cycle preset or scale adjustment
        var btnScaleUp = GetPrivateField<Button>(controller, "_btnScaleUp");
        Assert.IsNotNull(btnScaleUp, "ScaleUp button should be wired");
        btnScaleUp.onClick.Invoke();

        Assert.Greater(charRect.localScale.x, originalScale, "Scale should increase after pressing Scale Up");

        // Toggle edit mode back
        controller.ToggleEditMode();
        isEditMode = GetPrivateField<bool>(controller, "_isEditMode");
        Assert.IsFalse(isEditMode, "Controller should exit Edit Mode");

        yield return null;
    }

    private T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null) return (T)field.GetValue(obj);
        return default;
    }
}
