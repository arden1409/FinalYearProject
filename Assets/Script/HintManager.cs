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
    [Tooltip("GameObject chứa Text hoặc TextMeshProUGUI component")]
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
        // Ẩn hint panel ban đầu
        if (hintPanel != null)
        {
            hintPanel.SetActive(false);
        }

        // Setup close button
        if (hintCloseButton != null)
        {
            hintCloseButton.onClick.AddListener(CloseHint);
        }

        // Tìm Text component trong cooldownTextObject (tìm cả trong children)
        if (cooldownTextObject != null)
        {
            // Tìm TextMeshProUGUI trước (vì thường dùng TextMeshPro hơn)
            // Dùng GetComponents để tìm ngay cả khi component bị disable
            Component[] allComponents = cooldownTextObject.GetComponents<Component>();
            
            // Tìm component có type name là "TextMeshProUGUI"
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
                    // Nếu không có UNITY_TMPRO, vẫn có thể lưu reference
                    // Nhưng không thể dùng TextMeshProUGUI type
                    Debug.LogWarning("HintManager: Tìm thấy TextMeshProUGUI nhưng UNITY_TMPRO chưa được define!");
                    break;
#endif
                }
            }
            
#if UNITY_TMPRO
            // Nếu không tìm được, thử tìm trong children
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
            
            // Nếu vẫn không tìm được, thử dùng GetComponent thông thường
            if (cooldownTextTMP == null)
            {
                cooldownTextTMP = cooldownTextObject.GetComponent<TextMeshProUGUI>();
                if (cooldownTextTMP == null)
                {
                    cooldownTextTMP = cooldownTextObject.GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }
#endif
            
            // Nếu không tìm được TextMeshProUGUI, tìm Text component
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
            
            // Ẩn cooldown text từ ban đầu (chỉ hiện khi đã dùng hint)
            // Lưu ý: GameObject có thể đã không active từ đầu, nhưng vẫn có thể set active sau
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
            
            // Kiểm tra có tìm được component không
            if (cooldownText == null 
#if UNITY_TMPRO
                && cooldownTextTMP == null
#endif
                )
            {
                Debug.LogWarning($"HintManager: Không tìm thấy Text hoặc TextMeshProUGUI component trong '{cooldownTextObject.name}'! " +
                    $"Hãy kiểm tra GameObject có component Text hoặc TextMeshProUGUI không.");
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
            // Tìm Text component trước
            cooldownText = cooldownTextObject.GetComponent<Text>();
            if (cooldownText == null)
            {
                cooldownText = cooldownTextObject.GetComponentInChildren<Text>(true);
            }
            
#if UNITY_TMPRO
            // Nếu không tìm được Text, tìm TextMeshProUGUI
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

        // Hiển thị hint panel
        if (hintPanel != null)
        {
            hintPanel.SetActive(true);
            isHintActive = true;
            lastHintTime = Time.unscaledTime; // Dùng unscaledTime để đếm ngược ngay cả khi game pause
            
            // Ẩn cooldown text khi đang xem hint
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
            
            // Cập nhật cooldown text ngay khi đóng hint
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

        // Có thể thay đổi màu sắc hoặc hiển thị cooldown
        if (hintButtonImage != null)
        {
            Color buttonColor = canUse ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
            hintButtonImage.color = buttonColor;
        }
    }

    private void UpdateCooldownText()
    {
        // Nếu chưa tìm được Text component, thử tìm lại (kể cả khi GameObject không active)
        if (cooldownText == null 
#if UNITY_TMPRO
            && cooldownTextTMP == null
#endif
            && cooldownTextObject != null)
        {
            // Tìm TextMeshProUGUI trước (vì thường dùng TextMeshPro hơn)
            // Dùng GetComponents để tìm ngay cả khi component bị disable
            Component[] allComponents = cooldownTextObject.GetComponents<Component>();
            
            // Tìm component có type name là "TextMeshProUGUI"
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
                    // Nếu không có UNITY_TMPRO, vẫn có thể lưu reference
                    // Nhưng không thể dùng TextMeshProUGUI type
                    Debug.LogWarning("HintManager: Tìm thấy TextMeshProUGUI nhưng UNITY_TMPRO chưa được define!");
                    break;
#endif
                }
            }
            
#if UNITY_TMPRO
            // Nếu không tìm được, thử tìm trong children
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
            
            // Nếu vẫn không tìm được, thử dùng GetComponent thông thường
            if (cooldownTextTMP == null)
            {
                cooldownTextTMP = cooldownTextObject.GetComponent<TextMeshProUGUI>();
                if (cooldownTextTMP == null)
                {
                    cooldownTextTMP = cooldownTextObject.GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }
#endif
            
            // Nếu không tìm được TextMeshProUGUI, tìm Text component
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
            
            // Kiểm tra có tìm được component không
            if (cooldownText == null 
#if UNITY_TMPRO
                && cooldownTextTMP == null
#endif
                )
            {
                Debug.LogWarning($"HintManager: Không tìm thấy Text hoặc TextMeshProUGUI component trong '{cooldownTextObject.name}'! " +
                    $"GameObject active: {cooldownTextObject.activeSelf}, " +
                    $"Components: {string.Join(", ", cooldownTextObject.GetComponents<Component>().Select(c => c.GetType().Name))}");
            }
        }

        // Chỉ hiển thị cooldown nếu đã dùng hint (lastHintTime > 0)
        // lastHintTime = -999f nghĩa là chưa dùng hint lần nào
        if (lastHintTime < 0f)
        {
            // Chưa dùng hint, ẩn cooldown text
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
        
        // Hỗ trợ cả Text và TextMeshPro
        if (cooldownText != null)
        {
            if (remaining > 0f)
            {
                // Set text value trước
                cooldownText.text = text;
                
                // Đảm bảo GameObject active (kể cả khi ban đầu không active)
                GameObject textObj = cooldownText.gameObject;
                if (!textObj.activeSelf)
                {
                    textObj.SetActive(true);
                }
            }
            else
            {
                // Hết cooldown, ẩn text
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
                // Set text value trước
                cooldownTextTMP.text = text;
                
                // Đảm bảo GameObject active (kể cả khi ban đầu không active)
                GameObject textObj = cooldownTextTMP.gameObject;
                if (!textObj.activeSelf)
                {
                    textObj.SetActive(true);
                }
            }
            else
            {
                // Hết cooldown, ẩn text
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
        // Cập nhật trạng thái nút hint mỗi frame để hiển thị cooldown
        if (hintButton != null && !isHintActive)
        {
            UpdateHintButtonState();
        }

        // Cập nhật cooldown text
        UpdateCooldownText();
    }

    public float GetCooldownRemaining()
    {
        if (lastHintTime < 0f) return 0f; // Chưa dùng hint
        float timeSinceLastHint = Time.unscaledTime - lastHintTime;
        return Mathf.Max(0f, hintCooldown - timeSinceLastHint);
    }

    public bool IsHintAvailable()
    {
        if (lastHintTime < 0f) return true; // Chưa dùng hint, có thể dùng
        float timeSinceLastHint = Time.unscaledTime - lastHintTime;
        return timeSinceLastHint >= hintCooldown && !isHintActive;
    }
}

