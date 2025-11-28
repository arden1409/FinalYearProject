using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_TMPRO
using TMPro;
#endif

public class HintManager : MonoBehaviour
{
    [Header("Hint Settings")]
    [SerializeField] private float hintCooldown = 30f;
    
    [Header("Hint Panel")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private Button hintCloseButton;
    
    [Header("Cooldown Display")]
    [Tooltip("GameObject containing Text or TextMeshProUGUI component")]
    [SerializeField] private GameObject cooldownTextObject;
    [SerializeField] private string cooldownFormat = "{0:F0}s";
    
    private Text cooldownText;
#if UNITY_TMPRO
    private TextMeshProUGUI cooldownTextTMP;
#endif

    private float lastHintTime = -999f;
    private bool isHintActive = false;
    private Button hintButton;
    private Image hintButtonImage;

    public static HintManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (hintPanel != null)
        {
            hintPanel.SetActive(false);
        }

        if (hintCloseButton != null)
        {
            hintCloseButton.onClick.AddListener(CloseHint);
        }

        // Find Text component in cooldownTextObject (search in children too)
        if (cooldownTextObject != null)
        {
            // Try TextMeshProUGUI first (more commonly used)
            Component[] allComponents = cooldownTextObject.GetComponents<Component>();
            
            foreach (Component comp in allComponents)
            {
                if (comp != null && comp.GetType().Name == "TextMeshProUGUI")
                {
#if UNITY_TMPRO
                    cooldownTextTMP = comp as TextMeshProUGUI;
                    if (cooldownTextTMP != null)
                    {
                        break;
                    }
#else
                    Debug.LogWarning("HintManager: Found TextMeshProUGUI but UNITY_TMPRO is not defined!");
                    break;
#endif
                }
            }
            
#if UNITY_TMPRO
            if (cooldownTextTMP == null)
            {
                allComponents = cooldownTextObject.GetComponentsInChildren<Component>(true);
                foreach (Component comp in allComponents)
                {
                    if (comp != null && comp.gameObject != cooldownTextObject && 
                        comp.GetType().Name == "TextMeshProUGUI")
                    {
                        cooldownTextTMP = comp as TextMeshProUGUI;
                        if (cooldownTextTMP != null)
                        {
                            break;
                        }
                    }
                }
            }
            
            if (cooldownTextTMP == null)
            {
                cooldownTextTMP = cooldownTextObject.GetComponent<TextMeshProUGUI>();
                if (cooldownTextTMP == null)
                {
                    cooldownTextTMP = cooldownTextObject.GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }
#endif
            
            if (
#if UNITY_TMPRO
                cooldownTextTMP == null &&
#endif
                cooldownText == null)
            {
                cooldownText = cooldownTextObject.GetComponent<Text>();
                if (cooldownText == null)
                {
                    cooldownText = cooldownTextObject.GetComponentInChildren<Text>(true);
                }
            }
            
            // Hide cooldown text initially (only show after using hint)
            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(false);
            }
#if UNITY_TMPRO
            if (cooldownTextTMP != null)
            {
                cooldownTextTMP.gameObject.SetActive(false);
            }
#endif
            
            if (cooldownText == null 
#if UNITY_TMPRO
                && cooldownTextTMP == null
#endif
                )
            {
                Debug.LogWarning($"HintManager: Could not find Text or TextMeshProUGUI component in '{cooldownTextObject.name}'! " +
                    $"Please check if GameObject has Text or TextMeshProUGUI component.");
            }
        }
    }

    public void SetHintButton(Button button)
    {
        hintButton = button;
        if (hintButton != null)
        {
            hintButtonImage = hintButton.GetComponent<Image>();
            UpdateHintButtonState();
        }
    }

