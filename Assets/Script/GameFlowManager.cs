using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    public enum GameState
    {
        Boot,
        CharacterIntro,
        MainMenu,
        LevelSelect,
        StoryIntro,
        Gameplay,
        StoryOutro
    }

    [Serializable]
    public class LevelDefinition
    {
        public string levelId = "Level_1";
        public string levelScene = "Assets/Scenes/Level/Level1.unity";
        [Tooltip("Optional story sequence id to play before the level")]
        public string storyIntroId = "";
        [Tooltip("Optional story sequence id to play after the level")]
        public string storyOutroId = "";
        public bool unlockedByDefault = false;
    }

    [Serializable]
    public class LevelProgress
    {
        public string levelId;
        public bool unlocked;
        public bool completed;
        public int bestScore;

        public LevelProgress(string id, bool unlockedByDefault)
        {
            levelId = id;
            unlocked = unlockedByDefault;
            completed = false;
            bestScore = 0;
        }
    }

    [Header("Scene Names")]
    public string characterIntroScene = "Assets/Scenes/CharacterIntro.unity";
    public string mainMenuScene = "Assets/Scenes/MainMenu.unity";
    public string levelSelectScene = "Assets/Scenes/LevelSelect.unity";
    public string storyScene = "Assets/Scenes/StoryScreen.unity";

    [Header("Level List")]
    public List<LevelDefinition> levels = new List<LevelDefinition>();

    public Action<GameState> onStateChanged;
    public Action<LevelProgress> onProgressUpdated;

    public GameState CurrentState { get; private set; } = GameState.Boot;
    public LevelDefinition CurrentLevel { get; private set; }
    public LevelProgress CurrentLevelProgress { get; private set; }

    private readonly Dictionary<string, LevelProgress> progressLookup = new Dictionary<string, LevelProgress>();
    private const string ProgressKeyPrefix = "LEVEL_PROGRESS_";
    private const string LastLevelKey = "LAST_LEVEL";

    private const string HasSeenCharacterIntroKey = "HAS_SEEN_CHARACTER_INTRO";
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BootstrapProgress();
    }
    
    private void Start()
    {
        // Chỉ load character intro khi game khởi động lần đầu (Boot state)
        if (CurrentState == GameState.Boot)
        {
            // Kiểm tra xem đã xem character intro chưa (có thể bỏ qua nếu muốn luôn hiển thị)
            bool hasSeenIntro = PlayerPrefs.GetInt(HasSeenCharacterIntroKey, 0) == 1;
            
            if (!hasSeenIntro && !string.IsNullOrEmpty(characterIntroScene))
            {
                LoadCharacterIntro();
            }
            else
            {
                LoadMainMenu();
            }
        }
    }
    
    private void LoadCharacterIntro()
    {
        TransitionState(GameState.CharacterIntro);
        LoadSceneAsync(characterIntroScene);
    }
    
    public void NotifyCharacterIntroFinished()
    {
        // Đánh dấu đã xem character intro
        PlayerPrefs.SetInt(HasSeenCharacterIntroKey, 1);
        PlayerPrefs.Save();
        
        LoadMainMenu();
    }

    private void BootstrapProgress()
    {
        progressLookup.Clear();
        foreach (var level in levels)
        {
            if (string.IsNullOrEmpty(level.levelId))
                continue;

            LevelProgress data = LoadLevelProgress(level);
            progressLookup[level.levelId] = data;
        }
    }

    private LevelProgress LoadLevelProgress(LevelDefinition level)
    {
        string key = ProgressKeyPrefix + level.levelId;
        if (!PlayerPrefs.HasKey(key))
        {
            LevelProgress newProgress = new LevelProgress(level.levelId, level.unlockedByDefault);
            Debug.Log($"[GameFlowManager] Created new progress for {level.levelId}: Unlocked={newProgress.unlocked}");
            return newProgress;
        }

        string json = PlayerPrefs.GetString(key);
        LevelProgress progress = JsonUtility.FromJson<LevelProgress>(json);
        if (progress == null)
        {
            progress = new LevelProgress(level.levelId, level.unlockedByDefault);
            Debug.LogWarning($"[GameFlowManager] Failed to parse progress for {level.levelId}, using default");
        }
        else
        {
            Debug.Log($"[GameFlowManager] Loaded progress for {level.levelId}: Unlocked={progress.unlocked}, Completed={progress.completed}, Score={progress.bestScore}");
        }
        return progress;
    }

    private void SaveLevelProgress(LevelProgress progress)
    {
        if (progress == null)
            return;
            
        string json = JsonUtility.ToJson(progress);
        string key = ProgressKeyPrefix + progress.levelId;
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save(); // Đảm bảo lưu ngay vào disk
        
        // Debug log để kiểm tra
        Debug.Log($"[GameFlowManager] Saved progress for {progress.levelId}: Unlocked={progress.unlocked}, Completed={progress.completed}, Score={progress.bestScore}");
        
        onProgressUpdated?.Invoke(progress);
    }

    public void ResetAllProgress()
    {
        foreach (var kvp in progressLookup)
        {
            kvp.Value.completed = false;
            kvp.Value.bestScore = 0;
            kvp.Value.unlocked = GetLevelDefinition(kvp.Key)?.unlockedByDefault ?? false;
            SaveLevelProgress(kvp.Value);
        }
        PlayerPrefs.DeleteKey(LastLevelKey);
        PlayerPrefs.DeleteKey(HasSeenCharacterIntroKey);
    }

    public void LoadMainMenu()
    {
        CurrentLevel = null;
        CurrentLevelProgress = null;
        TransitionState(GameState.MainMenu);
        LoadSceneAsync(mainMenuScene);
    }

    public void LoadLevelSelect()
    {
        CurrentLevel = null;
        CurrentLevelProgress = null;
        TransitionState(GameState.LevelSelect);
        LoadSceneAsync(levelSelectScene);
    }

    public void StartNewGame()
    {
        ResetAllProgress();
        LevelDefinition firstLevel = levels.Count > 0 ? levels[0] : null;
        if (firstLevel == null)
        {
            Debug.LogWarning("[GameFlowManager] No levels configured.");
            return;
        }

        StartLevel(firstLevel.levelId, playStoryIntro: true);
    }

    public void ContinueGame()
    {
        string lastLevelId = PlayerPrefs.GetString(LastLevelKey, string.Empty);
        
        // Nếu có LAST_LEVEL và level đó đã unlock, tiếp tục từ đó
        if (!string.IsNullOrEmpty(lastLevelId))
        {
            if (progressLookup.TryGetValue(lastLevelId, out LevelProgress progress) && progress.unlocked)
            {
                StartLevel(lastLevelId, playStoryIntro: false);
                return;
            }
        }
        
        // Nếu không có LAST_LEVEL hoặc level đó bị lock, tìm level đầu tiên đã unlock
        foreach (var level in levels)
        {
            if (progressLookup.TryGetValue(level.levelId, out LevelProgress levelProgress) && levelProgress.unlocked)
            {
                StartLevel(level.levelId, playStoryIntro: false);
                return;
            }
        }
        
        // Nếu không có level nào unlock, chuyển sang LevelSelect
        LoadLevelSelect();
    }

    public void StartLevel(string levelId, bool playStoryIntro = true)
    {
        LevelDefinition definition = GetLevelDefinition(levelId);
        if (definition == null)
        {
            Debug.LogError($"[GameFlowManager] Level id {levelId} not found.");
            return;
        }

        if (!progressLookup.TryGetValue(levelId, out LevelProgress progress) || !progress.unlocked)
        {
            Debug.LogWarning($"[GameFlowManager] Level {levelId} is locked.");
            return;
        }

        CurrentLevel = definition;
        CurrentLevelProgress = progress;
        PlayerPrefs.SetString(LastLevelKey, levelId);
        PlayerPrefs.Save();

        if (playStoryIntro && !string.IsNullOrEmpty(definition.storyIntroId))
        {
            TransitionState(GameState.StoryIntro);
            LoadSceneAsync(storyScene);
        }
        else
        {
            TransitionState(GameState.Gameplay);
            LoadSceneAsync(definition.levelScene);
        }
    }

    public void NotifyStoryIntroFinished()
    {
        if (CurrentLevel == null)
        {
            Debug.LogWarning("[GameFlowManager] No active level for story intro.");
            return;
        }

        TransitionState(GameState.Gameplay);
        LoadSceneAsync(CurrentLevel.levelScene);
    }

    public void CompleteLevel(int score, bool autoTransition = true)
    {
        if (CurrentLevelProgress == null || CurrentLevel == null)
        {
            Debug.LogWarning("[GameFlowManager] CompleteLevel called without active level.");
            return;
        }

        CurrentLevelProgress.completed = true;
        CurrentLevelProgress.bestScore = Mathf.Max(CurrentLevelProgress.bestScore, score);
        SaveLevelProgress(CurrentLevelProgress);

        UnlockNextLevel(CurrentLevel.levelId);

        if (!autoTransition)
        {
            return;
        }

        if (!string.IsNullOrEmpty(CurrentLevel.storyOutroId))
        {
            TransitionState(GameState.StoryOutro);
            LoadSceneAsync(storyScene);
        }
        else
        {
            LoadLevelSelect();
        }
    }

    public void LoadNextLevelImmediate()
    {
        if (levels == null || levels.Count == 0)
        {
            Debug.LogWarning("[GameFlowManager] No levels configured.");
            return;
        }

        if (CurrentLevel == null)
        {
            Debug.LogWarning("[GameFlowManager] No current level to advance from. Returning to level select.");
            LoadLevelSelect();
            return;
        }

        int currentIndex = levels.FindIndex(l => l.levelId == CurrentLevel.levelId);
        if (currentIndex < 0 || currentIndex + 1 >= levels.Count)
        {
            Debug.Log("[GameFlowManager] Completed the last available level. Returning to level select.");
            LoadLevelSelect();
            return;
        }

        LevelDefinition next = levels[currentIndex + 1];
        if (!progressLookup.TryGetValue(next.levelId, out var nextProgress) || !nextProgress.unlocked)
        {
            Debug.LogWarning($"[GameFlowManager] Next level {next.levelId} is locked. Returning to level select.");
            LoadLevelSelect();
            return;
        }

        StartLevel(next.levelId, playStoryIntro: false);
    }

    public void NotifyStoryOutroFinished()
    {
        // Sau khi story outro kết thúc, có thể chuyển sang level tiếp theo hoặc về level select
        // Hiện tại mặc định về level select, có thể sửa để tự động chuyển level tiếp theo nếu muốn
        LoadLevelSelect();
    }

    private void UnlockNextLevel(string completedLevelId)
    {
        int idx = levels.FindIndex(l => l.levelId == completedLevelId);
        if (idx < 0 || idx + 1 >= levels.Count)
            return;

        LevelDefinition next = levels[idx + 1];
        
        // Đảm bảo progress entry tồn tại
        if (!progressLookup.TryGetValue(next.levelId, out LevelProgress progress))
        {
            // Tạo entry mới nếu chưa có
            progress = new LevelProgress(next.levelId, next.unlockedByDefault);
            progressLookup[next.levelId] = progress;
        }
        
        // Unlock và lưu ngay lập tức
        if (!progress.unlocked)
        {
            progress.unlocked = true;
            SaveLevelProgress(progress);
            Debug.Log($"[GameFlowManager] Unlocked level: {next.levelId}");
        }
    }

    private void LoadSceneAsync(string scenePathOrName)
    {
        if (string.IsNullOrEmpty(scenePathOrName))
        {
            Debug.LogWarning("[GameFlowManager] Attempted to load empty scene name.");
            return;
        }

        string sceneName = scenePathOrName.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFileNameWithoutExtension(scenePathOrName)
            : scenePathOrName;

        SceneManager.LoadScene(sceneName);
    }

    public LevelDefinition GetLevelDefinition(string levelId)
    {
        if (string.IsNullOrEmpty(levelId))
            return null;

        return levels.Find(l => l.levelId == levelId);
    }

    public LevelProgress GetProgress(string levelId)
    {
        if (string.IsNullOrEmpty(levelId))
            return null;

        progressLookup.TryGetValue(levelId, out var progress);
        return progress;
    }

    private void TransitionState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
        onStateChanged?.Invoke(newState);
        Debug.Log($"[GameFlowManager] State changed to {newState}");
    }
}

