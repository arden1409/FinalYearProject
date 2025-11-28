using System.Collections;
using UnityEngine;

/// <summary>
/// Controls book animation in Level Select:
/// - Plays book opening animation (state BookAnimate or configured name).
/// - When animation ends (called from Animation Event OnBookOpened), shows level panel with fade-in effect.
/// </summary>
public class BookAnimationController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Animator attached to book object")]
    [SerializeField] private Animator bookAnimator;

    [Tooltip("Panel containing level selection buttons")]
    [SerializeField] private GameObject levelSelectPanel;

    [Tooltip("CanvasGroup of panel for fade-in effect")]
    [SerializeField] private CanvasGroup levelSelectCanvasGroup;

    [Header("Animation Settings")]
    [Tooltip("Name of opening state/clip in Animator (e.g. BookAnimate)")]
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
    /// Call to play book opening animation (usually from Start).
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
    /// Called from Animation Event at the end of opening clip.
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


