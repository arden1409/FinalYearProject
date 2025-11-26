using System.Collections;
using UnityEngine;

/// <summary>
/// Điều khiển animation quyển sách ở Level Select:
/// - Phát animation mở sách (state BookAnimate hoặc tên bạn cấu hình).
/// - Khi animation kết thúc (gọi từ Animation Event OnBookOpened), hiện panel level bằng hiệu ứng fade-in.
/// </summary>
public class BookAnimationController : MonoBehaviour
{
    [Header("Tham chiếu")]
    [Tooltip("Animator gắn trên object quyển sách")]
    [SerializeField] private Animator bookAnimator;

    [Tooltip("Panel chứa các nút chọn level")]
    [SerializeField] private GameObject levelSelectPanel;

    [Tooltip("CanvasGroup của panel để làm hiệu ứng fade-in")]
    [SerializeField] private CanvasGroup levelSelectCanvasGroup;

    [Header("Thiết lập animation")]
    [Tooltip("Tên state/clip mở sách trong Animator (ví dụ: BookAnimate)")]
    [SerializeField] private string openAnimationName = "BookAnimate";

    [SerializeField] private bool playOnStart = true;

    [Range(0.1f, 2f)]
    [SerializeField] private float animationSpeed = 1f;

    [Header("Fade panel")]
    [SerializeField] private float panelFadeDuration = 0.5f;

    private bool animationPlayed;

    private void Reset()
    {
        bookAnimator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (bookAnimator == null)
            bookAnimator = GetComponent<Animator>();

        if (levelSelectPanel != null && levelSelectCanvasGroup == null)
            levelSelectCanvasGroup = levelSelectPanel.GetComponent<CanvasGroup>();

        if (levelSelectPanel != null)
            levelSelectPanel.SetActive(false);

        if (levelSelectCanvasGroup != null)
        {
            levelSelectCanvasGroup.alpha = 0f;
            levelSelectCanvasGroup.interactable = false;
            levelSelectCanvasGroup.blocksRaycasts = false;
        }
    }

    private void Start()
    {
        if (playOnStart)
            PlayOpenAnimation();
    }

    /// <summary>
    /// Gọi khi muốn phát animation mở sách (thường là Start).
    /// </summary>
    public void PlayOpenAnimation()
    {
        if (animationPlayed || bookAnimator == null)
            return;

        bookAnimator.speed = animationSpeed;

        if (!string.IsNullOrEmpty(openAnimationName))
            bookAnimator.Play(openAnimationName, 0, 0f);
        else
            bookAnimator.Play(0, 0, 0f);

        animationPlayed = true;
    }

    /// <summary>
    /// Gọi từ Animation Event ở frame cuối clip mở sách.
    /// </summary>
    public void OnBookOpened()
    {
        StartCoroutine(FadePanelIn());
    }

    private IEnumerator FadePanelIn()
    {
        if (levelSelectPanel != null)
            levelSelectPanel.SetActive(true);

        if (levelSelectCanvasGroup == null)
            yield break;

        levelSelectCanvasGroup.alpha = 0f;
        levelSelectCanvasGroup.interactable = false;
        levelSelectCanvasGroup.blocksRaycasts = false;

        float elapsed = 0f;

        while (elapsed < panelFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / panelFadeDuration);
            levelSelectCanvasGroup.alpha = t;
            yield return null;
        }

        levelSelectCanvasGroup.alpha = 1f;
        levelSelectCanvasGroup.interactable = true;
        levelSelectCanvasGroup.blocksRaycasts = true;
    }
}