    public void SetCooldownTextObject(GameObject textObject)
    {
        cooldownTextObject = textObject;
        cooldownText = null;
#if UNITY_TMPRO
        cooldownTextTMP = null;
#endif
        
        if (cooldownTextObject != null)
        {
            cooldownText = cooldownTextObject.GetComponent<Text>();
            if (cooldownText == null)
            {
                cooldownText = cooldownTextObject.GetComponentInChildren<Text>(true);
            }
            
#if UNITY_TMPRO
            if (cooldownText == null)
            {
                cooldownTextTMP = cooldownTextObject.GetComponent<TextMeshProUGUI>();
                if (cooldownTextTMP == null)
                {
                    cooldownTextTMP = cooldownTextObject.GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }
#endif
        }
    }

    public void ShowHint()
    {
        float timeSinceLastHint = Time.unscaledTime - lastHintTime;
        
        if (lastHintTime > 0f && timeSinceLastHint < hintCooldown)
        {
            return;
        }

        if (isHintActive)
        {
            return;
        }

        if (hintPanel != null)
        {
            hintPanel.SetActive(true);
            isHintActive = true;
            lastHintTime = Time.unscaledTime; // Use unscaledTime to countdown even when game is paused
            
            UpdateCooldownText();
            UpdateHintButtonState();
        }
    }

    public void CloseHint()
    {
        if (hintPanel != null)
        {
            hintPanel.SetActive(false);
            isHintActive = false;
            
            UpdateCooldownText();
            UpdateHintButtonState();
        }
    }

    private void UpdateHintButtonState()
    {
        if (hintButton == null) return;

        float timeSinceLastHint = lastHintTime < 0f ? 999f : (Time.unscaledTime - lastHintTime);
        bool canUse = timeSinceLastHint >= hintCooldown && !isHintActive;

        hintButton.interactable = canUse;

        if (hintButtonImage != null)
        {
            Color buttonColor = canUse ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
            hintButtonImage.color = buttonColor;
        }
    }

    private void UpdateCooldownText()
    {
        // Try to find Text component again if not found (even if GameObject is inactive)
        if (cooldownText == null 
#if UNITY_TMPRO
            && cooldownTextTMP == null
#endif
            && cooldownTextObject != null)
        {
            Component[] allComponents = cooldownTextObject.GetComponents<Component>();
            
            foreach (Component comp in allComponents)
            {
                if (comp != null && comp.GetType().Name == "TextMeshProUGUI")
                {
#if UNITY_TMPRO
                    cooldownTextTMP = comp as TextMeshProUGUI;
                    if (cooldownTextTMP != null)
                    {
                        break;
                    }
#else
                    Debug.LogWarning("HintManager: Found TextMeshProUGUI but UNITY_TMPRO is not defined!");
                    break;
#endif
                }
            }
            
#if UNITY_TMPRO
            if (cooldownTextTMP == null)
            {
                allComponents = cooldownTextObject.GetComponentsInChildren<Component>(true);
                foreach (Component comp in allComponents)
                {
                    if (comp != null && comp.gameObject != cooldownTextObject && 
                        comp.GetType().Name == "TextMeshProUGUI")
                    {
                        cooldownTextTMP = comp as TextMeshProUGUI;
                        if (cooldownTextTMP != null)
                        {
                            break;
                        }
                    }
                }
            }
            
            if (cooldownTextTMP == null)
            {
                cooldownTextTMP = cooldownTextObject.GetComponent<TextMeshProUGUI>();
                if (cooldownTextTMP == null)
                {
                    cooldownTextTMP = cooldownTextObject.GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }
#endif
            
            if (
#if UNITY_TMPRO
                cooldownTextTMP == null &&
#endif
                cooldownText == null)
            {
                cooldownText = cooldownTextObject.GetComponent<Text>();
                if (cooldownText == null)
                {
                    cooldownText = cooldownTextObject.GetComponentInChildren<Text>(true);
                }
            }
            
            if (cooldownText == null 
#if UNITY_TMPRO
                && cooldownTextTMP == null
#endif
                )
            {
                Debug.LogWarning($"HintManager: Could not find Text or TextMeshProUGUI component in '{cooldownTextObject.name}'! " +
                    $"GameObject active: {cooldownTextObject.activeSelf}, " +
                    $"Components: {string.Join(", ", cooldownTextObject.GetComponents<Component>().Select(c => c.GetType().Name))}");
            }
        }

        // Only show cooldown if hint has been used (lastHintTime > 0)
        // lastHintTime = -999f means hint hasn't been used yet
        if (lastHintTime < 0f)
        {
            if (cooldownText != null && cooldownText.gameObject.activeSelf)
            {
                cooldownText.gameObject.SetActive(false);
            }
#if UNITY_TMPRO
            if (cooldownTextTMP != null && cooldownTextTMP.gameObject.activeSelf)
            {
                cooldownTextTMP.gameObject.SetActive(false);
            }
#endif
            return;
        }

        float remaining = GetCooldownRemaining();
        string text = string.Format(cooldownFormat, remaining);
        
        if (cooldownText != null)
        {
            if (remaining > 0f)
            {
                cooldownText.text = text;
                
                GameObject textObj = cooldownText.gameObject;
                if (!textObj.activeSelf)
                {
                    textObj.SetActive(true);
                }
            }
            else
            {
                if (cooldownText.gameObject.activeSelf)
                {
                    cooldownText.gameObject.SetActive(false);
                }
            }
        }
#if UNITY_TMPRO
        else if (cooldownTextTMP != null)
        {
            if (remaining > 0f)
            {
                cooldownTextTMP.text = text;
                
                GameObject textObj = cooldownTextTMP.gameObject;
                if (!textObj.activeSelf)
                {
                    textObj.SetActive(true);
                }
            }
            else
            {
                if (cooldownTextTMP.gameObject.activeSelf)
                {
                    cooldownTextTMP.gameObject.SetActive(false);
                }
            }
        }
#endif
    }

    private void Update()
    {
        if (hintButton != null && !isHintActive)
        {
            UpdateHintButtonState();
        }

        UpdateCooldownText();
    }

    public float GetCooldownRemaining()
    {
        if (lastHintTime < 0f) return 0f;
        float timeSinceLastHint = Time.unscaledTime - lastHintTime;
        return Mathf.Max(0f, hintCooldown - timeSinceLastHint);
    }

    public bool IsHintAvailable()
    {
        if (lastHintTime < 0f) return true;
        float timeSinceLastHint = Time.unscaledTime - lastHintTime;
        return timeSinceLastHint >= hintCooldown && !isHintActive;
    }
}

